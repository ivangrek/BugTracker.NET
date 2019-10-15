/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Controllers
{
    using BugTracker.Web.Areas.Administration.Models.User;
    using BugTracker.Web.Core;
    using BugTracker.Web.Core.Controls;
    using BugTracker.Web.Models;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;

    [Authorize(Roles = ApplicationRoles.Administrators)]
    [OutputCache(Location = OutputCacheLocation.None)]
    public class UserController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;

        public UserController(
            IApplicationSettings applicationSettings,
            ISecurity security)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
        }

        [HttpGet]
        public ActionResult Index()
        {
            string sql;

            if (this.security.User.IsAdmin)
            {
                sql = @"
                    select distinct pu_user
                    into #t
                    from
                    project_user_xref
                    where pu_admin = 1;

                    select u.us_id [id],
                    '<a href=" + VirtualPathUtility.ToAbsolute("~/Administration/User/Update/") + @"' + convert(varchar,u.us_id) + '>edit</a>' [$no_sort_edit],
                    '<a href=" + VirtualPathUtility.ToAbsolute("~/Administration/User/Copy/") + @"' + convert(varchar,u.us_id) + '>copy</a>' [$no_sort_add<br>like<br>this],
                    '<a href=" + VirtualPathUtility.ToAbsolute("~/Administration/User/Delete/") + @"' + convert(varchar,u.us_id) + '>delete</a>' [$no_sort_delete],

                    u.us_username [username],
                    isnull(u.us_firstname,'') + ' ' + isnull(u.us_lastname,'') [name],
                    '<a sort=''' + og_name + ''' href=" + VirtualPathUtility.ToAbsolute("~/Administration/Organization/Update/") + @"' + convert(varchar,og_id) + '>' + og_name + '</a>' [org],
                    isnull(u.us_email,'') [email],
                    case when u.us_admin = 1 then 'Y' else 'N' end [admin],
                    case when pu_user is null then 'N' else 'Y' end [project<br>admin],
                    case when u.us_active = 1 then 'Y' else 'N' end [active],
                    case when og_external_user = 1 then 'Y' else 'N' end [external],
                    isnull(pj_name,'') [forced<br>project],
                    isnull(qu_desc,'') [default query],
                    case when u.us_enable_notifications = 1 then 'Y' else 'N' end [notif-<br>ications],
                    u.us_most_recent_login_datetime [most recent login],
                    u2.us_username [created<br>by]

                    from users u
                    inner join orgs on u.us_org = og_id
                    left outer join queries on u.us_default_query = qu_id
                    left outer join projects on u.us_forced_project = pj_id
                    left outer join users u2 on u.us_created_user = u2.us_id
                    left outer join #t on u.us_id = pu_user
                    where u.us_active in (1 $inactive)
                    $filter_users
                    order by u.us_username;

                    drop table #t";
            }
            else
            {
                sql = @"
                    select distinct pu_user
                    into #t
                    from
                    project_user_xref
                    where pu_admin = 1;

                    select u.us_id [id],
                    '<a href=" + VirtualPathUtility.ToAbsolute("~/Administration/User/Update/") + @"' + convert(varchar,u.us_id) + '>edit</a>' [$no_sort_edit],
                    '<a href=" + VirtualPathUtility.ToAbsolute("~/Administration/User/Copy/") + @"' + convert(varchar,u.us_id) + '>copy</a>' [$no_sort_add<br>like<br>this],
                    '<a href=" + VirtualPathUtility.ToAbsolute("~/Administration/User/Delete/") + @"' + convert(varchar,u.us_id) + '>delete</a>' [$no_sort_delete],

                    u.us_username [username],
                    isnull(u.us_firstname,'') + ' ' + isnull(u.us_lastname,'') [name],
                    og_name [org],
                    isnull(u.us_email,'') [email],			
                    case when u.us_admin = 1 then 'Y' else 'N' end [admin],
                    case when pu_user is null then 'N' else 'Y' end [project<br>admin],
                    case when u.us_active = 1 then 'Y' else 'N' end [active],
                    case when og_external_user = 1 then 'Y' else 'N' end [external],
                    isnull(pj_name,'') [forced<br>project],
                    isnull(qu_desc,'') [default query],
                    case when u.us_enable_notifications = 1 then 'Y' else 'N' end [notif-<br>ications],
                    u.us_most_recent_login_datetime [most recent login]
                    from users u
                    inner join orgs on us_org = og_id
                    left outer join queries on us_default_query = qu_id
                    left outer join projects on us_forced_project = pj_id
                    left outer join #t on us_id = pu_user
                    where us_created_user = $us
                    and us_active in (1 $inactive)
                    $filter_users
                    order by us_username;

                    drop table #t";
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - users",
                SelectedItem = MainMenuSections.Administration
            };

            var filterCookie = Request.Cookies[nameof(IndexModel.Filter)];

            if (filterCookie != null && !string.IsNullOrEmpty(filterCookie.Value))
            {
                ViewBag.Filter = filterCookie.Value;
                sql = sql.Replace($"$filter_users", "and u.us_username like '" + filterCookie.Value + "%'");
            }
            else
            {
                ViewBag.Filter = string.Empty;
                sql = sql.Replace("$filter_users", string.Empty);
            }

            var hideInactiveCookie = Request.Cookies[nameof(IndexModel.HideInactive)];

            if (hideInactiveCookie != null && !string.IsNullOrEmpty(hideInactiveCookie.Value) && Convert.ToBoolean(hideInactiveCookie.Value))
            {
                ViewBag.HideInactive = true;
                sql = sql.Replace("$inactive", "");
            }
            else
            {
                ViewBag.HideInactive = false;
                sql = sql.Replace("$inactive", ",0");
            }

            sql = sql.Replace("$us", Convert.ToString(this.security.User.Usid));

            var model = new SortableTableModel
            {
                DataSet = DbUtil.GetDataSet(sql),
                HtmlEncode = false
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(IndexModel model)
        {
            var dt = DateTime.Now;
            var ts = new TimeSpan(365, 0, 0, 0);

            var filterCookie = new HttpCookie(nameof(model.Filter), model.Filter)
            {
                Path = "/",
                Expires = dt.Add(ts)
            };

            Response.Cookies.Set(filterCookie);

            var hideInactiveCookie = new HttpCookie(nameof(model.HideInactive), model.HideInactive.ToString())
            {
                Path = "/",
                Expires = dt.Add(ts)
            };

            Response.Cookies.Set(hideInactiveCookie);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Create()
        {
            if (!this.security.User.IsAdmin)
            {
                // Check if the current user is an admin for any project
                var sql = @"select pu_project
                    from project_user_xref
                    where pu_user = $us
                    and pu_admin = 1"
                    .Replace("$us", Convert.ToString(this.security.User.Usid));
                var dsProjects = DbUtil.GetDataSet(sql);

                if (dsProjects.Tables[0].Rows.Count == 0)
                {
                    return Content("You not allowed to add users.");
                }
            }

            InitLists();

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - create user",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new EditModel
            {
                Active = true,
                BugsPerPage = 10,
                EnableBugListPopups = true,
                EnableNotifications = true,
                NotificationsSubscribedBugsReportedByMe = 4,
                NotificationsSubscribedBugsAssignedToMe = 4,
                NotificationsForAllOtherSubscribedBugs = 4,
                AutoSubscribeToAllItemsAssignedToYou = true,
                AutoSubscribeToAllItemsReportedByYou = true
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EditModel model)
        {
            var sql = string.Empty;

            if (!this.security.User.IsAdmin)
            {
                // Check if the current user is an admin for any project
                sql = @"select pu_project
                    from project_user_xref
                    where pu_user = $us
                    and pu_admin = 1"
                    .Replace("$us", Convert.ToString(this.security.User.Usid));

                var dsProjects = DbUtil.GetDataSet(sql);

                if (dsProjects.Tables[0].Rows.Count == 0)
                {
                    return Content("You not allowed to add users.");
                }
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError(nameof(EditModel.Password), "Password is required.");
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                if (!Util.CheckPasswordStrength(model.Password))
                {
                    ModelState.AddModelError(nameof(EditModel.Password), "Password is not difficult enough to guess.<br>Avoid common words.<br>Try using a mixture of lowercase, uppercase, digits, and special characters.");
                }

                if (model.ConfirmedPassword != model.Password)
                {
                    ModelState.AddModelError(nameof(EditModel.ConfirmedPassword), "Confirm Password must match Password.");
                }
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "User was not created.");

                InitLists();

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - create user",
                    SelectedItem = MainMenuSections.Administration
                };

                return View("Edit", model);
            }

            // See if the user already exists?
            sql = "select count(1) from users where us_username = N'$1'"
                .Replace("$1", model.Login);

            var userCount = (int)DbUtil.ExecuteScalar(sql);

            if (userCount == 0)
            {
                // MAW -- 2006/01/27 -- Converted to use new notification columns
                sql = @"
                    insert into users
                    (us_username, us_password,
                    us_firstname, us_lastname,
                    us_bugs_per_page,
                    us_use_fckeditor,
                    us_enable_bug_list_popups,
                    us_email,
                    us_active, us_admin,
                    us_enable_notifications,
                    us_send_notifications_to_self,
                    us_reported_notifications,
                    us_assigned_notifications,
                    us_subscribed_notifications,
                    us_auto_subscribe,
                    us_auto_subscribe_own_bugs,
                    us_auto_subscribe_reported_bugs,
                    us_default_query,
                    us_org,
                    us_signature,
                    us_forced_project,
                    us_created_user)

                    values (
                    N'$un', N'$pw', N'$fn', N'$ln',
                    $bp, $fk, $pp, N'$em',
                    $ac, $ad, $en,  $ss,
                    $rn, $an, $sn, $as,
                    $ao, $ar, $dq, $org, N'$sg',
                    $fp,
                    $createdby
                    );

                    select scope_identity()";

                sql = ReplaceVarsInQqlStatement(sql, model);
                sql = sql.Replace("$createdby", Convert.ToString(this.security.User.Usid));

                // only admins can create admins.
                if (this.security.User.IsAdmin)
                {
                    sql = sql.Replace("$ad", Util.BoolToString(model.Admin));
                }
                else
                {
                    sql = sql.Replace("$ad", "0");
                }

                // fill the password field with some junk, just temporarily.
                sql = sql.Replace("$pw", Convert.ToString(new Random().Next()));

                // insert the user
                var userId = Convert.ToInt32(DbUtil.ExecuteScalar(sql));

                // now encrypt the password and update the db
                Util.UpdateUserPassword(userId, model.Password);

                UpdateProjectUserXref(model, userId);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "User was not created.");
                ModelState.AddModelError("Login", "User already exists. Choose another username.");

                InitLists();

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - create user",
                    SelectedItem = MainMenuSections.Administration
                };

                return View("Edit", model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Update(int id)
        {
            var sql = string.Empty;

            if (!this.security.User.IsAdmin)
            {
                // Check if the current user is an admin for any project
                sql = @"select pu_project
                    from project_user_xref
                    where pu_user = $us
                    and pu_admin = 1"
                    .Replace("$us", Convert.ToString(this.security.User.Usid));

                var dsProjects = DbUtil.GetDataSet(sql);

                if (dsProjects.Tables[0].Rows.Count == 0)
                {
                    return Content("You not allowed to add users.");
                }
            }

            InitLists(id);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - update user",
                SelectedItem = MainMenuSections.Administration
            };


            if (!this.security.User.IsAdmin)
            {
                // logged in user is a project level admin

                // get values for permissions grid
                // Table 0
                sql = @"
                    select pj_id, pj_name,
                    isnull(a.pu_permission_level,$dpl) [pu_permission_level],
                    isnull(a.pu_auto_subscribe,0) [pu_auto_subscribe],
                    isnull(a.pu_admin,0) [pu_admin]
                    from projects
                    inner join project_user_xref project_admin on pj_id = project_admin.pu_project
                    and project_admin.pu_user = $this_usid
                    and project_admin.pu_admin = 1
                    left outer join project_user_xref a on pj_id = a.pu_project
                    and a.pu_user = $us
                    order by pj_name;"
                    .Replace("$this_usid", Convert.ToString(this.security.User.Usid));
            }
            else // user is a real admin
            {
                // Table 0

                // populate permissions grid
                sql = @"
                    select pj_id, pj_name,
                    isnull(pu_permission_level,$dpl) [pu_permission_level],
                    isnull(pu_auto_subscribe,0) [pu_auto_subscribe],
                    isnull(pu_admin,0) [pu_admin]
                    from projects
                    left outer join project_user_xref on pj_id = pu_project
                    and pu_user = $us
                    order by pj_name;";
            }

            sql += @"
                select
                    us_username,
                    isnull(us_firstname,'') [us_firstname],
                    isnull(us_lastname,'') [us_lastname],
                    isnull(us_bugs_per_page,10) [us_bugs_per_page],
                    us_use_fckeditor,
                    us_enable_bug_list_popups,
                    isnull(us_email,'') [us_email],
                    us_active,
                    us_admin,
                    us_enable_notifications,
                    us_send_notifications_to_self,
                    us_reported_notifications,
                    us_assigned_notifications,
                    us_subscribed_notifications,
                    us_auto_subscribe,
                    us_auto_subscribe_own_bugs,
                    us_auto_subscribe_reported_bugs,
                    us_default_query,
                    us_org,
                    isnull(us_signature,'') [us_signature],
                    isnull(us_forced_project,0) [us_forced_project],
                    us_created_user
                    from users
                    where us_id = $us";

            sql = sql.Replace("$us", Convert.ToString(id));
            sql = sql.Replace("$dpl", ((int)this.applicationSettings.DefaultPermissionLevel).ToString());

            var ds = DbUtil.GetDataSet(sql);

            // get the values for this existing user
            var dr = ds.Tables[1].Rows[0];

            // check if project admin is allowed to edit this user
            if (!this.security.User.IsAdmin)
            {
                if (this.security.User.Usid != (int)dr["us_created_user"])
                {
                    return Content("You not allowed to edit this user, because you didn't create it.");
                }
                else if ((int)dr["us_admin"] == 1)
                {
                    return Content("You not allowed to edit this user, because it is an admin.");
                }
            }

            var model = new EditModel
            {
                Id = id,
                Login = (string)dr["us_username"],
                FirstName = (string)dr["us_firstname"],
                LastName = (string)dr["us_lastname"],
                Email = (string)dr["us_email"],
                EmailSignature = (string)dr["us_signature"],
                Active = Convert.ToBoolean((int)dr["us_active"]),
                Admin = Convert.ToBoolean((int)dr["us_admin"]),
                OrganizationId = (int)dr["us_org"],
                DefaultQueryId = (int)dr["us_default_query"],
                BugsPerPage = (int)dr["us_bugs_per_page"],
                EnableBugListPopups = Convert.ToBoolean((int)dr["us_enable_bug_list_popups"]),
                EditText = Convert.ToBoolean((int)dr["us_use_fckeditor"]),
                EnableNotifications = Convert.ToBoolean((int)dr["us_enable_notifications"]),
                NotificationsSubscribedBugsReportedByMe = (int)dr["us_reported_notifications"],
                NotificationsSubscribedBugsAssignedToMe = (int)dr["us_assigned_notifications"],
                NotificationsForAllOtherSubscribedBugs = (int)dr["us_subscribed_notifications"],
                AutoSubscribeToAllItems = Convert.ToBoolean((int)dr["us_auto_subscribe"]),
                AutoSubscribeToAllItemsAssignedToYou = Convert.ToBoolean((int)dr["us_auto_subscribe_own_bugs"]),
                AutoSubscribeToAllItemsReportedByYou = Convert.ToBoolean((int)dr["us_auto_subscribe_reported_bugs"]),
                SendNotificationsEvenForItemsAddOrChange = Convert.ToBoolean((int)dr["us_send_notifications_to_self"]),
                ForcedProjectId = (int)dr["us_forced_project"]
            };

            foreach (DataRow dr2 in ds.Tables[0].Rows)
            {
                if ((int)dr2["pu_auto_subscribe"] == 1)
                {
                    model.AutoSubscribePerProjectIds.Add((int)dr2["pj_id"]);
                }

                if ((int)dr2["pu_admin"] == 1)
                {
                    model.AdminProjectIds.Add((int)dr2["pj_id"]);
                }
            }

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(EditModel model)
        {
            var sql = string.Empty;

            if (!this.security.User.IsAdmin)
            {
                // Check if the current user is an admin for any project
                sql = @"select pu_project
                    from project_user_xref
                    where pu_user = $us
                    and pu_admin = 1"
                    .Replace("$us", Convert.ToString(this.security.User.Usid));

                var dsProjects = DbUtil.GetDataSet(sql);

                if (dsProjects.Tables[0].Rows.Count == 0)
                {
                    return Content("You not allowed to add users.");
                }
            }

            if (!string.IsNullOrEmpty(model.Password))
            {
                if (!Util.CheckPasswordStrength(model.Password))
                {
                    ModelState.AddModelError(nameof(EditModel.Password), "Password is not difficult enough to guess.<br>Avoid common words.<br>Try using a mixture of lowercase, uppercase, digits, and special characters.");
                }

                if (model.ConfirmedPassword != model.Password)
                {
                    ModelState.AddModelError(nameof(EditModel.ConfirmedPassword), "Confirm Password must match Password.");
                }
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "User was not updated.");

                InitLists();

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - update user",
                    SelectedItem = MainMenuSections.Administration
                };

                return View("Edit", model);
            }

            // See if the user already exists?
            sql = @"select count(1)
                from users where us_username = N'$1' and us_id <> $2";
            sql = sql.Replace("$1", model.Login);
            sql = sql.Replace("$2", Convert.ToString(model.Id));

            var userCount = (int)DbUtil.ExecuteScalar(sql);

            if (userCount == 0)
            {
                sql = @"
                    update users set
                    us_username = N'$un',
                    us_firstname = N'$fn',
                    us_lastname = N'$ln',
                    us_bugs_per_page = N'$bp',
                    us_use_fckeditor = $fk,
                    us_enable_bug_list_popups = $pp,
                    us_email = N'$em',
                    us_active = $ac,
                    us_admin = $ad,
                    us_enable_notifications = $en,
                    us_send_notifications_to_self = $ss,
                    us_reported_notifications = $rn,
                    us_assigned_notifications = $an,
                    us_subscribed_notifications = $sn,
                    us_auto_subscribe = $as,
                    us_auto_subscribe_own_bugs = $ao,
                    us_auto_subscribe_reported_bugs = $ar,
                    us_default_query = $dq,
                    us_org = $org,
                    us_signature = N'$sg',
                    us_forced_project = $fp
                    where us_id = $id";

                sql = ReplaceVarsInQqlStatement(sql, model);

                DbUtil.ExecuteNonQuery(sql);

                // update the password
                if (!string.IsNullOrEmpty(model.Password))
                {
                    Util.UpdateUserPassword(model.Id, model.Password);
                }

                UpdateProjectUserXref(model, model.Id);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "User was not updated.");
                ModelState.AddModelError(nameof(EditModel.Login), "Username already exists.   Choose another username.");

                InitLists();

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - update user",
                    SelectedItem = MainMenuSections.Administration
                };

                return View("Edit", model);
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Copy(int id)
        {
            var sql = string.Empty;

            if (!this.security.User.IsAdmin)
            {
                // Check if the current user is an admin for any project
                sql = @"select pu_project
                    from project_user_xref
                    where pu_user = $us
                    and pu_admin = 1"
                    .Replace("$us", Convert.ToString(this.security.User.Usid));

                var dsProjects = DbUtil.GetDataSet(sql);

                if (dsProjects.Tables[0].Rows.Count == 0)
                {
                    return Content("You not allowed to add users.");
                }
            }

            InitLists(id);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - update user",
                SelectedItem = MainMenuSections.Administration
            };


            if (!this.security.User.IsAdmin)
            {
                // logged in user is a project level admin

                // get values for permissions grid
                // Table 0
                sql = @"
                    select pj_id, pj_name,
                    isnull(a.pu_permission_level,$dpl) [pu_permission_level],
                    isnull(a.pu_auto_subscribe,0) [pu_auto_subscribe],
                    isnull(a.pu_admin,0) [pu_admin]
                    from projects
                    inner join project_user_xref project_admin on pj_id = project_admin.pu_project
                    and project_admin.pu_user = $this_usid
                    and project_admin.pu_admin = 1
                    left outer join project_user_xref a on pj_id = a.pu_project
                    and a.pu_user = $us
                    order by pj_name;"
                    .Replace("$this_usid", Convert.ToString(this.security.User.Usid));
            }
            else // user is a real admin
            {
                // Table 0

                // populate permissions grid
                sql = @"
                    select pj_id, pj_name,
                    isnull(pu_permission_level,$dpl) [pu_permission_level],
                    isnull(pu_auto_subscribe,0) [pu_auto_subscribe],
                    isnull(pu_admin,0) [pu_admin]
                    from projects
                    left outer join project_user_xref on pj_id = pu_project
                    and pu_user = $us
                    order by pj_name;";
            }

            sql += @"
                select
                    us_username,
                    isnull(us_firstname,'') [us_firstname],
                    isnull(us_lastname,'') [us_lastname],
                    isnull(us_bugs_per_page,10) [us_bugs_per_page],
                    us_use_fckeditor,
                    us_enable_bug_list_popups,
                    isnull(us_email,'') [us_email],
                    us_active,
                    us_admin,
                    us_enable_notifications,
                    us_send_notifications_to_self,
                    us_reported_notifications,
                    us_assigned_notifications,
                    us_subscribed_notifications,
                    us_auto_subscribe,
                    us_auto_subscribe_own_bugs,
                    us_auto_subscribe_reported_bugs,
                    us_default_query,
                    us_org,
                    isnull(us_signature,'') [us_signature],
                    isnull(us_forced_project,0) [us_forced_project],
                    us_created_user
                    from users
                    where us_id = $us";

            sql = sql.Replace("$us", Convert.ToString(id));
            sql = sql.Replace("$dpl", ((int)this.applicationSettings.DefaultPermissionLevel).ToString());

            var ds = DbUtil.GetDataSet(sql);

            // get the values for this existing user
            var dr = ds.Tables[1].Rows[0];

            // check if project admin is allowed to edit this user
            if (!this.security.User.IsAdmin)
            {
                if (this.security.User.Usid != (int)dr["us_created_user"])
                {
                    return Content("You not allowed to edit this user, because you didn't create it.");
                }
                else if ((int)dr["us_admin"] == 1)
                {
                    return Content("You not allowed to edit this user, because it is an admin.");
                }
            }

            var model = new EditModel
            {
                Login = "Enter username here",
                FirstName = string.Empty,
                LastName = string.Empty,
                Email = string.Empty,
                EmailSignature = string.Empty,
                Active = Convert.ToBoolean((int)dr["us_active"]),
                Admin = Convert.ToBoolean((int)dr["us_admin"]),
                OrganizationId = (int)dr["us_org"],
                DefaultQueryId = (int)dr["us_default_query"],
                BugsPerPage = (int)dr["us_bugs_per_page"],
                EnableBugListPopups = Convert.ToBoolean((int)dr["us_enable_bug_list_popups"]),
                EditText = Convert.ToBoolean((int)dr["us_use_fckeditor"]),
                EnableNotifications = Convert.ToBoolean((int)dr["us_enable_notifications"]),
                NotificationsSubscribedBugsReportedByMe = (int)dr["us_reported_notifications"],
                NotificationsSubscribedBugsAssignedToMe = (int)dr["us_assigned_notifications"],
                NotificationsForAllOtherSubscribedBugs = (int)dr["us_subscribed_notifications"],
                AutoSubscribeToAllItems = Convert.ToBoolean((int)dr["us_auto_subscribe"]),
                AutoSubscribeToAllItemsAssignedToYou = Convert.ToBoolean((int)dr["us_auto_subscribe_own_bugs"]),
                AutoSubscribeToAllItemsReportedByYou = Convert.ToBoolean((int)dr["us_auto_subscribe_reported_bugs"]),
                SendNotificationsEvenForItemsAddOrChange = Convert.ToBoolean((int)dr["us_send_notifications_to_self"]),
                ForcedProjectId = (int)dr["us_forced_project"]
            };

            foreach (DataRow dr2 in ds.Tables[0].Rows)
            {
                if ((int)dr2["pu_auto_subscribe"] == 1)
                {
                    model.AutoSubscribePerProjectIds.Add((int)dr2["pj_id"]);
                }

                if ((int)dr2["pu_admin"] == 1)
                {
                    model.AdminProjectIds.Add((int)dr2["pj_id"]);
                }
            }

            return View("Edit", model);
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            if (!this.security.User.IsAdmin)
            {
                var sql = @"select us_created_user, us_admin from users where us_id = $us"
                    .Replace("$us", id.ToString());

                var dr = DbUtil.GetDataRow(sql);

                if (this.security.User.Usid != (int)dr["us_created_user"])
                {
                    return Content("You not allowed to delete this user, because you didn't create it.");
                }
                else if ((int)dr["us_admin"] == 1)
                {
                    return Content("You not allowed to delete this user, because it is an admin.");
                }
            }

            var sql2 = @"declare @cnt int
                select @cnt = count(1) from bugs where bg_reported_user = $us or bg_assigned_to_user = $us
                if @cnt = 0
                begin
                    select @cnt = count(1) from bug_posts where bp_user = $us
                end
                select us_username, @cnt [cnt] from users where us_id = $us"
                .Replace("$us", id.ToString());

            var dr2 = DbUtil.GetDataRow(sql2);

            if ((int)dr2["cnt"] > 0)
            {
                return Content($"You can't delete user \"{dr2["us_username"]}\" because some bugs still reference it.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete user",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new DeleteModel
            {
                Id = id,
                Name = (string)dr2["us_username"]
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(DeleteModel model)
        {
            if (!this.security.User.IsAdmin)
            {
                var sql = @"select us_created_user, us_admin from users where us_id = $us"
                    .Replace("$us", model.Id.ToString());

                var dr = DbUtil.GetDataRow(sql);

                if (this.security.User.Usid != (int)dr["us_created_user"])
                {
                    return Content("You not allowed to delete this user, because you didn't create it.");
                }
                else if ((int)dr["us_admin"] == 1)
                {
                    return Content("You not allowed to delete this user, because it is an admin.");
                }
            }

            // do delete here
            var sqlDelete = @"
                delete from emailed_links where el_username in (select us_username from users where us_id = $us)
                delete users where us_id = $us
                delete project_user_xref where pu_user = $us
                delete bug_subscriptions where bs_user = $us
                delete bug_user where bu_user = $us
                delete queries where qu_user = $us
                delete queued_notifications where qn_user = $us
                delete dashboard_items where ds_user = $us"
                .Replace("$us", model.Id.ToString());

            DbUtil.ExecuteNonQuery(sqlDelete);

            return RedirectToAction(nameof(Index));
        }

        private void InitLists(int id = -1)
        {
            var sql = string.Empty;

            if (!this.security.User.IsAdmin)
            {
                // logged in user is a project level admin

                // get values for permissions grid
                // Table 0
                sql = @"
                    select pj_id, pj_name,
                    isnull(a.pu_permission_level,$dpl) [pu_permission_level],
                    isnull(a.pu_auto_subscribe,0) [pu_auto_subscribe],
                    isnull(a.pu_admin,0) [pu_admin]
                    from projects
                    inner join project_user_xref project_admin on pj_id = project_admin.pu_project
                    and project_admin.pu_user = $this_usid
                    and project_admin.pu_admin = 1
                    left outer join project_user_xref a on pj_id = a.pu_project
                    and a.pu_user = $us
                    order by pj_name;"
                    .Replace("$this_usid", Convert.ToString(this.security.User.Usid));
            }
            else // user is a real admin
            {
                // Table 0

                // populate permissions grid
                sql = @"
                    select pj_id, pj_name,
                    isnull(pu_permission_level,$dpl) [pu_permission_level],
                    isnull(pu_auto_subscribe,0) [pu_auto_subscribe],
                    isnull(pu_admin,0) [pu_admin]
                    from projects
                    left outer join project_user_xref on pj_id = pu_project
                    and pu_user = $us
                    order by pj_name;";
            }

            sql += @"/* populate query dropdown */
                declare @org int
                set @org = null
                select @org = us_org from users where us_id = $us

                select qu_id, qu_desc
                from queries
                where (isnull(qu_user,0) = 0 and isnull(qu_org,0) = 0)
                or isnull(qu_user,0) = $us
                or isnull(qu_org,0) = isnull(@org,-1)
                order by qu_desc;";

            // Table 2
            if (this.security.User.IsAdmin)
            {
                sql += @"/* populate org dropdown 1 */
                    select og_id, og_name
                    from orgs
                    order by og_name;";
            }
            else
            {
                if (this.security.User.OtherOrgsPermissionLevel == SecurityPermissionLevel.PermissionAll)
                {
                    sql += @"/* populate org dropdown 2 */
                        select og_id, og_name
                        from orgs
                        where og_non_admins_can_use = 1
                        order by og_name;";
                }
                else
                {
                    sql += @"/* populate org dropdown 3 */
                        select 1; -- dummy";
                }
            }

            sql = sql.Replace("$us", Convert.ToString(id));
            sql = sql.Replace("$dpl", ((int)this.applicationSettings.DefaultPermissionLevel).ToString());

            var ds = DbUtil.GetDataSet(sql);

            // query dropdown
            ViewBag.Queries = new List<SelectListItem>();

            foreach (DataRowView row in ds.Tables[1].DefaultView)
            {
                ViewBag.Queries.Add(new SelectListItem
                {
                    Value = ((int)row["qu_id"]).ToString(),
                    Text = (string)row["qu_desc"],
                });
            }

            ViewBag.Projects = new List<SelectListItem>();
            ViewBag.ForcedProjects = new List<SelectListItem>();

            ViewBag.ForcedProjects.Add(new SelectListItem
            {
                Value = "0",
                Text = "[no forced project]"
            });

            foreach (DataRowView row in ds.Tables[0].DefaultView)
            {
                ViewBag.Projects.Add(new SelectListItem
                {
                    Value = ((int)row["pj_id"]).ToString(),
                    Text = (string)row["pj_name"]
                });

                ViewBag.ForcedProjects.Add(new SelectListItem
                {
                    Value = ((int)row["pj_id"]).ToString(),
                    Text = (string)row["pj_name"],
                });
            }

            ViewBag.Organizations = new List<SelectListItem>();

            if (this.security.User.IsAdmin || this.security.User.OtherOrgsPermissionLevel == SecurityPermissionLevel.PermissionAll)
            {
                ViewBag.Organizations.Add(new SelectListItem
                {
                    Value = string.Empty,
                    Text = "[select org]"
                });

                foreach (DataRowView row in ds.Tables[2].DefaultView)
                {
                    ViewBag.Organizations.Add(new SelectListItem
                    {
                        Value = ((int)row["og_id"]).ToString(),
                        Text = (string)row["og_name"],
                    });
                }
            }
            else
            {
                ViewBag.Organizations.Add(new SelectListItem
                {
                    Value = Convert.ToString(this.security.User.Org),
                    Text = this.security.User.OrgName
                });
            }

            // populate permissions grid
            ViewBag.ProjectPermissions = ds.Tables[0].DefaultView;

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

        private string ReplaceVarsInQqlStatement(string sql, EditModel model)
        {
            sql = sql.Replace("$un", model.Login);
            sql = sql.Replace("$fn", model.FirstName);
            sql = sql.Replace("$ln", model.LastName);
            sql = sql.Replace("$bp", model.BugsPerPage.ToString());
            sql = sql.Replace("$fk", Util.BoolToString(model.EditText));
            sql = sql.Replace("$pp", Util.BoolToString(model.EnableBugListPopups));
            sql = sql.Replace("$em", model.Email);
            sql = sql.Replace("$ac", Util.BoolToString(model.Active));
            sql = sql.Replace("$en", Util.BoolToString(model.EnableNotifications));
            sql = sql.Replace("$ss", Util.BoolToString(model.SendNotificationsEvenForItemsAddOrChange));
            sql = sql.Replace("$rn", model.NotificationsSubscribedBugsReportedByMe.ToString());
            sql = sql.Replace("$an", model.NotificationsSubscribedBugsAssignedToMe.ToString());
            sql = sql.Replace("$sn", model.NotificationsForAllOtherSubscribedBugs.ToString());
            sql = sql.Replace("$as", Util.BoolToString(model.AutoSubscribeToAllItems));
            sql = sql.Replace("$ao", Util.BoolToString(model.AutoSubscribeToAllItemsAssignedToYou));
            sql = sql.Replace("$ar", Util.BoolToString(model.AutoSubscribeToAllItemsReportedByYou));
            sql = sql.Replace("$dq", model.DefaultQueryId.ToString());
            sql = sql.Replace("$org", model.OrganizationId.Value.ToString());
            sql = sql.Replace("$sg", model.EmailSignature);
            sql = sql.Replace("$fp", model.ForcedProjectId.ToString());
            sql = sql.Replace("$id", Convert.ToString(model.Id));

            // only admins can create admins.
            if (this.security.User.IsAdmin)
            {
                sql = sql.Replace("$ad", Util.BoolToString(model.Admin));
            }
            else
            {
                sql = sql.Replace("$ad", "0");
            }

            return sql;
        }

        private void UpdateProjectUserXref(EditModel model, int userId)
        {
            var hashProjects = new Hashtable();

            foreach (var id in model.AutoSubscribePerProjectIds)
            {
                var p = new Project
                {
                    Id = id,
                    AutoSubscribe = 1,
                    MaybeInsert = true
                };

                hashProjects[p.Id] = p;
            }

            foreach (var id in model.AdminProjectIds)
            {
                if (hashProjects.ContainsKey(id))
                {
                    var p = (Project)hashProjects[id];

                    p.Admin = 1;
                    p.MaybeInsert = true;
                }
                else
                {
                    var p = new Project
                    {
                        Id = id,
                        Admin = 1,
                        MaybeInsert = true
                    };

                    hashProjects[p.Id] = p;
                }
            }

            //RadioButton rb;
            int permissionLevel;
            var defaultPermissionLevel = this.applicationSettings.DefaultPermissionLevel;

            foreach (var permission in model.Permissions)
            {
                if (permission.Value[0] == "0")
                {
                    permissionLevel = 0;
                }
                else
                {
                    if (permission.Value[0] == "1")
                    {
                        permissionLevel = 1;
                    }
                    else
                    {
                        if (permission.Value[0] == "3")
                        {
                            permissionLevel = 3;
                        }
                        else
                        {
                            permissionLevel = 2;
                        }
                    }
                }

                var pjId = Convert.ToInt32(permission.Key);

                if (hashProjects.ContainsKey(pjId))
                {
                    var pj = (Project)hashProjects[pjId];

                    pj.PermissionLevel = permissionLevel;

                    if (permissionLevel != defaultPermissionLevel)
                    {
                        pj.MaybeInsert = true;
                    }
                }
                else
                {
                    var pj = new Project
                    {
                        Id = pjId,
                        PermissionLevel = permissionLevel,
                        MaybeInsert = true
                    };

                    if (permissionLevel != defaultPermissionLevel)
                    {
                        pj.MaybeInsert = true;
                    }

                    hashProjects[pj.Id] = pj;
                }
            }

            var projects = string.Empty;

            foreach (Project p in hashProjects.Values)
            {
                if (p.MaybeInsert)
                {
                    if (!string.IsNullOrEmpty(projects))
                    {
                        projects += ",";
                    }

                    projects += Convert.ToString(p.Id);
                }
            }

            var sql = string.Empty;

            // Insert new recs - we will update them later
            // Downstream logic is now simpler in that it just deals with existing recs
            if (!string.IsNullOrEmpty(projects))
            {
                sql += @"
                    insert into project_user_xref (pu_project, pu_user, pu_auto_subscribe)
                    select pj_id, $us, 0
                    from projects
                    where pj_id in ($projects)
                    and pj_id not in (select pu_project from project_user_xref where pu_user = $us);";

                sql = sql.Replace("$projects", projects);
            }

            // First turn everything off, then turn selected ones on.
            sql += @"
                update project_user_xref
                set pu_auto_subscribe = 0,
                pu_admin = 0,
                pu_permission_level = $dpl
                where pu_user = $us;";

            projects = string.Empty;

            foreach (Project p in hashProjects.Values)
            {
                if (p.AutoSubscribe == 1)
                {
                    if (!string.IsNullOrEmpty(projects))
                    {
                        projects += ",";
                    }

                    projects += Convert.ToString(p.Id);
                }
            }

            var autoSubscribeProjects = projects; // save for later

            if (!string.IsNullOrEmpty(projects))
            {
                sql += @"
                update project_user_xref
                set pu_auto_subscribe = 1
                where pu_user = $us
                and pu_project in ($projects);";

                sql = sql.Replace("$projects", projects);
            }

            if (this.security.User.IsAdmin)
            {
                projects = string.Empty;

                foreach (Project p in hashProjects.Values)
                {
                    if (p.Admin == 1)
                    {
                        if (!string.IsNullOrEmpty(projects))
                        {
                            projects += ",";
                        }

                        projects += Convert.ToString(p.Id);
                    }
                }

                if (!string.IsNullOrEmpty(projects))
                {
                    sql += @"
                        update project_user_xref
                        set pu_admin = 1
                        where pu_user = $us
                        and pu_project in ($projects);";

                    sql = sql.Replace("$projects", projects);
                }
            }

            // update permission levels to 0
            projects = string.Empty;

            foreach (Project p in hashProjects.Values)
            {
                if (p.PermissionLevel == 0)
                {
                    if (!string.IsNullOrEmpty(projects))
                    {
                        projects += ",";
                    }

                    projects += Convert.ToString(p.Id);
                }
            }

            if (!string.IsNullOrEmpty(projects))
            {
                sql += @"
                    update project_user_xref
                    set pu_permission_level = 0
                    where pu_user = $us
                    and pu_project in ($projects);";

                sql = sql.Replace("$projects", projects);
            }

            // update permission levels to 1
            projects = string.Empty;

            foreach (Project p in hashProjects.Values)
            {
                if (p.PermissionLevel == 1)
                {
                    if (!string.IsNullOrEmpty(projects))
                    {
                        projects += ",";
                    }

                    projects += Convert.ToString(p.Id);
                }
            }

            if (!string.IsNullOrEmpty(projects))
            {
                sql += @"
                    update project_user_xref
                    set pu_permission_level = 1
                    where pu_user = $us
                    and pu_project in ($projects);";

                sql = sql.Replace("$projects", projects);
            }

            // update permission levels to 2
            projects = string.Empty;

            foreach (Project p in hashProjects.Values)
            {
                if (p.PermissionLevel == 2)
                {
                    if (!string.IsNullOrEmpty(projects))
                    {
                        projects += ",";
                    }

                    projects += Convert.ToString(p.Id);
                }
            }

            if (!string.IsNullOrEmpty(projects))
            {
                sql += @"
                    update project_user_xref
                    set pu_permission_level = 2
                    where pu_user = $us
                    and pu_project in ($projects);";

                sql = sql.Replace("$projects", projects);
            }

            // update permission levels to 3
            projects = string.Empty;

            foreach (Project p in hashProjects.Values)
            {
                if (p.PermissionLevel == 3)
                {
                    if (!string.IsNullOrEmpty(projects))
                    {
                        projects += ",";
                    }

                    projects += Convert.ToString(p.Id);
                }
            }

            if (projects != string.Empty)
            {
                sql += @"
                    update project_user_xref
                    set pu_permission_level = 3
                    where pu_user = $us
                    and pu_project in ($projects);";

                sql = sql.Replace("$projects", projects);
            }

            // apply subscriptions retroactively
            if (model.ApplySubscriptionChangesRetroactively)
            {
                sql = @"
                    delete from bug_subscriptions where bs_user = $us;";

                if (model.AutoSubscribeToAllItems)
                {
                    sql += @"
                        insert into bug_subscriptions (bs_bug, bs_user)
                            select bg_id, $us from bugs;";
                }
                else
                {
                    if (model.AutoSubscribeToAllItemsReportedByYou)
                    {
                        sql += @"
                            insert into bug_subscriptions (bs_bug, bs_user)
                            select bg_id, $us from bugs where bg_reported_user = $us
                            and bg_id not in (select bs_bug from bug_subscriptions where bs_user = $us);";
                    }

                    if (model.AutoSubscribeToAllItemsAssignedToYou)
                    {
                        sql += @"
                            insert into bug_subscriptions (bs_bug, bs_user)
                            select bg_id, $us from bugs where bg_assigned_to_user = $us
                            and bg_id not in (select bs_bug from bug_subscriptions where bs_user = $us);";
                    }

                    if (autoSubscribeProjects != string.Empty)
                    {
                        sql += @"
                            insert into bug_subscriptions (bs_bug, bs_user)
                            select bg_id, $us from bugs where bg_project in ($projects)
                            and bg_id not in (select bs_bug from bug_subscriptions where bs_user = $us);";

                        sql = sql.Replace("$projects", autoSubscribeProjects);
                    }
                }
            }

            sql = sql.Replace("$us", userId.ToString());
            sql = sql.Replace("$dpl", this.applicationSettings.DefaultPermissionLevel.ToString());

            DbUtil.ExecuteNonQuery(sql);
        }

        private class Project
        {
            public int Admin;
            public int AutoSubscribe;
            public int Id;
            public bool MaybeInsert;
            public int PermissionLevel;
        }
    }
}