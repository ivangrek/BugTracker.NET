/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Controllers
{
    using Core;
    using Core.Controls;
    using Models;
    using Models.Account;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.DirectoryServices;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;
    using Core.Identification;
    using Core.Mail;

    [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
    public class AccountController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;
        private readonly IAuthenticate authenticate;

        public AccountController(
            IApplicationSettings applicationSettings,
            ISecurity security,
            IAuthenticate authenticate)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
            this.authenticate = authenticate;
        }

        [HttpGet]
        public ActionResult Register()
        {
            if (!this.applicationSettings.AllowSelfRegistration)
            {
                return Content("Sorry, Web.config AllowSelfRegistration is set to 0");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - register"
            };

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterModel model)
        {
            if (!this.applicationSettings.AllowSelfRegistration)
            {
                return Content("Sorry, Web.config AllowSelfRegistration is set to 0");
            }

            if (!Util.CheckPasswordStrength(model.Password))
            {
                ModelState.AddModelError("Password", "Password is not difficult enough to guess.");
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Registration was not submitted.");

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - register"
                };

                return View(model);
            }

            var guid = Guid.NewGuid().ToString();

            // encrypt the password
            var random = new Random();
            var salt = random.Next(10000, 99999);
            var encrypted = Util.EncryptStringUsingMd5(model.Password + Convert.ToString(salt));

            var sql = new SqlString(@"
                insert into emailed_links
                (
                    el_id,
                    el_date,
                    el_email,
                    el_action,
                    el_username,
                    el_salt,
                    el_password,
                    el_firstname,
                    el_lastname
                )
                values
                (
                    @guid,
                    getdate(),
                    @email,
                    N'register',
                    @username,
                    @salt,
                    @password,
                    @firstname,
                    @lastname
                )");

            sql = sql.AddParameterWithValue("guid", guid);
            sql = sql.AddParameterWithValue("password", encrypted);
            sql = sql.AddParameterWithValue("salt", salt);
            sql = sql.AddParameterWithValue("username", model.Login);
            sql = sql.AddParameterWithValue("email", model.Email);
            sql = sql.AddParameterWithValue("firstname", model.FirstName);
            sql = sql.AddParameterWithValue("lastname", model.LastName);

            DbUtil.ExecuteNonQuery(sql);

            var result = Email.SendEmail(model.Email, this.applicationSettings.NotificationEmailFrom,
                string.Empty, // cc
                "Please complete registration",
                "Click to <a href='"
                + this.applicationSettings.AbsoluteUrlPrefix
                + VirtualPathUtility.ToAbsolute("~/Account/CompleteRegistration?id=")
                + guid
                + "'>complete registration</a>.",
                MailFormat.Html);

            ModelState.AddModelError(string.Empty, $"An email has been sent to {model.Email}<br>Please click on the link in the email message to complete registration.");

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - register"
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult CompleteRegistration(string id)
        {
            var sql = new SqlString(@"
                declare @expiration datetime
                set @expiration = dateadd(n, @minutes, getdate())

                select *,
                    case when el_date < @expiration then 1 else 0 end [expired]
                    from emailed_links
                    where el_id = @guid

                delete from emailed_links
                    where el_date < dateadd(n, -240, getdate())");

            sql.AddParameterWithValue("minutes", -this.applicationSettings.RegistrationExpiration);
            sql.AddParameterWithValue("guid", id);

            var dr = DbUtil.GetDataRow(sql);

            if (dr == null)
            {
                ModelState.AddModelError(string.Empty, "The link you clicked on is expired or invalid.<br>Please start over again.");
            }
            else if ((int)dr["expired"] == 1)
            {
                ModelState.AddModelError(string.Empty, "The link you clicked has expired.<br>Please start over again.");
            }
            else
            {
                Core.User.CopyUser(
                    (string)dr["el_username"],
                    (string)dr["el_email"],
                    (string)dr["el_firstname"],
                    (string)dr["el_lastname"],
                    string.Empty,
                    (int)dr["el_salt"],
                    (string)dr["el_password"], this.applicationSettings.SelfRegisteredUserTemplate,
                    false);

                //  Delete the temp link
                sql = new SqlString(@"delete from emailed_links where el_id = @guid");

                sql.AddParameterWithValue("guid", id);

                DbUtil.ExecuteNonQuery(sql);

                ModelState.AddModelError("Ok", "Your registration is complete.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - complete registration"
            };

            return View();
        }

        [HttpGet]
        public ActionResult Login()
        {
            // see if the connection string works
            try
            {
                // Intentionally getting an extra connection here so that we fall into the right "catch"
                using (var conn = DbUtil.GetSqlConnection())
                { }

                try
                {
                    DbUtil.ExecuteNonQuery("select count(1) from users");
                }
                catch (SqlException ex)
                {
                    Util.WriteToLog(ex.Message);
                    Util.WriteToLog(this.applicationSettings.ConnectionString);

                    ModelState.AddModelError(string.Empty, "Unable to find \"bugs\" table.<br>Click to <a href='/Asp/Install'>setup database tables</a>");
                }
            }
            catch (SqlException e2)
            {
                ModelState.AddModelError(string.Empty, "Unable to connect.<br>"
                                     + e2.Message + "<br>"
                                     + "Check Web.config file \"ConnectionString\" setting.<br>"
                                     + "Check also README.html<br>"
                                     + "Check also <a href=http://sourceforge.net/projects/btnet/forums/forum/226938>Help Forum</a> on Sourceforge.");
            }

            // Get authentication mode
            var authMode = this.applicationSettings.WindowsAuthentication;
            var usernameCookie = Request.Cookies["user"];
            var previousAuthMode = "0";

            if (usernameCookie != null)
            {
                previousAuthMode = usernameCookie["NTLM"];
            }

            // If an error occured, then force the authentication to manual
            if (Request.QueryString["msg"] == null)
            {
                // If windows authentication only, then redirect
                if (authMode == AuthenticationMode.Windows)
                {
                    return Redirect(Util.RedirectUrl("~/Account/LoginNt", System.Web.HttpContext.Current.Request));
                }

                // If previous login was with windows authentication, then try it again
                if (previousAuthMode == "1" && authMode == AuthenticationMode.Both)
                {
                    Response.Cookies["user"]["name"] = string.Empty;
                    Response.Cookies["user"]["NTLM"] = "0";

                    return Redirect(Util.RedirectUrl("~/Account/LoginNt", System.Web.HttpContext.Current.Request));
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, $"Error during windows authentication:<br>{HttpUtility.HtmlEncode(Request.QueryString["msg"])}");
            }

            var model = new LoginModel();

            if (previousAuthMode == "0")
            {
                // User name and password are not on the querystring.
                if (usernameCookie != null)
                {
                    // Set the user name from the last logon.
                    model.Login = usernameCookie["name"];
                }
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - logon"
            };

            return View(model);
        }

        [HttpPost]
        //[ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model)
        {
            // see if the connection string works
            try
            {
                // Intentionally getting an extra connection here so that we fall into the right "catch"
                using (var conn = DbUtil.GetSqlConnection())
                { }

                try
                {
                    DbUtil.ExecuteNonQuery("select count(1) from users");
                }
                catch (SqlException ex)
                {
                    Util.WriteToLog(ex.Message);
                    Util.WriteToLog(this.applicationSettings.ConnectionString);

                    ModelState.AddModelError(string.Empty, "Unable to find \"bugs\" table.<br>Click to <a href='/Asp/Install'>setup database tables</a>");
                }
            }
            catch (SqlException e2)
            {
                ModelState.AddModelError(string.Empty, "Unable to connect.<br>"
                                     + e2.Message + "<br>"
                                     + "Check Web.config file \"ConnectionString\" setting.<br>"
                                     + "Check also README.html<br>"
                                     + "Check also <a href=http://sourceforge.net/projects/btnet/forums/forum/226938>Help Forum</a> on Sourceforge.");
            }

            // Get authentication mode
            var authMode = this.applicationSettings.WindowsAuthentication;
            var usernameCookie = Request.Cookies["user"];
            var previousAuthMode = "0";

            if (usernameCookie != null)
            {
                previousAuthMode = usernameCookie["NTLM"];
            }

            // If an error occured, then force the authentication to manual
            if (Request.QueryString["msg"] == null)
            {
                // If windows authentication only, then redirect
                if (authMode == AuthenticationMode.Windows)
                {
                    return Redirect(Util.RedirectUrl("~/Account/LoginNt", System.Web.HttpContext.Current.Request));
                }

                // If previous login was with windows authentication, then try it again
                if (previousAuthMode == "1" && authMode == AuthenticationMode.Both)
                {
                    Response.Cookies["user"]["name"] = string.Empty;
                    Response.Cookies["user"]["NTLM"] = "0";

                    return Redirect(Util.RedirectUrl("~/Account/LoginNt", System.Web.HttpContext.Current.Request));
                }
            }
            else
            {
                ModelState.AddModelError(string.Empty, $"Error during windows authentication:<br>{HttpUtility.HtmlEncode(Request.QueryString["msg"])}");
            }

            // OnLogon
            if (authMode != AuthenticationMode.Site && string.IsNullOrEmpty(model.Login.Trim()))
            {
                return Redirect(Util.RedirectUrl("~/Account/LoginNt", System.Web.HttpContext.Current.Request));
            }

            if (model.AsGuest)
            {
                // for now
                var sql = new SqlString("select us_id from users where us_username = @us");

                sql.AddParameterWithValue("us", "guest");

                var dr = DbUtil.GetDataRow(sql);

                if (dr != null)
                {
                    this.authenticate
                        .SignIn("guest", false);

                    return Redirect(Util.RedirectUrl(System.Web.HttpContext.Current.Request));
                }
                else
                {
                    // How could this happen?  If someday the authentication
                    // method uses, say LDAP, then check_password could return
                    // true, even though there's no user in the database";
                    ModelState.AddModelError(string.Empty, "User not found in database");
                }
            }
            else
            {
                var authenticated = this.authenticate.CheckPassword(model.Login, model.Password);

                if (authenticated)
                {
                    var sql = new SqlString("select us_id from users where us_username = @us");

                    sql.AddParameterWithValue("us", model.Login);

                    var dr = DbUtil.GetDataRow(sql);

                    if (dr != null)
                    {
                        this.authenticate
                            .SignIn(model.Login, model.RememberMe);

                        return Redirect(Util.RedirectUrl(System.Web.HttpContext.Current.Request));
                    }
                    else
                    {
                        // How could this happen?  If someday the authentication
                        // method uses, say LDAP, then check_password could return
                        // true, even though there's no user in the database";
                        ModelState.AddModelError(string.Empty, "User not found in database");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid User or Password.");
                }
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - logon"
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult LoginNt()
        {
            using (DbUtil.GetSqlConnection())
            { }

            // Get authentication mode
            var authMode = this.applicationSettings.WindowsAuthentication;

            // If manual authentication only, we shouldn't be here, so redirect to manual screen
            if (authMode == AuthenticationMode.Site)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get the logon user from IIS
            var domainWindowsUsername = Request.ServerVariables["LOGON_USER"];

            if (string.IsNullOrEmpty(domainWindowsUsername))
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
                var sql = new SqlString(@"
                    select
                        us_id,
                        us_username
                    from
                        users
                    where
                        us_username = @us
                        and
                        us_active = 1");

                sql.AddParameterWithValue("us", windowsUsername);

                var dr = DbUtil.GetDataRow(sql);

                if (dr != null)
                {
                    // The user was found, so bake a cookie and redirect
                    var userid = (int)dr["us_id"];
                    this.authenticate
                        .SignIn((string)dr["us_username"], false);

                    Util.UpdateMostRecentLoginDateTime(userid);

                    var url = Util.RedirectUrl(System.Web.HttpContext.Current.Request);

                    return Redirect(url);
                }

                // Is self register enabled for users authenticated by windows?
                // If yes, then automatically insert a row in the user table
                var enableAutoRegistration = this.applicationSettings.EnableWindowsUserAutoRegistration;
                if (enableAutoRegistration)
                {
                    var templateUser = this.applicationSettings.WindowsUserAutoRegistrationUserTemplate;

                    var firstName = windowsUsername;
                    var lastName = windowsUsername;
                    var signature = windowsUsername;
                    var email = string.Empty;

                    // From the browser, we only know the Windows username.  Maybe we can get the other
                    // info from LDAP?
                    if (this.applicationSettings.EnableWindowsUserAutoRegistrationLdapSearch)
                        using (var de = new DirectoryEntry())
                        {
                            de.Path = this.applicationSettings.LdapDirectoryEntryPath;

                            de.AuthenticationType =
                                (AuthenticationTypes)Enum.Parse(
                                    typeof(AuthenticationTypes), this.applicationSettings.LdapDirectoryEntryAuthenticationType);

                            de.Username = this.applicationSettings.LdapDirectoryEntryUsername;
                            de.Password = this.applicationSettings.LdapDirectoryEntryPassword;

                            using (var search =
                                new DirectorySearcher(de))
                            {
                                var searchFilter = this.applicationSettings.LdapDirectorySearcherFilter;
                                search.Filter = searchFilter.Replace("$REPLACE_WITH_USERNAME$", windowsUsername);
                                SearchResult result = null;

                                try
                                {
                                    result = search.FindOne();
                                    if (result != null)
                                    {
                                        firstName = GetLdapPropertyValue(result, this.applicationSettings.LdapFirstName, firstName);
                                        lastName = GetLdapPropertyValue(result, this.applicationSettings.LdapLastName, lastName);
                                        email = GetLdapPropertyValue(result, this.applicationSettings.LdapEmail, email);
                                        signature = GetLdapPropertyValue(result, this.applicationSettings.LdapEmailSignature, signature);
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
                        this.authenticate
                            .SignIn((string)dr["us_username"], false);

                        Util.UpdateMostRecentLoginDateTime(newUserId);

                        var url = Util.RedirectUrl(System.Web.HttpContext.Current.Request);

                        return Redirect(url);
                    }
                }

                // Try fetching the guest user.
                sql = new SqlString(@"
                    select
                        us_id,
                        us_username
                    from
                        users
                    where
                        us_username = 'guest'
                        and
                        us_active = 1");

                dr = DbUtil.GetDataRow(sql);

                if (dr != null)
                {
                    // The Guest user was found, so bake a cookie and redirect
                    var userid = (int)dr["us_id"];
                    this.authenticate
                        .SignIn((string)dr["us_username"], false);

                    Util.UpdateMostRecentLoginDateTime(userid);

                    var url = Util.RedirectUrl(System.Web.HttpContext.Current.Request);

                    return Redirect(url);
                }

                // If using mixed-mode authentication and we got this far,
                // then we can't sign in using integrated security. Redirect
                // to the manual screen.
                if (authMode != AuthenticationMode.Windows)
                {
                    return RedirectToAction("Login", "Account", new { msg = "user+not+valid" });
                }

                // If we are still here, then toss a 401 error.
                return new HttpStatusCodeResult(401);
            }

            return View();
        }

        [HttpGet]
        public ActionResult Forgot()
        {
            if (!this.applicationSettings.ShowForgotPasswordLink)
            {
                return Content("Sorry, Web.config ShowForgotPasswordLink is set to 0");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - forgot password"
            };

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Forgot(ForgotModel model)
        {
            if (!this.applicationSettings.ShowForgotPasswordLink)
            {
                return Content("Sorry, Web.config ShowForgotPasswordLink is set to 0");
            }

            if (string.IsNullOrEmpty(model.Login) && string.IsNullOrEmpty(model.Email))
            {
                ModelState.AddModelError(string.Empty, "Enter either your Username or your Email address.");
            }
            else if (!string.IsNullOrEmpty(model.Email) && !Util.ValidateEmail(model.Email))
            {
                ModelState.AddModelError(string.Empty, "Format of email address is invalid.");
            }
            else
            {
                var userCount = 0;
                var userId = 0;

                if (!string.IsNullOrEmpty(model.Email) && string.IsNullOrEmpty(model.Login))
                {
                    // check if email exists
                    userCount = (int)DbUtil.ExecuteScalar(
                        "select count(1) from users where us_email = N'" + model.Email.Replace("'", "''") +
                        "'");

                    if (userCount == 1)
                    {
                        userId = (int)DbUtil.ExecuteScalar(
                            "select us_id from users where us_email = N'" + model.Email.Replace("'", "''") +
                            "'");
                    }
                }
                else if (string.IsNullOrEmpty(model.Email) && !string.IsNullOrEmpty(model.Login))
                {
                    // check if email exists
                    userCount = (int)DbUtil.ExecuteScalar(
                        "select count(1) from users where isnull(us_email,'') != '' and  us_username = N'" +
                        model.Login.Replace("'", "''") + "'");

                    if (userCount == 1)
                    {
                        userId = (int)DbUtil.ExecuteScalar(
                            "select us_id from users where us_username = N'" +
                            model.Login.Replace("'", "''") +
                            "'");
                    }
                }
                else if (!string.IsNullOrEmpty(model.Email) && !string.IsNullOrEmpty(model.Login))
                {
                    // check if email exists
                    userCount = (int)DbUtil.ExecuteScalar(
                        "select count(1) from users where us_username = N'" +
                        model.Login.Replace("'", "''") +
                        "' and us_email = N'"
                        + model.Email.Replace("'", "''") + "'");

                    if (userCount == 1)
                    {
                        userId = (int)DbUtil.ExecuteScalar(
                            "select us_id from users where us_username = N'" +
                            model.Login.Replace("'", "''") +
                            "' and us_email = N'"
                            + model.Email.Replace("'", "''") + "'");
                    }
                }

                if (userCount == 1)
                {
                    var guid = Guid.NewGuid().ToString();
                    var sql = new SqlString(@"
                        declare @username nvarchar(255)
                        declare @email nvarchar(255)

                        select @username = us_username, @email = us_email
                            from users where us_id = @user_id

                        insert into emailed_links
                            (el_id, el_date, el_email, el_action, el_user_id)
                            values (@guid, getdate(), @email, N'forgot', @user_id)

                        select @username us_username, @email us_email");

                    sql = sql.AddParameterWithValue("guid", guid);
                    sql = sql.AddParameterWithValue("user_id", userId);

                    var dr = DbUtil.GetDataRow(sql);

                    var result = Email.SendEmail(
                        (string)dr["us_email"], this.applicationSettings.NotificationEmailFrom,
                        string.Empty, // cc
                        "reset password",
                        "Click to <a href='"
                        + this.applicationSettings.AbsoluteUrlPrefix
                        + VirtualPathUtility.ToAbsolute("~/Account/ChangePassword?id=")
                        + guid
                        + "'>reset password</a> for user \""
                        + (string)dr["us_username"]
                        + "\".",
                        MailFormat.Html);

                    if (string.IsNullOrEmpty(result))
                    {
                        ModelState.AddModelError(string.Empty, "An email with password info has been sent to you.");
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "There was a problem sending the email." + "<br>" + result);
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Unknown username or email address.<br>Are you sure you spelled everything correctly?<br>Try just username, just email, or both.");
                }
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - forgot password"
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult ChangePassword(string id)
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - change password"
            };

            var model = new ChangePasswordModel
            {
                Id = id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordModel model)
        {
            if (!Util.CheckPasswordStrength(model.Password))
            {
                ModelState.AddModelError(string.Empty, "Password is not difficult enough to guess.<br>Avoid common words.<br>Try using a mixture of lowercase, uppercase, digits, and special characters.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - forgot password"
                };

                return View(model);
            }

            if (string.IsNullOrEmpty(model.Id))
            {
                return Content("no guid");
            }

            var sql = new SqlString(@"
                    declare @expiration datetime
                    set @expiration = dateadd(n, @minutes, getdate())

                    select *,
                        case when el_date < @expiration then 1 else 0 end [expired]
                        from emailed_links
                        where el_id = @guid

                    delete from emailed_links
                        where el_date < dateadd(n,-240, getdate())");

            sql.AddParameterWithValue("minutes", -this.applicationSettings.RegistrationExpiration);
            sql.AddParameterWithValue("guid", model.Id);

            var dr = DbUtil.GetDataRow(sql);

            if (dr == null)
            {
                ModelState.AddModelError(string.Empty, "The link you clicked on is expired or invalid.<br>Please start over again.");
            }
            else if ((int)dr["expired"] == 1)
            {
                ModelState.AddModelError(string.Empty, "The link you clicked has expired.<br>Please start over again.");
            }
            else
            {
                Util.UpdateUserPassword((int)dr["el_user_id"], model.Password);

                ModelState.AddModelError(string.Empty, "Your password has been changed.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - forgot password"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logoff()
        {
            Session.Abandon();
            Request.Cookies.Clear();

            this.authenticate
                .SignOut();

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        [Authorize(Roles = ApplicationRole.Member)]
        public ActionResult Settings()
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit your settings",
                SelectedItem = MainMenuSections.Settings
            };

            InitSettingsLists();

            var sql = new SqlString(@"
                select
                    pj_id,
                    pj_name,
                    isnull(pu_auto_subscribe,0) [pu_auto_subscribe]
                from
                    projects
                    
                    left outer join
                        project_user_xref
                    on
                        pj_id = pu_project
                        and
                         pu_user = @us
                where
                    isnull(pu_permission_level, @dpl) <> 0
                order by pj_name");

            sql = sql.AddParameterWithValue("us", this.security.User.Usid);
            sql = sql.AddParameterWithValue("dpl", this.applicationSettings.DefaultPermissionLevel);

            var projectsDv = DbUtil.GetDataView(sql);
            var projectsAutoSubscribe = new List<int>();

            foreach (DataRowView row in projectsDv)
            {
                if ((int)row["pu_auto_subscribe"] == 1)
                {
                    projectsAutoSubscribe.Add((int)row["pj_id"]);
                }
            }

            // Get this entry's data from the db and fill in the form
            // MAW -- 2006/01/27 -- Converted to use new notification columns
            sql = new SqlString(@"
                select
                    us_username [username],
                    isnull(us_firstname,'') [firstname],
                    isnull(us_lastname,'') [lastname],
                    isnull(us_bugs_per_page,10) [us_bugs_per_page],
                    us_use_fckeditor,
                    us_enable_bug_list_popups,
                    isnull(us_email,'') [email],
                    us_enable_notifications,
                    us_send_notifications_to_self,
                    us_reported_notifications,
                    us_assigned_notifications,
                    us_subscribed_notifications,
                    us_auto_subscribe,
                    us_auto_subscribe_own_bugs,
                    us_auto_subscribe_reported_bugs,
                    us_default_query,
                    isnull(us_signature,'') [signature]
                from
                    users
                where
                    us_id = @id");

            sql = sql.AddParameterWithValue("id", this.security.User.Usid);

            var dr = DbUtil.GetDataRow(sql);

            // Fill in this form
            var model = new SettingsModel
            {
                FirstName = (string)dr["firstname"],
                LastName = (string)dr["lastname"],
                BugsPerPage = (int)dr["us_bugs_per_page"],
                EditText = Convert.ToBoolean((int)dr["us_use_fckeditor"]),
                EnableBugListPopups = Convert.ToBoolean((int)dr["us_enable_bug_list_popups"]),
                Email = (string)dr["email"],
                EnableNotifications = Convert.ToBoolean((int)dr["us_enable_notifications"]),
                NotificationsForAllOtherSubscribedBugs = (int)dr["us_subscribed_notifications"],
                NotificationsSubscribedBugsReportedByMe = (int)dr["us_reported_notifications"],
                NotificationsSubscribedBugsAssignedToMe = (int)dr["us_assigned_notifications"],
                SendNotificationsEvenForItemsAddOrChange = Convert.ToBoolean((int)dr["us_send_notifications_to_self"]),
                AutoSubscribeToAllItems = Convert.ToBoolean((int)dr["us_auto_subscribe"]),
                AutoSubscribeToAllItemsAssignedToYou = Convert.ToBoolean((int)dr["us_auto_subscribe_own_bugs"]),
                AutoSubscribeToAllItemsReportedByYou = Convert.ToBoolean((int)dr["us_auto_subscribe_reported_bugs"]),
                EmailSignature = (string)dr["signature"],
                DefaultQueryId = (int)dr["us_default_query"],
                AutoSubscribePerProjectIds = projectsAutoSubscribe
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ApplicationRole.Member)]
        public ActionResult Settings(SettingsModel model)
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit your settings",
                SelectedItem = MainMenuSections.Settings
            };

            InitSettingsLists();

            if (!string.IsNullOrEmpty(model.Password))
            {
                if (!Util.CheckPasswordStrength(model.Password))
                {
                    ModelState.AddModelError(nameof(SettingsModel.Password), "Password is not difficult enough to guess.<br>Avoid common words.<br>Try using a mixture of lowercase, uppercase, digits, and special characters.");
                }

                if (model.ConfirmedPassword != model.Password)
                {
                    ModelState.AddModelError(nameof(SettingsModel.ConfirmedPassword), "Confirm Password must match Password.");
                }
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Your settings have not been updated.");

                return View(model);
            }

            var sql = new SqlString(@"
                update
                    users
                set
                    us_firstname = @fn,
                    us_lastname = @ln,
                    us_bugs_per_page = @bp,
                    us_use_fckeditor = @fk,
                    us_enable_bug_list_popups = @pp,
                    us_email = @em,
                    us_enable_notifications = @en,
                    us_send_notifications_to_self = @ss,
                    us_reported_notifications = @rn,
                    us_assigned_notifications = @an,
                    us_subscribed_notifications = @sn,
                    us_auto_subscribe = @as,
                    us_auto_subscribe_own_bugs = @ao,
                    us_auto_subscribe_reported_bugs = @ar,
                    us_default_query = @dq,
                    us_signature = @sg
                    where us_id = @id");

            sql = sql.AddParameterWithValue("fn", model.FirstName);
            sql = sql.AddParameterWithValue("ln", model.LastName);
            sql = sql.AddParameterWithValue("bp", model.BugsPerPage);
            sql = sql.AddParameterWithValue("fk", Util.BoolToString(model.EditText));
            sql = sql.AddParameterWithValue("pp", Util.BoolToString(model.EnableBugListPopups));
            sql = sql.AddParameterWithValue("em", model.Email);
            sql = sql.AddParameterWithValue("en", Util.BoolToString(model.EnableNotifications));
            sql = sql.AddParameterWithValue("ss", Util.BoolToString(model.SendNotificationsEvenForItemsAddOrChange));
            sql = sql.AddParameterWithValue("rn", model.NotificationsSubscribedBugsReportedByMe);
            sql = sql.AddParameterWithValue("an", model.NotificationsSubscribedBugsAssignedToMe);
            sql = sql.AddParameterWithValue("sn", model.NotificationsForAllOtherSubscribedBugs);
            sql = sql.AddParameterWithValue("as", Util.BoolToString(model.AutoSubscribeToAllItems));
            sql = sql.AddParameterWithValue("ao", Util.BoolToString(model.AutoSubscribeToAllItemsAssignedToYou));
            sql = sql.AddParameterWithValue("ar", Util.BoolToString(model.AutoSubscribeToAllItemsReportedByYou));
            sql = sql.AddParameterWithValue("dq", model.DefaultQueryId);
            sql = sql.AddParameterWithValue("sg", model.EmailSignature);
            sql = sql.AddParameterWithValue("id", this.security.User.Usid);

            // update user
            DbUtil.ExecuteNonQuery(sql);

            // update the password
            if (!string.IsNullOrEmpty(model.Password))
            {
                Util.UpdateUserPassword(this.security.User.Usid, model.Password);
            }

            // Now update project_user_xref
            // First turn everything off, then turn selected ones on.
            sql = new SqlString(@"
                update project_user_xref
                set
                    pu_auto_subscribe = 0
                where pu_user = @id");

            sql = sql.AddParameterWithValue("id", this.security.User.Usid);

            DbUtil.ExecuteNonQuery(sql);

            // Second see what to turn back on
            var projects = string.Empty;

            foreach (var id in model.AutoSubscribePerProjectIds)
            {
                if (!string.IsNullOrEmpty(projects))
                {
                    projects += ",";
                }

                projects += Convert.ToInt32(id);
            }

            // If we need to turn anything back on
            if (!string.IsNullOrEmpty(projects))
            {
                sql = new SqlString(@"
                    update
                        project_user_xref
                    set
                        pu_auto_subscribe = 1
                    where
                        pu_user = @id
                        and
                        pu_project in (@projects)

                insert into project_user_xref (pu_project, pu_user, pu_auto_subscribe)
                select
                    pj_id,
                    @id,
                    1
                from
                    projects
                where
                    pj_id in (@projects)
                    and
                    pj_id not in
                    (
                        select
                            pu_project
                        from
                            project_user_xref
                        where
                            pu_user = @id
                    )");

                sql = sql.AddParameterWithValue("id", this.security.User.Usid);
                sql = sql.AddParameterWithValue("projects", projects);

                DbUtil.ExecuteNonQuery(sql);
            }

            // apply subscriptions retroactively
            if (model.ApplySubscriptionChangesRetroactively)
            {
                sql = new SqlString(@"delete from bug_subscriptions where bs_user = @id;");

                if (model.AutoSubscribeToAllItems)
                {
                    sql.Append(@"
                        insert into
                            bug_subscriptions (bs_bug, bs_user)
                        select
                            bg_id,
                            @id
                        from
                            bugs;");
                }
                else
                {
                    if (model.AutoSubscribeToAllItemsReportedByYou)
                    {
                        sql.Append(@"insert into bug_subscriptions (bs_bug, bs_user)
                            select
                                bg_id,
                                @id
                            from
                                bugs
                            where
                                bg_reported_user = @id
                                and
                                bg_id not in
                                (
                                    select
                                        bs_bug
                                    from
                                        bug_subscriptions
                                    where
                                        bs_user = @id
                                );");
                    }

                    if (model.AutoSubscribeToAllItemsAssignedToYou)
                    {
                        sql.Append(@"insert into bug_subscriptions (bs_bug, bs_user)
                            select
                                bg_id,
                                @id
                            from
                                bugs
                            where
                                bg_assigned_to_user = @id
                                and
                                bg_id not in
                                (
                                    select
                                        bs_bug
                                    from
                                        bug_subscriptions
                                    where
                                        bs_user = @id
                                );");
                    }

                    if (!string.IsNullOrEmpty(projects))
                    {
                        sql.Append(@"insert into bug_subscriptions (bs_bug, bs_user)
                            select
                                bg_id,
                                @id
                            from
                                bugs
                            where
                                bg_project in (@projects)
                                and
                                bg_id not in
                                (
                                    select
                                        bs_bug
                                    from
                                        bug_subscriptions
                                    where
                                        bs_user = @id
                                );");
                    }
                }

                sql = sql.AddParameterWithValue("id", this.security.User.Usid);
                sql = sql.AddParameterWithValue("projects", projects);

                DbUtil.ExecuteNonQuery(sql);
            }

            ModelState.AddModelError("Ok", "Your settings have been updated.");

            return View(model);
        }

        private static string GetLdapPropertyValue(SearchResult result, string propertyName, string defaultValue)
        {
            var values = result.Properties[propertyName];

            if (values != null && values.Count == 1 && values[0] is string)
            {
                return (string)values[0];
            }

            return defaultValue;
        }

        private void InitSettingsLists()
        {
            var sql = new SqlString(@"
                declare @org int
                select
                    @org = us_org
                from
                    users
                where us_id = @us

                select
                    qu_id,
                    qu_desc
                from
                    queries
                where
                    (
                        isnull(qu_user,0) = 0
                        and
                        isnull(qu_org,0) = 0
                    )
                    or
                    isnull(qu_user,0) = @us
                    or
                    isnull(qu_org,0) = @org
                order by
                    qu_desc");

            sql = sql.AddParameterWithValue("us", this.security.User.Usid);

            var queriesDv = DbUtil.GetDataView(sql);

            ViewBag.Queries = new List<SelectListItem>();

            foreach (DataRowView row in queriesDv/*ds.Tables[1].DefaultView*/)
            {
                ViewBag.Queries.Add(new SelectListItem
                {
                    Value = ((int)row["qu_id"]).ToString(),
                    Text = (string)row["qu_desc"],
                });
            }

            sql = new SqlString(@"
                select
                    pj_id,
                    pj_name,
                    isnull(pu_auto_subscribe,0) [pu_auto_subscribe]
                from
                    projects
                    
                    left outer join
                        project_user_xref
                    on
                        pj_id = pu_project
                        and
                        pu_user = @us
                where
                    isnull(pu_permission_level, @dpl) <> 0
                order
                    by pj_name");

            sql = sql.AddParameterWithValue("us", this.security.User.Usid);
            sql = sql.AddParameterWithValue("dpl", this.applicationSettings.DefaultPermissionLevel);

            var projectsDv = DbUtil.GetDataView(sql);

            ViewBag.Projects = new List<SelectListItem>();

            foreach (DataRowView row in projectsDv)
            {
                ViewBag.Projects.Add(new SelectListItem
                {
                    Value = ((int)row["pj_id"]).ToString(),
                    Text = (string)row["pj_name"],
                });
            }

            ViewBag.Notifications = new List<SelectListItem>();

            ViewBag.Notifications.Add(new SelectListItem
            {
                Value = "0",
                Text = "no notifications"
            });

            ViewBag.Notifications.Add(new SelectListItem
            {
                Value = "1",
                Text = "when created"
            });

            ViewBag.Notifications.Add(new SelectListItem
            {
                Value = "2",
                Text = "when status changes"
            });

            ViewBag.Notifications.Add(new SelectListItem
            {
                Value = "3",
                Text = "when status or assigned-to changes"
            });

            ViewBag.Notifications.Add(new SelectListItem
            {
                Value = "4",
                Text = "when anything changes"
            });
        }
    }
}