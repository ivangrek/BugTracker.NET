/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Accounts
{
    using System;
    using System.DirectoryServices;
    using System.Web.UI;
    using Core;

    public partial class LoginNt : Page
    {
        public string Sql;

        public void Page_Load(object sender, EventArgs e)
        {
            DbUtil.GetSqlConnection();

            Util.DoNotCache(Response);

            // Get authentication mode
            var authMode = Util.GetSetting("WindowsAuthentication", "0");

            // If manual authentication only, we shouldn't be here, so redirect to manual screen

            if (authMode == "0") Util.Redirect("~/Accounts/Login.aspx", Request, Response);

            // Get the logon user from IIS
            var domainWindowsUsername = Request.ServerVariables["LOGON_USER"];

            if (domainWindowsUsername == "")
            {
                // If the logon user is blank, then the page is misconfigured
                // in IIS. Do nothing and let the HTML display.
            }
            else
            {
                // Extract the user name from the logon ID
                var pos = domainWindowsUsername.IndexOf("\\") + 1;
                var windowsUsername =
                    domainWindowsUsername.Substring(pos, domainWindowsUsername.Length - pos);

                // Fetch the user's information from the users table
                this.Sql = @"select us_id, us_username
			from users
			where us_username = N'$us'
			and us_active = 1";
                this.Sql = this.Sql.Replace("$us", windowsUsername.Replace("'", "''"));

                var dr = DbUtil.GetDataRow(this.Sql);
                if (dr != null)
                {
                    // The user was found, so bake a cookie and redirect
                    var userid = (int) dr["us_id"];
                    Security.CreateSession(
                        Request,
                        Response,
                        userid,
                        (string) dr["us_username"],
                        "1");

                    Util.UpdateMostRecentLoginDateTime(userid);

                    Util.Redirect(Request, Response);
                }

                // Is self register enabled for users authenticated by windows?
                // If yes, then automatically insert a row in the user table
                var enableAutoRegistration = Util.GetSetting("EnableWindowsUserAutoRegistration", "1") == "1";
                if (enableAutoRegistration)
                {
                    var templateUser = Util.GetSetting("WindowsUserAutoRegistrationUserTemplate", "guest");

                    var firstName = windowsUsername;
                    var lastName = windowsUsername;
                    var signature = windowsUsername;
                    var email = string.Empty;

                    // From the browser, we only know the Windows username.  Maybe we can get the other
                    // info from LDAP?
                    if (Util.GetSetting("EnableWindowsUserAutoRegistrationLdapSearch", "0") == "1")
                        using (var de = new DirectoryEntry())
                        {
                            de.Path = Util.GetSetting("LdapDirectoryEntryPath",
                                "LDAP://127.0.0.1/DC=mycompany,DC=com");

                            de.AuthenticationType =
                                (AuthenticationTypes) Enum.Parse(
                                    typeof(AuthenticationTypes),
                                    Util.GetSetting("LdapDirectoryEntryAuthenticationType", "Anonymous"));

                            de.Username = Util.GetSetting("LdapDirectoryEntryUsername", "");
                            de.Password = Util.GetSetting("LdapDirectoryEntryPassword", "");

                            using (var search =
                                new DirectorySearcher(de))
                            {
                                var searchFilter = Util.GetSetting("LdapDirectorySearcherFilter",
                                    "(uid=$REPLACE_WITH_USERNAME$)");
                                search.Filter = searchFilter.Replace("$REPLACE_WITH_USERNAME$", windowsUsername);
                                SearchResult result = null;

                                try
                                {
                                    result = search.FindOne();
                                    if (result != null)
                                    {
                                        firstName = get_ldap_property_value(result,
                                            Util.GetSetting("LdapFirstName", "gn"), firstName);
                                        lastName = get_ldap_property_value(result,
                                            Util.GetSetting("LdapLastName", "sn"),
                                            lastName);
                                        email = get_ldap_property_value(result, Util.GetSetting("LdapEmail", "mail"),
                                            email);
                                        ;
                                        signature = get_ldap_property_value(result,
                                            Util.GetSetting("LdapEmailSigniture", "cn"), signature);
                                        ;
                                    }
                                    else
                                    {
                                        Util.WriteToLog("LDAP search.FindOne() result = null");
                                    }
                                }
                                catch (Exception e2)
                                {
                                    var s = e2.Message;

                                    if (e2.InnerException != null)
                                    {
                                        s += "\n";
                                        s += e2.InnerException.Message;
                                    }

                                    // write the message to the log
                                    Util.WriteToLog("LDAP search failed: " + s);
                                }
                            }
                        }

                    var newUserId = Core.User.CopyUser(
                        windowsUsername,
                        email,
                        firstName,
                        lastName,
                        signature,
                        0, // salt
                        Guid.NewGuid().ToString(), // random value for password
                        templateUser,
                        false);

                    if (newUserId > 0) // automatically created the user
                    {
                        // The user was created, so bake a cookie and redirect
                        Security.CreateSession(
                            Request,
                            Response,
                            newUserId,
                            windowsUsername.Replace("'", "''"),
                            "1");

                        Util.UpdateMostRecentLoginDateTime(newUserId);

                        Util.Redirect(Request, Response);
                    }
                }

                // Try fetching the guest user.
                this.Sql = @"select us_id, us_username
			from users
			where us_username = 'guest'
			and us_active = 1";

                dr = DbUtil.GetDataRow(this.Sql);
                if (dr != null)
                {
                    // The Guest user was found, so bake a cookie and redirect
                    var userid = (int) dr["us_id"];
                    Security.CreateSession(
                        Request,
                        Response,
                        userid,
                        (string) dr["us_username"],
                        "1");

                    Util.UpdateMostRecentLoginDateTime(userid);

                    Util.Redirect(Request, Response);
                }

                // If using mixed-mode authentication and we got this far,
                // then we can't sign in using integrated security. Redirect
                // to the manual screen.
                if (authMode != "1") Util.Redirect("~/Accounts/Login.aspx?msg=user+not+valid", Request, Response);

                // If we are still here, then toss a 401 error.
                Response.StatusCode = 401;
                Response.End();
            }
        }

        public string get_ldap_property_value(SearchResult result, string propertyName, string defaultValue)
        {
            var values = result.Properties[propertyName];
            if (values != null && values.Count == 1 && values[0] is string)
                return (string) values[0];
            return defaultValue;
        }
    }
}