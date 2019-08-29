/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.DirectoryServices;
    using System.Web.UI;
    using Core;

    public partial class loginnt : Page
    {
        public string sql;

        public void Page_Load(object sender, EventArgs e)
        {
            DbUtil.get_sqlconnection();

            Util.do_not_cache(Response);

            // Get authentication mode
            var auth_mode = Util.get_setting("WindowsAuthentication", "0");

            // If manual authentication only, we shouldn't be here, so redirect to manual screen

            if (auth_mode == "0") Util.redirect("default.aspx", Request, Response);

            // Get the logon user from IIS
            var domain_windows_username = Request.ServerVariables["LOGON_USER"];

            if (domain_windows_username == "")
            {
                // If the logon user is blank, then the page is misconfigured
                // in IIS. Do nothing and let the HTML display.
            }
            else
            {
                // Extract the user name from the logon ID
                var pos = domain_windows_username.IndexOf("\\") + 1;
                var windows_username =
                    domain_windows_username.Substring(pos, domain_windows_username.Length - pos);

                // Fetch the user's information from the users table
                this.sql = @"select us_id, us_username
			from users
			where us_username = N'$us'
			and us_active = 1";
                this.sql = this.sql.Replace("$us", windows_username.Replace("'", "''"));

                var dr = DbUtil.get_datarow(this.sql);
                if (dr != null)
                {
                    // The user was found, so bake a cookie and redirect
                    var userid = (int) dr["us_id"];
                    Security.create_session(
                        Request,
                        Response,
                        userid,
                        (string) dr["us_username"],
                        "1");

                    Util.update_most_recent_login_datetime(userid);

                    Util.redirect(Request, Response);
                }

                // Is self register enabled for users authenticated by windows?
                // If yes, then automatically insert a row in the user table
                var enable_auto_registration = Util.get_setting("EnableWindowsUserAutoRegistration", "1") == "1";
                if (enable_auto_registration)
                {
                    var template_user = Util.get_setting("WindowsUserAutoRegistrationUserTemplate", "guest");

                    var first_name = windows_username;
                    var last_name = windows_username;
                    var signature = windows_username;
                    var email = string.Empty;

                    // From the browser, we only know the Windows username.  Maybe we can get the other
                    // info from LDAP?
                    if (Util.get_setting("EnableWindowsUserAutoRegistrationLdapSearch", "0") == "1")
                        using (var de = new DirectoryEntry())
                        {
                            de.Path = Util.get_setting("LdapDirectoryEntryPath",
                                "LDAP://127.0.0.1/DC=mycompany,DC=com");

                            de.AuthenticationType =
                                (AuthenticationTypes) Enum.Parse(
                                    typeof(AuthenticationTypes),
                                    Util.get_setting("LdapDirectoryEntryAuthenticationType", "Anonymous"));

                            de.Username = Util.get_setting("LdapDirectoryEntryUsername", "");
                            de.Password = Util.get_setting("LdapDirectoryEntryPassword", "");

                            using (var search =
                                new DirectorySearcher(de))
                            {
                                var search_filter = Util.get_setting("LdapDirectorySearcherFilter",
                                    "(uid=$REPLACE_WITH_USERNAME$)");
                                search.Filter = search_filter.Replace("$REPLACE_WITH_USERNAME$", windows_username);
                                SearchResult result = null;

                                try
                                {
                                    result = search.FindOne();
                                    if (result != null)
                                    {
                                        first_name = get_ldap_property_value(result,
                                            Util.get_setting("LdapFirstName", "gn"), first_name);
                                        last_name = get_ldap_property_value(result,
                                            Util.get_setting("LdapLastName", "sn"),
                                            last_name);
                                        email = get_ldap_property_value(result, Util.get_setting("LdapEmail", "mail"),
                                            email);
                                        ;
                                        signature = get_ldap_property_value(result,
                                            Util.get_setting("LdapEmailSigniture", "cn"), signature);
                                        ;
                                    }
                                    else
                                    {
                                        Util.write_to_log("LDAP search.FindOne() result = null");
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
                                    Util.write_to_log("LDAP search failed: " + s);
                                }
                            }
                        }

                    var new_user_id = Core.User.copy_user(
                        windows_username,
                        email,
                        first_name,
                        last_name,
                        signature,
                        0, // salt
                        Guid.NewGuid().ToString(), // random value for password
                        template_user,
                        false);

                    if (new_user_id > 0) // automatically created the user
                    {
                        // The user was created, so bake a cookie and redirect
                        Security.create_session(
                            Request,
                            Response,
                            new_user_id,
                            windows_username.Replace("'", "''"),
                            "1");

                        Util.update_most_recent_login_datetime(new_user_id);

                        Util.redirect(Request, Response);
                    }
                }

                // Try fetching the guest user.
                this.sql = @"select us_id, us_username
			from users
			where us_username = 'guest'
			and us_active = 1";

                dr = DbUtil.get_datarow(this.sql);
                if (dr != null)
                {
                    // The Guest user was found, so bake a cookie and redirect
                    var userid = (int) dr["us_id"];
                    Security.create_session(
                        Request,
                        Response,
                        userid,
                        (string) dr["us_username"],
                        "1");

                    Util.update_most_recent_login_datetime(userid);

                    Util.redirect(Request, Response);
                }

                // If using mixed-mode authentication and we got this far,
                // then we can't sign in using integrated security. Redirect
                // to the manual screen.
                if (auth_mode != "1") Util.redirect("default.aspx?msg=user+not+valid", Request, Response);

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