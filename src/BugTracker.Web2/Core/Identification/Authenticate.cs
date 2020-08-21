/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Identification
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.DirectoryServices.Protocols;
    using System.Net;
    using System.Security.Claims;
    using System.Web;
    using Microsoft.Owin.Security;

    public interface IAuthenticate
    {
        bool CheckPassword(string username, string password);

        void SignIn(string username, bool persistent);

        void SignOut();
    }

    internal sealed class Authenticate : IAuthenticate
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly IAuthenticationManager authenticationManager;

        public Authenticate(
            IApplicationSettings applicationSettings,
            IAuthenticationManager authenticationManager)
        {
            this.applicationSettings = applicationSettings;
            this.authenticationManager = authenticationManager;
        }

        public bool CheckPassword(string username, string password)
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

            var dr = DbUtil.GetDataRow(sql);

            if (dr == null)
            {
                Util.WriteToLog($"Unknown user {username} attempted to login.");

                return false;
            }

            var usActive = (int)dr["us_active"];

            if (usActive == 0)
            {
                Util.WriteToLog($"Inactive user {username} attempted to login.");

                return false;
            }

            // Too many failed attempts?
            // We'll only allow N in the last N minutes.
            var failedAttempts = (LinkedList<DateTime>)HttpRuntime.Cache[username];

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
                            Util.WriteToLog("removing stale failed attempt for " + username);
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
                Util.WriteToLog($"failed attempt count for {username}: {failedAttempts.Count}");

                if (failedAttempts.Count > failedAttemptsAllowed)
                {
                    Util.WriteToLog("Too many failed login attempts in too short a time period: " + username);

                    return false;
                }

                // Save the list of attempts
                HttpRuntime.Cache[username] = failedAttempts;
            }

            bool authenticated;

            if (this.applicationSettings.AuthenticateUsingLdap)
            {
                authenticated = CheckPasswordWithLdap(username, password);
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
                    HttpRuntime.Cache[username] = failedAttempts;
                }

                Util.UpdateMostRecentLoginDateTime((int)dr["us_id"]);

                return true;
            }

            if (failedAttempts == null)
            {
                failedAttempts = new LinkedList<DateTime>();
            }

            // Record a failed login attempt.
            failedAttempts.AddLast(DateTime.Now);
            HttpRuntime.Cache[username] = failedAttempts;

            return false;
        }

        private bool CheckPasswordWithLdap(string username, string password)
        {
            // allow multiple, seperated by a pipe character
            var dns = this.applicationSettings.LdapUserDistinguishedName;
            var dnArray = dns.Split('|');

            var ldapServer = this.applicationSettings.LdapServer;

            using (var ldap = new LdapConnection(ldapServer))
            {
                for (var i = 0; i < dnArray.Length; i++)
                {
                    var dn = dnArray[i].Replace("$REPLACE_WITH_USERNAME$", username);
                    var cred = new NetworkCredential(dn, password);

                    ldap.AuthType = (AuthType)Enum.Parse
                    (typeof(AuthType), this.applicationSettings.LdapAuthType);

                    try
                    {
                        ldap.Bind(cred);
                        Util.WriteToLog($"LDAP authentication ok using {dn} for username: {username}");

                        return true;
                    }
                    catch (Exception e)
                    {
                        var exceptionMsg = e.Message;

                        if (e.InnerException != null)
                        {
                            exceptionMsg += "\n";
                            exceptionMsg += e.InnerException.Message;
                        }

                        Util.WriteToLog($"LDAP authentication failed using {dn}: {exceptionMsg}");
                    }
                }
            }

            return false;
        }

        public static bool CheckPasswordWithDb(string username, string enteredPassword, DataRow dr)
        {
            var salt = (string)dr["us_salt"];
            var hashedEnteredPassword = Util.HashString(enteredPassword, salt);
            var databasePassword = (string)dr["us_password"];

            if (hashedEnteredPassword == databasePassword)
            {
                return true;
            }

            Util.WriteToLog($"User {username} entered an incorrect password.");

            return false;
        }

        public void SignIn(string username, bool persistent)
        {
            var properties = new AuthenticationProperties
            {
                IsPersistent = persistent
            };

            var sql = new SqlString(@"
                select
                    us_id,
                    us_username,
                    us_admin,
                    isnull(proj.pu_admin, 0) pu_admin
                from
                    users

                    left outer join
                        project_user_xref proj
                    on
                        proj.pu_project = us_forced_project
                        and
                        proj.pu_user = us_id
                where
                    us_username = @us
                    and
                    us_active = 1");

            sql = sql.AddParameterWithValue("us", username);

            var dr = DbUtil.GetDataRow(sql);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, Convert.ToString(dr["us_id"])),
                new Claim(ClaimTypes.Name, Convert.ToString(dr["us_username"])),
            };

            if (username == "guest")
            {
                claims.Add(new Claim(ClaimTypes.Role, ApplicationRole.Guest));
            }
            else
            {
                if ((int)dr["us_admin"] == 1)
                {
                    claims.Add(new Claim(ClaimTypes.Role, ApplicationRole.Administrator));
                }
                else
                {
                    if ((int)dr["pu_admin"] > 0)
                    {
                        claims.Add(new Claim(ClaimTypes.Role, ApplicationRole.ProjectAdministrator));
                    }
                }
            }

            claims.Add(new Claim(ClaimTypes.Role, ApplicationRole.Member));

            var identity = new ClaimsIdentity(claims, "ApplicationCookie", ClaimTypes.Name, ClaimTypes.Role);

            this.authenticationManager
                .SignIn(properties, identity);
        }

        public void SignOut()
        {
            this.authenticationManager
                .SignOut();
        }
    }
}