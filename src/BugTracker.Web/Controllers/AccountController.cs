namespace BugTracker.Web.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading.Tasks;
    using Core;
    using Core.Identification;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Models.Account;

    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class AccountController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly IAuthenticate authenticate;
        private readonly IDbUtil dbUtil;

        public AccountController(
            IApplicationSettings applicationSettings,
            IAuthenticate authenticate,
            IDbUtil dbUtil)
        {
            this.applicationSettings = applicationSettings;
            this.authenticate = authenticate;
            this.dbUtil = dbUtil;
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
        public IActionResult Register()
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
        public IActionResult Register(RegisterModel model)
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
        public IActionResult Forgot()
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
        public IActionResult Forgot(ForgotModel model)
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

        [HttpGet]
        [Authorize(Roles = BtNetRole.User)]
        public IActionResult Settings()
        {
            ViewBag.Title = $"{this.applicationSettings.ApplicationTitle} - Edit your settings";
            ViewBag.SelectedItem = MainMenuSection.Settings;

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

            sql = sql.AddParameterWithValue("us", User.Identity.GetUserId());
            sql = sql.AddParameterWithValue("dpl", this.applicationSettings.DefaultPermissionLevel);

            var projectsDv = this.dbUtil
                .GetDataView(sql);

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

            sql = sql.AddParameterWithValue("id", User.Identity.GetUserId());

            var dr = this.dbUtil
                .GetDataRow(sql);

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
        [Authorize(Roles = BtNetRole.User)]
        public IActionResult Settings(SettingsModel model)
        {
            ViewBag.Title = $"{this.applicationSettings.ApplicationTitle} - Edit your settings";
            ViewBag.SelectedItem = MainMenuSection.Settings;

            InitSettingsLists();

            if (!string.IsNullOrEmpty(model.Password))
            {
                if (!this.authenticate.CheckPasswordStrength(model.Password))
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
            sql = sql.AddParameterWithValue("fk", model.EditText);
            sql = sql.AddParameterWithValue("pp", model.EnableBugListPopups);
            sql = sql.AddParameterWithValue("em", model.Email);
            sql = sql.AddParameterWithValue("en", model.EnableNotifications);
            sql = sql.AddParameterWithValue("ss", model.SendNotificationsEvenForItemsAddOrChange);
            sql = sql.AddParameterWithValue("rn", model.NotificationsSubscribedBugsReportedByMe);
            sql = sql.AddParameterWithValue("an", model.NotificationsSubscribedBugsAssignedToMe);
            sql = sql.AddParameterWithValue("sn", model.NotificationsForAllOtherSubscribedBugs);
            sql = sql.AddParameterWithValue("as", model.AutoSubscribeToAllItems);
            sql = sql.AddParameterWithValue("ao", model.AutoSubscribeToAllItemsAssignedToYou);
            sql = sql.AddParameterWithValue("ar", model.AutoSubscribeToAllItemsReportedByYou);
            sql = sql.AddParameterWithValue("dq", model.DefaultQueryId);
            sql = sql.AddParameterWithValue("sg", model.EmailSignature);
            sql = sql.AddParameterWithValue("id", User.Identity.GetUserId());

            // update user
            this.dbUtil
                .ExecuteNonQuery(sql);

            // update the password
            if (!string.IsNullOrEmpty(model.Password))
            {
                this.authenticate
                    .UpdateUserPassword(User.Identity.GetUserId(), model.Password);
            }

            // Now update project_user_xref
            // First turn everything off, then turn selected ones on.
            sql = new SqlString(@"
                update project_user_xref
                set
                    pu_auto_subscribe = 0
                where pu_user = @id");

            sql = sql.AddParameterWithValue("id", User.Identity.GetUserId());

            this.dbUtil
                .ExecuteNonQuery(sql);

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

                sql = sql.AddParameterWithValue("id", User.Identity.GetUserId());
                sql = sql.AddParameterWithValue("projects", projects);

                this.dbUtil
                    .ExecuteNonQuery(sql);
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

                sql = sql.AddParameterWithValue("id", User.Identity.GetUserId());
                sql = sql.AddParameterWithValue("projects", projects);

                this.dbUtil
                    .ExecuteNonQuery(sql);
            }

            ModelState.AddModelError("Ok", "Your settings have been updated.");

            return View(model);
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

            sql = sql.AddParameterWithValue("us", User.Identity.GetUserId());

            var queriesDv = this.dbUtil
                .GetDataView(sql);

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

            sql = sql.AddParameterWithValue("us", User.Identity.GetUserId());
            sql = sql.AddParameterWithValue("dpl", this.applicationSettings.DefaultPermissionLevel);

            var projectsDv = this.dbUtil
                .GetDataView(sql);

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
