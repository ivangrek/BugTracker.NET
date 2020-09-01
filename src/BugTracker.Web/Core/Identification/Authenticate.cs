namespace BugTracker.Web.Core.Identification
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.DirectoryServices.Protocols;
    using System.Net;
    using System.Security.Claims;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Caching.Memory;

    public interface IAuthenticate
    {
        Task SignInAsync(string username, string password, bool persistent, bool asGuest);

        Task SignOutAsync();

        void UpdateUserPassword(int userId, string password);

        bool CheckPasswordStrength(string password);
    }

    internal sealed class Authenticate : IAuthenticate
    {
        private const string GuestLogin = "guest";

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
                username = GuestLogin;

                var account = FindAccount(username);

                if (account == null)
                {
                    this.applicationLogger
                        .WriteToLog($"Unknown user {username} attempted to login.");

                    throw new InvalidOperationException("Guest not exist.");
                }

                var usActive = (int)account["us_active"];

                if (usActive == 0)
                {
                    throw new InvalidOperationException("Guest not active.");
                }
            }
            else
            {
                var unlocked = CheckFailedAttempts(username);

                if (!unlocked)
                {
                    throw new InvalidOperationException("User login blocked.");
                }

                var authenticated = AuthenticateUser(username, password);

                if (!authenticated)
                {
                    AddFailedAttempts(username);

                    //result.Success = false;
                    //result.ErrorMessage = "Invalid User or Password.";
                    throw new InvalidOperationException("Invalid User or Password.");
                }

                ClearFailedAttempts(username);
            }

            var user = FindUser(username);
            var identity = CreateIdentity(user);
            var authenticationProperties = new AuthenticationProperties
            {
                IsPersistent = persistent && !asGuest
            };

            await this.httpContextAccessor
                .HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    authenticationProperties);

            UpdateMostRecentLoginDateTime((int)user["us_id"]);
        }

        public async Task SignOutAsync()
        {
            await this.httpContextAccessor
                .HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        }

        //From Util
        public void UpdateUserPassword(int userId, string password)
        {
            var salt = GenerateRandomString();
            var hashed = HashString(password, Convert.ToString(salt));
            var sql = new SqlString("update users set us_password = @hashed, us_salt = @salt where us_id = @id");

            sql = sql.AddParameterWithValue("hashed", hashed);
            sql = sql.AddParameterWithValue("salt", salt);
            sql = sql.AddParameterWithValue("id", userId);

            this.dbUtil
                .ExecuteNonQuery(sql);
        }

        //From Util
        public bool CheckPasswordStrength(string password)
        {
            if (!applicationSettings.RequireStrongPasswords)
            {
                return true;
            }

            if (password.Length < 8) return false;
            if (password.IndexOf("password") > -1) return false;
            if (password.IndexOf("123") > -1) return false;
            if (password.IndexOf("asdf") > -1) return false;
            if (password.IndexOf("qwer") > -1) return false;
            if (password.IndexOf("test") > -1) return false;

            var lowercase = 0;
            var uppercase = 0;
            var digits = 0;
            var specialChars = 0;

            for (var i = 0; i < password.Length; i++)
            {
                var c = password[i];
                if (c >= 'a' && c <= 'z') lowercase = 1;
                else if (c >= 'A' && c <= 'Z') uppercase = 1;
                else if (c >= '0' && c <= '9') digits = 1;
                else specialChars = 1;
            }

            if (lowercase + uppercase + digits + specialChars < 2) return false;

            return true;
        }

        private DataRow FindAccount(string username)
        {
            var sql = new SqlString(@"
                select
                    us_id,
                    us_username,
                    us_password,
                    isnull(us_salt, 0) us_salt,
                    us_active
                from
                    users
                where
                    us_username = @username");

            sql = sql.AddParameterWithValue("username", username);

            return this.dbUtil
                .GetDataRow(sql);
        }

        private bool AuthenticateUser(string username, string password)
        {
            var account = FindAccount(username);

            if (account == null)
            {
                this.applicationLogger
                    .WriteToLog($"Unknown user {username} attempted to login.");

                return false;
            }

            var usActive = (int)account["us_active"];

            if (usActive == 0)
            {
                this.applicationLogger
                    .WriteToLog($"Inactive user {username} attempted to login.");

                return false;
            }

            if (this.applicationSettings.AuthenticateUsingLdap)
            {
                return AuthenticateUserWithLdap(username, password);
            }

            return AuthenticateUserWithDb(username, password, account);
        }

        private bool AuthenticateUserWithLdap(string username, string password)
        {
            // allow multiple, seperated by a pipe character
            var dns = this.applicationSettings.LdapUserDistinguishedName;
            var dnArray = dns.Split('|');

            var ldapServer = this.applicationSettings.LdapServer;
            var ldapDirectoryIdentifier = new LdapDirectoryIdentifier(ldapServer, /*389*/636);

            using var ldapConnection = new LdapConnection(ldapDirectoryIdentifier)
            {
                AuthType = (AuthType)Enum.Parse(typeof(AuthType), this.applicationSettings.LdapAuthType),
                SessionOptions =
                {
                    ProtocolVersion = 3,
                    SecureSocketLayer = true
                }
            };

            ldapConnection.SessionOptions.VerifyServerCertificate += (LdapConnection connection, X509Certificate certificate) => { return true; };

            for (var i = 0; i < dnArray.Length; i++)
            {
                var dn = dnArray[i].Replace("$REPLACE_WITH_USERNAME$", username);
                var credential = new NetworkCredential(dn, password);

                try
                {
                    ldapConnection.Bind(credential);

                    this.applicationLogger
                        .WriteToLog($"LDAP authentication ok using {dn} for username: {username}");

                    return true;
                }
                catch (LdapException e)
                {
                    this.applicationLogger
                        .WriteToLog($"LDAP authentication failed using {dn}: {e}");
                }
                catch (Exception e)
                {
                    this.applicationLogger
                        .WriteToLog($"LDAP authentication failed using {dn}: {e}");
                }
            }

            return false;
        }

        private bool AuthenticateUserWithDb(string username, string password, DataRow account)
        {
            var salt = (string)account["us_salt"];
            var hashedPassword = HashString(password, salt);
            var databasePassword = (string)account["us_password"];

            if (hashedPassword == databasePassword)
            {
                return true;
            }

            this.applicationLogger
                .WriteToLog($"User {username} entered an incorrect password.");

            return false;
        }

        private bool CheckFailedAttempts(string username)
        {
            // Too many failed attempts?
            // We'll only allow N in the last N minutes.
            var failedAttempts = this.memoryCache
                .GetOrCreate<LinkedList<DateTime>>(username, x => new LinkedList<DateTime>());

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
                            .WriteToLog($"Removing stale failed attempt for {username}");

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
                .WriteToLog($"Failed attempt count for {username}: {failedAttempts.Count}");

            if (failedAttempts.Count > failedAttemptsAllowed)
            {
                this.applicationLogger
                    .WriteToLog($"Too many failed login attempts in too short a time period: {username}");

                return false;
            }

            // Save the list of attempts
            this.memoryCache
                .Set(username, failedAttempts);

            return true;
        }

        private void ClearFailedAttempts(string username)
        {
            // clear list of failed attempts
            var failedAttempts = this.memoryCache
                .GetOrCreate<LinkedList<DateTime>>(username, x => new LinkedList<DateTime>());

            failedAttempts.Clear();

            this.memoryCache
                .Set(username, failedAttempts);
        }

        private void AddFailedAttempts(string username)
        {
            var failedAttempts = this.memoryCache
                .GetOrCreate<LinkedList<DateTime>>(username, x => new LinkedList<DateTime>());

            // Record a failed login attempt.
            failedAttempts.AddLast(DateTime.Now);

            this.memoryCache
                .Set(username, failedAttempts);
        }

        //From Util
        private static string HashString(string password, string salt)
        {
            var k2 = new Rfc2898DeriveBytes(password, Encoding.UTF8.GetBytes(salt + salt));
            var result = Encoding.UTF8.GetString(k2.GetBytes(128));

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

        //From Util
        private string GenerateRandomString()
        {
            using var random = RandomNumberGenerator.Create();
            var value = new byte[100];

            random.GetBytes(value);

            return Encoding.UTF8.GetString(value);

            //var characters = "ABCDEFGHIJKLMNOPQURSTUVWXYZabcdefghijklmnopqurtuvwxyz1234567890".ToCharArray();
            //var builder = new StringBuilder();

            //for (var i = 0; i < _random.Next(10, 100); i++)
            //{
            //    builder.Append(characters[_random.Next(characters.Length - 1)]);
            //}

            //return builder.ToString();
        }

        private DataRow FindUser(string username)
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
                    u.us_admin,
                    u.us_active
                from users u
                    inner join orgs org 
                    on u.us_org = org.og_id

                    left outer join project_user_xref proj
                    on proj.pu_project = u.us_forced_project
                    and proj.pu_user = u.us_id
                where
                    us_username = @us
                    and
                    u.us_active = 1");

            sql = sql.AddParameterWithValue("us", username);

            return this.dbUtil
                .GetDataRow(sql);
        }

        private ClaimsIdentity CreateIdentity(DataRow user)
        {
            var bugsPerPage = user["us_bugs_per_page"] == DBNull.Value
                ? 10
                : (int)user["us_bugs_per_page"];

            var canAdd = true;
            var permissionLevel = user["pu_permission_level"] == DBNull.Value
                ? (PermissionLevel)this.applicationSettings.DefaultPermissionLevel
                : (PermissionLevel)(int)user["pu_permission_level"];

            // if user is forced to a specific project, and doesn't have
            // at least reporter permission on that project, than user
            // can't add bugs
            var forcedProjectId = user["us_forced_project"] == DBNull.Value
                ? 0
                : (int)user["us_forced_project"];

            if (forcedProjectId != 0)
            {
                if (permissionLevel == PermissionLevel.ReadOnly || permissionLevel == PermissionLevel.None)
                {
                    canAdd = false;
                }
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, Convert.ToString(user["us_username"])),
                new Claim(ClaimTypes.Email, Convert.ToString(user["us_email"])),

                new Claim(BtNetClaimType.UserId, Convert.ToString(user["us_id"])),
                new Claim(BtNetClaimType.OrganizationId, Convert.ToString(user["us_org"])),
                new Claim(BtNetClaimType.ForcedProjectId, Convert.ToString(forcedProjectId)),
                new Claim(BtNetClaimType.BugsPerPage, Convert.ToString(bugsPerPage)),
                new Claim(BtNetClaimType.EnablePopUps, Convert.ToString((int) user["us_enable_bug_list_popups"] == 1)),
                new Claim(BtNetClaimType.UseFCKEditor, Convert.ToString((int) user["us_use_fckeditor"] == 1)),
                new Claim(BtNetClaimType.CanAddBugs, Convert.ToString(canAdd)),

                new Claim(BtNetClaimType.OtherOrgsPermissionLevel, Convert.ToString(user["og_other_orgs_permission_level"])),

                new Claim(BtNetClaimType.CanSearch, Convert.ToString((int) user["og_can_search"] == 1)),
                new Claim(BtNetClaimType.IsExternalUser, Convert.ToString((int) user["og_external_user"] == 1)),
                new Claim(BtNetClaimType.CanOnlySeeOwnReportedBugs, Convert.ToString((int) user["og_can_only_see_own_reported"] == 1)),
                new Claim(BtNetClaimType.CanBeAssignedTo, Convert.ToString((int) user["og_can_be_assigned_to"] == 1)),
                new Claim(BtNetClaimType.NonAdminsCanUse, Convert.ToString((int) user["og_non_admins_can_use"] == 1)),

                new Claim(BtNetClaimType.ProjectFieldPermissionLevel, Convert.ToString(user["og_project_field_permission_level"])),
                new Claim(BtNetClaimType.OrgFieldPermissionLevel, Convert.ToString(user["og_org_field_permission_level"])),
                new Claim(BtNetClaimType.CategoryFieldPermissionLevel, Convert.ToString(user["og_category_field_permission_level"])),
                new Claim(BtNetClaimType.PriorityFieldPermissionLevel, Convert.ToString(user["og_priority_field_permission_level"])),
                new Claim(BtNetClaimType.StatusFieldPermissionLevel, Convert.ToString(user["og_status_field_permission_level"])),
                new Claim(BtNetClaimType.AssignedToFieldPermissionLevel, Convert.ToString(user["og_assigned_to_field_permission_level"])),
                new Claim(BtNetClaimType.UdfFieldPermissionLevel, Convert.ToString(user["og_udf_field_permission_level"])),
                new Claim(BtNetClaimType.TagsFieldPermissionLevel, Convert.ToString(user["og_tags_field_permission_level"])),

                new Claim(BtNetClaimType.CanEditSql, Convert.ToString((int) user["og_can_edit_sql"] == 1)),
                new Claim(BtNetClaimType.CanDeleteBugs, Convert.ToString((int) user["og_can_delete_bug"] == 1)),
                new Claim(BtNetClaimType.CanEditAndDeletePosts, Convert.ToString((int) user["og_can_edit_and_delete_posts"] == 1)),
                new Claim(BtNetClaimType.CanMergeBugs, Convert.ToString((int) user["og_can_merge_bugs"] == 1)),
                new Claim(BtNetClaimType.CanMassEditBugs, Convert.ToString((int) user["og_can_mass_edit_bugs"] == 1)),
                new Claim(BtNetClaimType.CanUseReports, Convert.ToString((int) user["og_can_use_reports"] == 1)),
                new Claim(BtNetClaimType.CanEditReports, Convert.ToString((int) user["og_can_edit_reports"] == 1)),
                new Claim(BtNetClaimType.CanEditTasks, Convert.ToString((int) user["og_can_edit_tasks"] == 1)),
                new Claim(BtNetClaimType.CanViewTasks, Convert.ToString((int) user["og_can_view_tasks"] == 1)),
                new Claim(BtNetClaimType.CanAssignToInternalUsers, Convert.ToString((int) user["og_can_assign_to_internal_users"] == 1))
            };


            //PermissionLevel tagsPermissionLevel;

            //if (this.applicationSettings.EnableTags)
            //{
            //    tagsPermissionLevel = (PermissionLevel)(int)user["og_tags_field_permission_level"];
            //}
            //else
            //{
            //    tagsPermissionLevel = PermissionLevel.None;
            //}

            //claims.Add(new Claim(BtNetClaimType.TagsFieldPermissionLevel, Convert.ToString(tagsPermissionLevel)));

            if ((int)user["us_admin"] == 1)
            {
                claims.Add(new Claim(ClaimTypes.Role, BtNetRole.Administrator));
            }
            else
            {
                if ((int)user["pu_admin"] > 0)
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