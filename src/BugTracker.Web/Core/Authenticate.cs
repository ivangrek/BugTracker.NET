/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.DirectoryServices.Protocols;
    using System.Net;
    using System.Web;

    public class Authenticate
    {
        public static bool CheckPassword(string username, string password)
        {
            var sql = @"
select us_username, us_id, us_password, isnull(us_salt,0) us_salt, us_active
from users
where us_username = N'$username'";

            sql = sql.Replace("$username", username.Replace("'", "''"));

            var dr = DbUtil.GetDataRow(sql);

            if (dr == null)
            {
                Util.WriteToLog("Unknown user " + username + " attempted to login.");
                return false;
            }

            var usActive = (int) dr["us_active"];

            if (usActive == 0)
            {
                Util.WriteToLog("Inactive user " + username + " attempted to login.");
                return false;
            }

            var authenticated = false;
            LinkedList<DateTime> failedAttempts = null;

            // Too many failed attempts?
            // We'll only allow N in the last N minutes.
            failedAttempts = (LinkedList<DateTime>) HttpRuntime.Cache[username];

            if (failedAttempts != null)
            {
                // Don't count attempts older than N minutes ago.
                var minutesAgo = Convert.ToInt32(Util.GetSetting("FailedLoginAttemptsMinutes", "10"));
                var failedAttemptsAllowed = Convert.ToInt32(Util.GetSetting("FailedLoginAttemptsAllowed", "10"));

                var nMinutesAgo = DateTime.Now.AddMinutes(-1 * minutesAgo);
                while (true)
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

                // how many failed attempts in last N minutes?
                Util.WriteToLog(
                    "failed attempt count for " + username + ":" + Convert.ToString(failedAttempts.Count));

                if (failedAttempts.Count > failedAttemptsAllowed)
                {
                    Util.WriteToLog("Too many failed login attempts in too short a time period: " + username);
                    return false;
                }

                // Save the list of attempts
                HttpRuntime.Cache[username] = failedAttempts;
            }

            if (Util.GetSetting("AuthenticateUsingLdap", "0") == "1")
                authenticated = CheckPasswordWithLdap(username, password);
            else
                authenticated = CheckPasswordWithDb(username, password, dr);

            if (authenticated)
            {
                // clear list of failed attempts
                if (failedAttempts != null)
                {
                    failedAttempts.Clear();
                    HttpRuntime.Cache[username] = failedAttempts;
                }

                Util.UpdateMostRecentLoginDateTime((int) dr["us_id"]);
                return true;
            }

            if (failedAttempts == null) failedAttempts = new LinkedList<DateTime>();

            // Record a failed login attempt.
            failedAttempts.AddLast(DateTime.Now);
            HttpRuntime.Cache[username] = failedAttempts;

            return false;
        }

        public static bool CheckPasswordWithLdap(string username, string password)
        {
            // allow multiple, seperated by a pipe character
            var dns = Util.GetSetting(
                "LdapUserDistinguishedName",
                "uid=$REPLACE_WITH_USERNAME$,ou=people,dc=mycompany,dc=com");

            var dnArray = dns.Split('|');

            var ldapServer = Util.GetSetting(
                "LdapServer",
                "127.0.0.1");

            using (var ldap = new LdapConnection(ldapServer))
            {
                for (var i = 0; i < dnArray.Length; i++)
                {
                    var dn = dnArray[i].Replace("$REPLACE_WITH_USERNAME$", username);

                    var cred = new NetworkCredential(dn, password);

                    ldap.AuthType = (AuthType) Enum.Parse
                    (typeof(AuthType),
                        Util.GetSetting("LdapAuthType", "Basic"));

                    try
                    {
                        ldap.Bind(cred);
                        Util.WriteToLog("LDAP authentication ok using " + dn + " for username: " + username);
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

                        Util.WriteToLog("LDAP authentication failed using " + dn + ": " + exceptionMsg);
                    }
                }
            }

            return false;
        }

        public static bool CheckPasswordWithDb(string username, string password, DataRow dr)
        {
            var usSalt = (int) dr["us_salt"];

            string encrypted;

            var usPassword = (string) dr["us_password"];

            if (usPassword.Length < 32) // if password in db is unencrypted
                encrypted = password; // in other words, unecrypted
            else if (usSalt == 0)
                encrypted = Util.EncryptStringUsingMd5(password);
            else
                encrypted = Util.EncryptStringUsingMd5(password + Convert.ToString(usSalt));

            if (encrypted == usPassword)
            {
                // Authenticated, but let's do a better job encrypting the password.
                // If it is not encrypted, or, if it is encrypted without salt, then
                // update it so that it is encrypted WITH salt.
                if (usSalt == 0 || usPassword.Length < 32) Util.UpdateUserPassword((int) dr["us_id"], password);
                return true;
            }

            Util.WriteToLog("User " + username + " entered an incorrect password.");
            return false;
        }
    }
}