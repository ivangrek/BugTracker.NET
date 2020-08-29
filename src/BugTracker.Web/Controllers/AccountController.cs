namespace BugTracker.Web.Controllers
{
    using System.Threading.Tasks;
    using Core;
    using Core.Identification;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Models.Account;

    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AccountController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly IAuthenticate authenticate;

        public AccountController(
            IApplicationSettings applicationSettings,
            IAuthenticate authenticate)
        {
            this.applicationSettings = applicationSettings;
            this.authenticate = authenticate;
        }

        [HttpGet]
        public IActionResult Login()
        {
            ViewBag.Title = $"{this.applicationSettings.ApplicationTitle} - Sign in";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (!model.AsGuest)
            {
                if (string.IsNullOrEmpty(model.Login))
                {
                    ModelState.AddModelError(nameof(LoginModel.Login), "Required username.");
                }

                if (string.IsNullOrEmpty(model.Password))
                {
                    ModelState.AddModelError(nameof(LoginModel.Password), "Required password.");
                }
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            await this.authenticate
                .SignInAsync(model.Login, model.Password, model.RememberMe, model.AsGuest);

            return RedirectToAction(nameof(BugController.Index), "Bug");
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LoginModel model)
        {
            await this.authenticate
                .SignOutAsync();

            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public ActionResult Register()
        {
            if (!this.applicationSettings.AllowSelfRegistration)
            {
                //TODO research
                //return Content("Sorry, Web.config AllowSelfRegistration is set to 0");
                return NotFound("Sorry, appsettings.json AllowSelfRegistration is set to 'false'");
            }

            ViewBag.Title = $"{this.applicationSettings.ApplicationTitle} - Sign up";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterModel model)
        {
            if (!this.applicationSettings.AllowSelfRegistration)
            {
                //TODO research
                //return Content("Sorry, Web.config AllowSelfRegistration is set to 0");
                return NotFound("Sorry, appsettings.json AllowSelfRegistration is set to 'false'");
            }

            return RedirectToAction(nameof(Register));

            //if (!Util.CheckPasswordStrength(model.Password))
            //{
            //    ModelState.AddModelError("Password", "Password is not difficult enough to guess.");
            //}

            //if (!ModelState.IsValid)
            //{
            //    ModelState.AddModelError(string.Empty, "Registration was not submitted.");

            //    ViewBag.Page = new PageModel
            //    {
            //        ApplicationSettings = this.applicationSettings,
            //        Security = this.security,
            //        Title = $"{this.applicationSettings.AppTitle} - register"
            //    };

            //    return View(model);
            //}

            //var guid = Guid.NewGuid().ToString();

            //// encrypt the password
            //var random = new Random();
            //var salt = random.Next(10000, 99999);
            //var encrypted = Util.EncryptStringUsingMd5(model.Password + Convert.ToString(salt));

            //var sql = new SqlString(@"
            //    insert into emailed_links
            //    (
            //        el_id,
            //        el_date,
            //        el_email,
            //        el_action,
            //        el_username,
            //        el_salt,
            //        el_password,
            //        el_firstname,
            //        el_lastname
            //    )
            //    values
            //    (
            //        @guid,
            //        getdate(),
            //        @email,
            //        N'register',
            //        @username,
            //        @salt,
            //        @password,
            //        @firstname,
            //        @lastname
            //    )");

            //sql = sql.AddParameterWithValue("guid", guid);
            //sql = sql.AddParameterWithValue("password", encrypted);
            //sql = sql.AddParameterWithValue("salt", salt);
            //sql = sql.AddParameterWithValue("username", model.Login);
            //sql = sql.AddParameterWithValue("email", model.Email);
            //sql = sql.AddParameterWithValue("firstname", model.FirstName);
            //sql = sql.AddParameterWithValue("lastname", model.LastName);

            //DbUtil.ExecuteNonQuery(sql);

            //var result = Email.SendEmail(model.Email, this.applicationSettings.NotificationEmailFrom,
            //    string.Empty, // cc
            //    "Please complete registration",
            //    "Click to <a href='"
            //    + this.applicationSettings.AbsoluteUrlPrefix
            //    + VirtualPathUtility.ToAbsolute("~/Account/CompleteRegistration?id=")
            //    + guid
            //    + "'>complete registration</a>.",
            //    MailFormat.Html);

            //ModelState.AddModelError(string.Empty, $"An email has been sent to {model.Email}<br>Please click on the link in the email message to complete registration.");

            //ViewBag.Page = new PageModel
            //{
            //    ApplicationSettings = this.applicationSettings,
            //    Security = this.security,
            //    Title = $"{this.applicationSettings.AppTitle} - register"
            //};

            //return View(model);
        }

        [HttpGet]
        public ActionResult Forgot()
        {
            if (!this.applicationSettings.ShowForgotPasswordLink)
            {
                //TODO research
                //return Content("Sorry, Web.config ShowForgotPasswordLink is set to 0");
                return NotFound("Sorry, appsettings.json ShowForgotPasswordLink is set to 'false'");
            }

            ViewBag.Title = $"{this.applicationSettings.ApplicationTitle} - Forgot password";

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Forgot(ForgotModel model)
        {
            if (!this.applicationSettings.ShowForgotPasswordLink)
            {
                //TODO research
                //return Content("Sorry, Web.config ShowForgotPasswordLink is set to 0");
                return NotFound("Sorry, appsettings.json ShowForgotPasswordLink is set to 'false'");
            }

            return RedirectToAction(nameof(Forgot));

            //if (string.IsNullOrEmpty(model.Login) && string.IsNullOrEmpty(model.Email))
            //{
            //    ModelState.AddModelError(string.Empty, "Enter either your Username or your Email address.");
            //}
            //else if (!string.IsNullOrEmpty(model.Email) && !Util.ValidateEmail(model.Email))
            //{
            //    ModelState.AddModelError(string.Empty, "Format of email address is invalid.");
            //}
            //else
            //{
            //    var userCount = 0;
            //    var userId = 0;

            //    if (!string.IsNullOrEmpty(model.Email) && string.IsNullOrEmpty(model.Login))
            //    {
            //        var sql = new SqlString("select count(1) from users where us_email = @email");

            //        sql.AddParameterWithValue("email", model.Email);

            //        // check if email exists
            //        userCount = (int)DbUtil.ExecuteScalar(sql);

            //        if (userCount == 1)
            //        {
            //            sql = new SqlString("select us_id from users where us_email = @email");

            //            sql.AddParameterWithValue("email", model.Email);

            //            userId = (int)DbUtil.ExecuteScalar(sql);
            //        }
            //    }
            //    else if (string.IsNullOrEmpty(model.Email) && !string.IsNullOrEmpty(model.Login))
            //    {
            //        var sql = new SqlString("select count(1) from users where isnull(us_email,'') != '' and  us_username = @username");

            //        sql.AddParameterWithValue("username", model.Login);

            //        // check if email exists
            //        userCount = (int)DbUtil.ExecuteScalar(sql);

            //        if (userCount == 1)
            //        {
            //            sql = new SqlString("select us_id from users where us_username = @username");

            //            sql.AddParameterWithValue("username", model.Login);

            //            userId = (int)DbUtil.ExecuteScalar(sql);
            //        }
            //    }
            //    else if (!string.IsNullOrEmpty(model.Email) && !string.IsNullOrEmpty(model.Login))
            //    {
            //        var sql = new SqlString("select count(1) from users where us_username = @username and us_email = @email");

            //        sql.AddParameterWithValue("username", model.Login);
            //        sql.AddParameterWithValue("email", model.Email);

            //        // check if email exists
            //        userCount = (int)DbUtil.ExecuteScalar(sql);

            //        if (userCount == 1)
            //        {
            //            sql = new SqlString("select us_id from users where us_username = @username and us_email = @email");

            //            sql.AddParameterWithValue("username", model.Login);
            //            sql.AddParameterWithValue("email", model.Email);

            //            userId = (int)DbUtil.ExecuteScalar(sql);
            //        }
            //    }

            //    if (userCount == 1)
            //    {
            //        var guid = Guid.NewGuid().ToString();
            //        var sql = new SqlString(@"
            //            declare @username nvarchar(255)
            //            declare @email nvarchar(255)

            //            select @username = us_username, @email = us_email
            //                from users where us_id = @user_id

            //            insert into emailed_links
            //                (el_id, el_date, el_email, el_action, el_user_id)
            //                values (@guid, getdate(), @email, N'forgot', @user_id)

            //            select @username us_username, @email us_email");

            //        sql = sql.AddParameterWithValue("guid", guid);
            //        sql = sql.AddParameterWithValue("user_id", userId);

            //        var dr = DbUtil.GetDataRow(sql);

            //        var result = Email.SendEmail(
            //            (string)dr["us_email"], this.applicationSettings.NotificationEmailFrom,
            //            string.Empty, // cc
            //            "reset password",
            //            "Click to <a href='"
            //            + this.applicationSettings.AbsoluteUrlPrefix
            //            + VirtualPathUtility.ToAbsolute("~/Account/ChangePassword?id=")
            //            + guid
            //            + "'>reset password</a> for user \""
            //            + (string)dr["us_username"]
            //            + "\".",
            //            MailFormat.Html);

            //        if (string.IsNullOrEmpty(result))
            //        {
            //            ModelState.AddModelError(string.Empty, "An email with password info has been sent to you.");
            //        }
            //        else
            //        {
            //            ModelState.AddModelError(string.Empty, "There was a problem sending the email." + "<br>" + result);
            //        }
            //    }
            //    else
            //    {
            //        ModelState.AddModelError(string.Empty, "Unknown username or email address.<br>Are you sure you spelled everything correctly?<br>Try just username, just email, or both.");
            //    }
            //}

            //ViewBag.Page = new PageModel
            //{
            //    ApplicationSettings = this.applicationSettings,
            //    Security = this.security,
            //    Title = $"{this.applicationSettings.AppTitle} - forgot password"
            //};

            //return View(model);
        }
    }
}
