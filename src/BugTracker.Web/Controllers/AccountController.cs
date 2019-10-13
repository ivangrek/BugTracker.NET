/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Controllers
{
    using BugTracker.Web.Core;
    using BugTracker.Web.Models;
    using BugTracker.Web.Models.Account;
    using System;
    using System.Data.SqlClient;
    using System.DirectoryServices;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.Security;
    using System.Web.UI;

    [OutputCache(Location = OutputCacheLocation.None)]
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
            Util.SetContext(System.Web.HttpContext.Current);

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
            Util.SetContext(System.Web.HttpContext.Current);

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
                ModelState.AddModelError("Message", "Registration was not submitted.");

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

            var sql = @"
                    insert into emailed_links
                        (el_id, el_date, el_email, el_action,
                            el_username, el_salt, el_password, el_firstname, el_lastname)
                        values ('$guid', getdate(), N'$email', N'register',
                            N'$username', $salt, N'$password', N'$firstname', N'$lastname')";

            sql = sql.Replace("$guid", guid);
            sql = sql.Replace("$password", encrypted);
            sql = sql.Replace("$salt", Convert.ToString(salt));
            sql = sql.Replace("$username", model.Login.Replace("'", "''"));
            sql = sql.Replace("$email", model.Email.Replace("'", "''"));
            sql = sql.Replace("$firstname", model.FirstName.Replace("'", "''"));
            sql = sql.Replace("$lastname", model.LastName.Replace("'", "''"));

            DbUtil.ExecuteNonQuery(sql);

            var result = Email.SendEmail(model.Email,
                this.applicationSettings.NotificationEmailFrom,
                string.Empty, // cc
                "Please complete registration",
                "Click to <a href='"
                + this.applicationSettings.AbsoluteUrlPrefix
                + VirtualPathUtility.ToAbsolute("~/Account/CompleteRegistration?id=")
                + guid
                + "'>complete registration</a>.",
                BtnetMailFormat.Html);

            ModelState.AddModelError("Message", $"An email has been sent to {model.Email}<br>Please click on the link in the email message to complete registration.");

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
            Util.SetContext(System.Web.HttpContext.Current);

            var sql = @"
                declare @expiration datetime
                set @expiration = dateadd(n,-$minutes,getdate())

                select *,
                    case when el_date < @expiration then 1 else 0 end [expired]
                    from emailed_links
                    where el_id = '$guid'

                delete from emailed_links
                    where el_date < dateadd(n,-240,getdate())"
                .Replace("$minutes", this.applicationSettings.RegistrationExpiration.ToString())
                .Replace("$guid", id.Replace("'", "''"));

            var dr = DbUtil.GetDataRow(sql);

            if (dr == null)
            {
                ModelState.AddModelError("Message", "The link you clicked on is expired or invalid.<br>Please start over again.");
            }
            else if ((int)dr["expired"] == 1)
            {
                ModelState.AddModelError("Message", "The link you clicked has expired.<br>Please start over again.");
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
                    (string)dr["el_password"],
                    this.applicationSettings.SelfRegisteredUserTemplate,
                    false);

                //  Delete the temp link
                sql = @"delete from emailed_links where el_id = '$guid'"
                    .Replace("$guid", id.Replace("'", "''"));

                DbUtil.ExecuteNonQuery(sql);

                ModelState.AddModelError("Message", "Your registration is complete.");
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
            Util.SetContext(System.Web.HttpContext.Current);

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

                    ModelState.AddModelError("Message", "Unable to find \"bugs\" table.<br>Click to <a href='/Asp/Install'>setup database tables</a>");
                }
            }
            catch (SqlException e2)
            {
                ModelState.AddModelError("Message", "Unable to connect.<br>"
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
                if (authMode == 1)
                {
                    return Redirect(Util.RedirectUrl("~/Account/LoginNt", System.Web.HttpContext.Current.Request));
                }

                // If previous login was with windows authentication, then try it again
                if (previousAuthMode == "1" && authMode == 2)
                {
                    Response.Cookies["user"]["name"] = "";
                    Response.Cookies["user"]["NTLM"] = "0";

                    return Redirect(Util.RedirectUrl("~/Account/LoginNt", System.Web.HttpContext.Current.Request));
                }
            }
            else
            {
                ModelState.AddModelError("Message", $"Error during windows authentication:<br>{HttpUtility.HtmlEncode(Request.QueryString["msg"])}");
            }

            // strange code
            //// fill in the username first time in
            //if (previousAuthMode == "0")
            //{
            //    if (Request.QueryString["user"] == null || Request.QueryString["password"] == null)
            //    {
            //        //  User name and password are not on the querystring.
            //        //  Set the user name from the last logon.
            //        if (usernameCookie != null)
            //        {
            //            this.user.Value = usernameCookie["name"];
            //        }
            //    }
            //    else
            //    {
            //        //  User name and password have been passed on the querystring.

            //        this.user.Value = Request.QueryString["user"];
            //        this.pw.Value = Request.QueryString["password"];

            //        OnLogon();
            //    }
            //}

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - logon"
            };

            var model = new LoginModel
            {
                Login = Request.QueryString["user"],
                Password = Request.QueryString["password"]
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model)
        {
            Util.SetContext(System.Web.HttpContext.Current);

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

                    ModelState.AddModelError("Message", "Unable to find \"bugs\" table.<br>Click to <a href='/Asp/Install'>setup database tables</a>");
                }
            }
            catch (SqlException e2)
            {
                ModelState.AddModelError("Message", "Unable to connect.<br>"
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
                if (authMode == 1)
                {
                    return Redirect(Util.RedirectUrl("~/Account/LoginNt", System.Web.HttpContext.Current.Request));
                }

                // If previous login was with windows authentication, then try it again
                if (previousAuthMode == "1" && authMode == 2)
                {
                    Response.Cookies["user"]["name"] = "";
                    Response.Cookies["user"]["NTLM"] = "0";

                    return Redirect(Util.RedirectUrl("~/Account/LoginNt", System.Web.HttpContext.Current.Request));
                }
            }
            else
            {
                ModelState.AddModelError("Message", $"Error during windows authentication:<br>{HttpUtility.HtmlEncode(Request.QueryString["msg"])}");
            }

            // OnLogon
            if (authMode != 0 && string.IsNullOrEmpty(model.Login.Trim()))
            {
                return Redirect(Util.RedirectUrl("~/Account/LoginNt", System.Web.HttpContext.Current.Request));
            }

            var authenticated = this.authenticate.CheckPassword(model.Login, model.Password);

            if (authenticated)
            {
                var sql = "select us_id from users where us_username = N'$us'"
                    .Replace("$us", model.Login.Replace("'", "''"));

                var dr = DbUtil.GetDataRow(sql);

                if (dr != null)
                {
                    var usId = (int)dr["us_id"];

                    this.security.CreateSession(System.Web.HttpContext.Current.Request, System.Web.HttpContext.Current.Response, usId, model.Login, "0");

                    FormsAuthentication.SetAuthCookie(model.Login, false);

                    return Redirect(Util.RedirectUrl(System.Web.HttpContext.Current.Request));
                }
                else
                {
                    // How could this happen?  If someday the authentication
                    // method uses, say LDAP, then check_password could return
                    // true, even though there's no user in the database";
                    ModelState.AddModelError("Message", "User not found in database");
                }
            }
            else
            {
                ModelState.AddModelError("Message", "Invalid User or Password.");
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

            if (authMode == 0)
            {
                var url = Util.RedirectUrl("~/Account/Login", System.Web.HttpContext.Current.Request);

                return Redirect(url);
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
                var sql = @"select us_id, us_username
                    from users
                    where us_username = N'$us'
                    and us_active = 1"
                    .Replace("$us", windowsUsername.Replace("'", "''"));

                var dr = DbUtil.GetDataRow(sql);

                if (dr != null)
                {
                    // The user was found, so bake a cookie and redirect
                    var userid = (int)dr["us_id"];
                    this.security.CreateSession(System.Web.HttpContext.Current.Request, System.Web.HttpContext.Current.Response,
                        userid,
                        (string)dr["us_username"],
                        "1");

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
                                    typeof(AuthenticationTypes),
                                    this.applicationSettings.LdapDirectoryEntryAuthenticationType);

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
                        this.security.CreateSession(System.Web.HttpContext.Current.Request, System.Web.HttpContext.Current.Response,
                            newUserId,
                            windowsUsername.Replace("'", "''"),
                            "1");

                        Util.UpdateMostRecentLoginDateTime(newUserId);

                        var url = Util.RedirectUrl(System.Web.HttpContext.Current.Request);

                        return Redirect(url);
                    }
                }

                // Try fetching the guest user.
                sql = @"select us_id, us_username
                        from users
                        where us_username = 'guest'
                        and us_active = 1";

                dr = DbUtil.GetDataRow(sql);

                if (dr != null)
                {
                    // The Guest user was found, so bake a cookie and redirect
                    var userid = (int)dr["us_id"];
                    this.security.CreateSession(System.Web.HttpContext.Current.Request, System.Web.HttpContext.Current.Response,
                        userid,
                        (string)dr["us_username"],
                        "1");

                    Util.UpdateMostRecentLoginDateTime(userid);

                    var url = Util.RedirectUrl(System.Web.HttpContext.Current.Request);

                    return Redirect(url);
                }

                // If using mixed-mode authentication and we got this far,
                // then we can't sign in using integrated security. Redirect
                // to the manual screen.
                if (authMode != 1)
                {
                    var url = Util.RedirectUrl("~/Account/Login?msg=user+not+valid", System.Web.HttpContext.Current.Request);

                    return Redirect(url);
                }

                // If we are still here, then toss a 401 error.
                return new HttpStatusCodeResult(401);
            }

            return View();
        }

        [HttpGet]
        public ActionResult MobileLogin()
        {
            Util.SetContext(System.Web.HttpContext.Current);

            if (!this.applicationSettings.EnableMobile)
            {
                return Content("BugTracker.NET EnableMobile is not set to 1 in Web.config");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - logon"
            };

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult MobileLogin(LoginModel model)
        {
            Util.SetContext(System.Web.HttpContext.Current);

            if (!this.applicationSettings.EnableMobile)
            {
                return Content("BugTracker.NET EnableMobile is not set to 1 in Web.config");
            }

            var authenticated = this.authenticate.CheckPassword(model.Login, model.Password);

            if (authenticated)
            {
                var sql = "select us_id from users where us_username = N'$us'"
                    .Replace("$us", model.Login.Replace("'", "''"));

                var dr = DbUtil.GetDataRow(sql);

                if (dr != null)
                {
                    var usId = (int)dr["us_id"];

                    this.security.CreateSession(System.Web.HttpContext.Current.Request, System.Web.HttpContext.Current.Response,
                        usId, model.Login, "0");

                    var url = Util.RedirectUrl(System.Web.HttpContext.Current.Request);

                    return Redirect(url);
                }
            }
            else
            {
                ModelState.AddModelError("Message", "Invalid User or Password.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - logon"
            };

            return View();
        }

        [HttpGet]
        public ActionResult Forgot()
        {
            Util.SetContext(System.Web.HttpContext.Current);

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
            Util.SetContext(System.Web.HttpContext.Current);

            if (!this.applicationSettings.ShowForgotPasswordLink)
            {
                return Content("Sorry, Web.config ShowForgotPasswordLink is set to 0");
            }

            if (string.IsNullOrEmpty(model.Login) && string.IsNullOrEmpty(model.Email))
            {
                ModelState.AddModelError("Message", "Enter either your Username or your Email address.");
            }
            else if (!string.IsNullOrEmpty(model.Email) && !Util.ValidateEmail(model.Email))
            {
                ModelState.AddModelError("Message", "Format of email address is invalid.");
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
                    var sql = @"
                        declare @username nvarchar(255)
                        declare @email nvarchar(255)

                        select @username = us_username, @email = us_email
                            from users where us_id = $user_id

                        insert into emailed_links
                            (el_id, el_date, el_email, el_action, el_user_id)
                            values ('$guid', getdate(), @email, N'forgot', $user_id)

                        select @username us_username, @email us_email";

                    sql = sql.Replace("$guid", guid);
                    sql = sql.Replace("$user_id", Convert.ToString(userId));

                    var dr = DbUtil.GetDataRow(sql);

                    var result = Email.SendEmail(
                        (string)dr["us_email"],
                        this.applicationSettings.NotificationEmailFrom,
                        string.Empty, // cc
                        "reset password",
                        "Click to <a href='"
                        + this.applicationSettings.AbsoluteUrlPrefix
                        + VirtualPathUtility.ToAbsolute("~/Account/ChangePassword?id=")
                        + guid
                        + "'>reset password</a> for user \""
                        + (string)dr["us_username"]
                        + "\".",
                        BtnetMailFormat.Html);

                    if (string.IsNullOrEmpty(result))
                    {
                        ModelState.AddModelError("Message", "An email with password info has been sent to you.");
                    }
                    else
                    {
                        ModelState.AddModelError("Message", "There was a problem sending the email." + "<br>" + result);
                    }
                }
                else
                {
                    ModelState.AddModelError("Message", "Unknown username or email address.<br>Are you sure you spelled everything correctly?<br>Try just username, just email, or both.");
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
            Util.SetContext(System.Web.HttpContext.Current);

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
            Util.SetContext(System.Web.HttpContext.Current);

            if (!Util.CheckPasswordStrength(model.Password))
            {
                ModelState.AddModelError("Message", "Password is not difficult enough to guess.<br>Avoid common words.<br>Try using a mixture of lowercase, uppercase, digits, and special characters.");
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

            var sql = @"
                    declare @expiration datetime
                    set @expiration = dateadd(n,-$minutes,getdate())

                    select *,
                        case when el_date < @expiration then 1 else 0 end [expired]
                        from emailed_links
                        where el_id = '$guid'

                    delete from emailed_links
                        where el_date < dateadd(n,-240,getdate())"
                .Replace("$minutes", this.applicationSettings.RegistrationExpiration.ToString())
                .Replace("$guid", model.Id.Replace("'", "''"));

            var dr = DbUtil.GetDataRow(sql);

            if (dr == null)
            {
                ModelState.AddModelError("Message", "The link you clicked on is expired or invalid.<br>Please start over again.");
            }
            else if ((int)dr["expired"] == 1)
            {
                ModelState.AddModelError("Message", "The link you clicked has expired.<br>Please start over again.");
            }
            else
            {
                Util.UpdateUserPassword((int)dr["el_user_id"], model.Password);

                ModelState.AddModelError("Message", "Your password has been changed.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - forgot password"
            };

            return View(model);
        }

        //[HttpPost]                    // TODO uncomment after migration
        //[ValidateAntiForgeryToken]    // TODO uncomment after migration
        public ActionResult Logoff()
        {
            Util.SetContext(System.Web.HttpContext.Current);

            using (DbUtil.GetSqlConnection())
            { }

            // delete the session row
            var cookie = Request.Cookies["se_id2"];

            if (cookie != null)
            {
                var seId = cookie.Value.Replace("'", "''");

                var sql = @"delete from sessions
                    where se_id = N'$se'
                    or datediff(d, se_date, getdate()) > 2"
                    .Replace("$se", seId);

                DbUtil.ExecuteNonQuery(sql);

                Session[seId] = 0;

                Session["SelectedBugQuery"] = null;
                Session["bugs"] = null;
                Session["bugs_unfiltered"] = null;
                Session["project"] = null;

                Session.Abandon();

                foreach (string key in Request.Cookies.AllKeys)
                {
                    Response.Cookies[key].Expires = DateTime.Now.AddDays(-1);
                }
            }

            // for now, quik code
            FormsAuthentication.SignOut();

            return Redirect("~/");
        }

        private string GetLdapPropertyValue(SearchResult result, string propertyName, string defaultValue)
        {
            var values = result.Properties[propertyName];

            if (values != null && values.Count == 1 && values[0] is string)
            {
                return (string)values[0];
            }

            return defaultValue;
        }
    }
}