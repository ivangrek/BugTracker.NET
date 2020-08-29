namespace BugTracker.Web.Core.Identification
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Caching.Memory;

    public interface IAuthenticate
    {
        Task SignInAsync(string username, string password, bool persistent, bool asGuest);

        Task SignOutAsync();
    }

    internal sealed class Authenticate : IAuthenticate
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IApplicationLogger applicationLogger;
        private readonly IDbUtil dbUtil;
        private readonly IMemoryCache memoryCache;

        public Authenticate(
            IApplicationSettings applicationSettings,
            IHttpContextAccessor httpContextAccessor,
            IApplicationLogger applicationLogger,
            IDbUtil dbUtil,
            IMemoryCache memoryCache)
        {
            this.applicationSettings = applicationSettings;
            this.httpContextAccessor = httpContextAccessor;
            this.applicationLogger = applicationLogger;
            this.dbUtil = dbUtil;
            this.memoryCache = memoryCache;
        }

        public async Task SignInAsync(string username, string password, bool persistent, bool asGuest)
        {
            //For now
            if (asGuest)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, "guest"),
                    new Claim(ClaimTypes.Role, BtNetRole.Guest)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

                await this.httpContextAccessor
                    .HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));

                return;
            }

            var authenticated = CheckPassword(username, password);

            if (!authenticated)
            {
                //result.Success = false;
                //result.ErrorMessage = "Invalid User or Password.";
                throw new InvalidOperationException("Invalid User or Password.");
            }

            var sql = new SqlString("select us_id, us_username, us_org from users where us_username = @us");

            sql = sql.AddParameterWithValue("us", username);

            var dr = this.dbUtil
                .GetDataRow(sql);

            if (dr != null)
            {
                var identity = GetIdentity(username);

                //var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authenticationProperties = new AuthenticationProperties
                {
                    IsPersistent = persistent/*model.RememberMe && !model.AsGuest*/
                };

                await this.httpContextAccessor
                    .HttpContext.SignInAsync(
                        CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity),
                        authenticationProperties);

                //result.Success = true;
                //result.ErrorMessage = string.Empty;
            }
            else
            {
                // How could this happen?  If someday the authentication
                // method uses, say LDAP, then check_password could return
                // true, even though there's no user in the database";
                //result.Success = false;
                //result.ErrorMessage = "User not found in database";
                throw new InvalidOperationException("User not found in database");
            }

            //return result;
        }

        public async Task SignOutAsync()
        {
            await this.httpContextAccessor
                .HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        private bool CheckPassword(string username, string password)
        {
            var sql = new SqlString(@"
                select
                    us_username,
                    us_id,
                    us_password,
                    isnull(us_salt,0) us_salt,
                    us_active
                from
                    users
                where
                    us_username = @username");

            sql = sql.AddParameterWithValue("username", username);

            var dr = this.dbUtil
                .GetDataRow(sql);

            if (dr == null)
            {
                this.applicationLogger
                    .WriteToLog($"Unknown user {username} attempted to login.");

                return false;
            }

            var usActive = (int)dr["us_active"];

            if (usActive == 0)
            {
                this.applicationLogger
                    .WriteToLog($"Inactive user {username} attempted to login.");

                return false;
            }

            // Too many failed attempts?
            // We'll only allow N in the last N minutes.
            var failedAttempts = this.memoryCache
                .Get<LinkedList<DateTime>>(username);

            if (failedAttempts != null)
            {
                // Don't count attempts older than N minutes ago.
                var minutesAgo = this.applicationSettings.FailedLoginAttemptsMinutes;
                var failedAttemptsAllowed = this.applicationSettings.FailedLoginAttemptsAllowed;

                var nMinutesAgo = DateTime.Now.AddMinutes(-1 * minutesAgo);

                while (true)
                {
                    if (failedAttempts.Count > 0)
                    {
                        if (failedAttempts.First.Value < nMinutesAgo)
                        {
                            this.applicationLogger
                                .WriteToLog("removing stale failed attempt for " + username);

                            failedAttempts.RemoveFirst();
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                // how many failed attempts in last N minutes?
                this.applicationLogger
                    .WriteToLog($"failed attempt count for {username}: {failedAttempts.Count}");

                if (failedAttempts.Count > failedAttemptsAllowed)
                {
                    this.applicationLogger
                        .WriteToLog("Too many failed login attempts in too short a time period: " + username);

                    return false;
                }

                // Save the list of attempts
                this.memoryCache
                    .Set(username, failedAttempts);
            }

            bool authenticated;

            if (this.applicationSettings.AuthenticateUsingLdap)
            {
                authenticated = false; //CheckPasswordWithLdap(username, password);
            }
            else
            {
                authenticated = CheckPasswordWithDb(username, password, dr);
            }

            if (authenticated)
            {
                // clear list of failed attempts
                if (failedAttempts != null)
                {
                    failedAttempts.Clear();

                    this.memoryCache
                        .Set(username, failedAttempts);
                }

                UpdateMostRecentLoginDateTime((int)dr["us_id"]);

                return true;
            }

            if (failedAttempts == null)
            {
                failedAttempts = new LinkedList<DateTime>();
            }

            // Record a failed login attempt.
            failedAttempts.AddLast(DateTime.Now);

            this.memoryCache
                .Set(username, failedAttempts);

            return false;
        }

        private bool CheckPasswordWithDb(string username, string enteredPassword, DataRow dr)
        {
            var salt = (string)dr["us_salt"];
            var hashedEnteredPassword = HashString(enteredPassword, salt);
            var databasePassword = (string)dr["us_password"];

            if (hashedEnteredPassword == databasePassword)
            {
                return true;
            }

            this.applicationLogger
                .WriteToLog($"User {username} entered an incorrect password.");

            return false;
        }

        //private bool CheckPasswordWithLdap(string username, string password)
        //{
        //    // allow multiple, seperated by a pipe character
        //    var dns = this.applicationSettings.LdapUserDistinguishedName;
        //    var dnArray = dns.Split('|');

        //    var ldapServer = this.applicationSettings.LdapServer;

        //    using (var ldap = new LdapConnection(ldapServer))
        //    {
        //        for (var i = 0; i < dnArray.Length; i++)
        //        {
        //            var dn = dnArray[i].Replace("$REPLACE_WITH_USERNAME$", username);
        //            var cred = new NetworkCredential(dn, password);

        //            ldap.AuthType = (AuthType)Enum.Parse
        //                (typeof(AuthType), this.applicationSettings.LdapAuthType);

        //            try
        //            {
        //                ldap.Bind(cred);
        //                this.applicationLogger
        //                    .WriteToLog($"LDAP authentication ok using {dn} for username: {username}");

        //                return true;
        //            }
        //            catch (Exception e)
        //            {
        //                var exceptionMsg = e.Message;

        //                if (e.InnerException != null)
        //                {
        //                    exceptionMsg += "\n";
        //                    exceptionMsg += e.InnerException.Message;
        //                }

        //                this.applicationLogger
        //                    .WriteToLog($"LDAP authentication failed using {dn}: {exceptionMsg}");
        //            }
        //        }
        //    }

        //    return false;
        //}

        //From Util
        private static string HashString(string password, string salt)
        {
            var k2 = new Rfc2898DeriveBytes(password, System.Text.Encoding.UTF8.GetBytes(salt + salt));
            var result = System.Text.Encoding.UTF8.GetString(k2.GetBytes(128));

            return result;
        }

        //From Util
        private void UpdateMostRecentLoginDateTime(int usId)
        {
            var sql = new SqlString(@"update users set us_most_recent_login_datetime = getdate() where us_id = @us");

            sql.AddParameterWithValue("us", usId);

            this.dbUtil
                .ExecuteNonQuery(sql);
        }

        private ClaimsIdentity GetIdentity(string username)
        {
            var sql = new SqlString(@"
                select
                    u.us_id,
                    u.us_username,
                    u.us_org,
                    u.us_bugs_per_page,
                    u.us_enable_bug_list_popups,
                    u.us_use_fckeditor,
                    u.us_forced_project,
                    u.us_email,
                    org.*,
                    isnull(u.us_forced_project, 0 ) us_forced_project,
                    proj.pu_permission_level,
                    isnull(proj.pu_admin, 0) pu_admin,
                    u.us_admin
                from users u
                    inner join orgs org 
                    on u.us_org = org.og_id

                    left outer join project_user_xref proj
                    on proj.pu_project = u.us_forced_project
                    and proj.pu_user = u.us_id
                where us_username = @us and u.us_active = 1");

            sql = sql.AddParameterWithValue("us", username);

            var dr = this.dbUtil
                .GetDataRow(sql);

            var bugsPerPage = dr["us_bugs_per_page"] == DBNull.Value
                ? 10
                : (int)dr["us_bugs_per_page"];

            var canAdd = true;
            var permissionLevel = dr["pu_permission_level"] == DBNull.Value
                ? (PermissionLevel)this.applicationSettings.DefaultPermissionLevel
                : (PermissionLevel)(int)dr["pu_permission_level"];

            // if user is forced to a specific project, and doesn't have
            // at least reporter permission on that project, than user
            // can't add bugs
            var forcedProjectId = dr["us_forced_project"] == DBNull.Value
                ? 0
                : (int)dr["us_forced_project"];

            if (forcedProjectId != 0)
            {
                if (permissionLevel == PermissionLevel.ReadOnly || permissionLevel == PermissionLevel.None)
                {
                    canAdd = false;
                }
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, Convert.ToString(dr["us_username"])),
                new Claim(ClaimTypes.Email, Convert.ToString(dr["us_email"])),

                new Claim(BtNetClaimType.UserId, Convert.ToString(dr["us_id"])),
                new Claim(BtNetClaimType.OrganizationId, Convert.ToString(dr["us_org"])),
                new Claim(BtNetClaimType.ForcedProjectId, Convert.ToString(forcedProjectId)),
                new Claim(BtNetClaimType.BugsPerPage, Convert.ToString(bugsPerPage)),
                new Claim(BtNetClaimType.EnablePopUps, Convert.ToString((int) dr["us_enable_bug_list_popups"] == 1)),
                new Claim(BtNetClaimType.UseFCKEditor, Convert.ToString((int) dr["us_use_fckeditor"] == 1)),
                new Claim(BtNetClaimType.CanAddBugs, Convert.ToString(canAdd)),

                new Claim(BtNetClaimType.OtherOrgsPermissionLevel, Convert.ToString(dr["og_other_orgs_permission_level"])),

                new Claim(BtNetClaimType.CanSearch, Convert.ToString((int) dr["og_can_search"] == 1)),
                new Claim(BtNetClaimType.IsExternalUser, Convert.ToString((int) dr["og_external_user"] == 1)),
                new Claim(BtNetClaimType.CanOnlySeeOwnReportedBugs, Convert.ToString((int) dr["og_can_only_see_own_reported"] == 1)),
                new Claim(BtNetClaimType.CanBeAssignedTo, Convert.ToString((int) dr["og_can_be_assigned_to"] == 1)),
                new Claim(BtNetClaimType.NonAdminsCanUse, Convert.ToString((int) dr["og_non_admins_can_use"] == 1)),

                new Claim(BtNetClaimType.ProjectFieldPermissionLevel, Convert.ToString(dr["og_project_field_permission_level"])),
                new Claim(BtNetClaimType.OrgFieldPermissionLevel, Convert.ToString(dr["og_org_field_permission_level"])),
                new Claim(BtNetClaimType.CategoryFieldPermissionLevel, Convert.ToString(dr["og_category_field_permission_level"])),
                new Claim(BtNetClaimType.PriorityFieldPermissionLevel, Convert.ToString(dr["og_priority_field_permission_level"])),
                new Claim(BtNetClaimType.StatusFieldPermissionLevel, Convert.ToString(dr["og_status_field_permission_level"])),
                new Claim(BtNetClaimType.AssignedToFieldPermissionLevel, Convert.ToString(dr["og_assigned_to_field_permission_level"])),
                new Claim(BtNetClaimType.UdfFieldPermissionLevel, Convert.ToString(dr["og_udf_field_permission_level"])),
                new Claim(BtNetClaimType.TagsFieldPermissionLevel, Convert.ToString(dr["og_tags_field_permission_level"])),

                new Claim(BtNetClaimType.CanEditSql, Convert.ToString((int) dr["og_can_edit_sql"] == 1)),
                new Claim(BtNetClaimType.CanDeleteBugs, Convert.ToString((int) dr["og_can_delete_bug"] == 1)),
                new Claim(BtNetClaimType.CanEditAndDeletePosts, Convert.ToString((int) dr["og_can_edit_and_delete_posts"] == 1)),
                new Claim(BtNetClaimType.CanMergeBugs, Convert.ToString((int) dr["og_can_merge_bugs"] == 1)),
                new Claim(BtNetClaimType.CanMassEditBugs, Convert.ToString((int) dr["og_can_mass_edit_bugs"] == 1)),
                new Claim(BtNetClaimType.CanUseReports, Convert.ToString((int) dr["og_can_use_reports"] == 1)),
                new Claim(BtNetClaimType.CanEditReports, Convert.ToString((int) dr["og_can_edit_reports"] == 1)),
                new Claim(BtNetClaimType.CanEditTasks, Convert.ToString((int) dr["og_can_edit_tasks"] == 1)),
                new Claim(BtNetClaimType.CanViewTasks, Convert.ToString((int) dr["og_can_view_tasks"] == 1)),
                new Claim(BtNetClaimType.CanAssignToInternalUsers, Convert.ToString((int) dr["og_can_assign_to_internal_users"] == 1))
            };


            //PermissionLevel tagsPermissionLevel;

            //if (this.applicationSettings.EnableTags)
            //{
            //    tagsPermissionLevel = (PermissionLevel)(int)dr["og_tags_field_permission_level"];
            //}
            //else
            //{
            //    tagsPermissionLevel = PermissionLevel.None;
            //}

            //claims.Add(new Claim(BtNetClaimType.TagsFieldPermissionLevel, Convert.ToString(tagsPermissionLevel)));

            if ((int)dr["us_admin"] == 1)
            {
                claims.Add(new Claim(ClaimTypes.Role, BtNetRole.Administrator));
            }
            else
            {
                if ((int)dr["pu_admin"] > 0)
                {
                    claims.Add(new Claim(ClaimTypes.Role, BtNetRole.ProjectAdministrator));
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, BtNetRole.Guest));
                }
            }

            claims.Add(new Claim(ClaimTypes.Role, BtNetRole.User));

            return new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme, ClaimTypes.Name, ClaimTypes.Role);
        }
    }
}