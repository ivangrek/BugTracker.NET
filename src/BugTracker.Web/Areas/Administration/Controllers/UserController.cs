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
    using System.Web;
    using System.Web.Mvc;

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
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

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
                    '<a href=" + VirtualPathUtility.ToAbsolute("~/Administration/User/Edit.aspx") + @"?copy=y&id=' + convert(varchar,u.us_id) + '>copy</a>' [$no_sort_add<br>like<br>this],
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
                    '<a href=" + VirtualPathUtility.ToAbsolute("~/Administration/User/Edit.aspx") + @"?copy=y&id=' + convert(varchar,u.us_id) + '>copy</a>' [$no_sort_add<br>like<br>this],
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

            var model = new SortableTableModel
            {
                //DataSet = this.priorityService.LoadList(),
                EditUrl = VirtualPathUtility.ToAbsolute("~/Administration/Priority/Update/"),
                DeleteUrl = VirtualPathUtility.ToAbsolute("~/Administration/Priority/Delete/"),
                HtmlEncode = false
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

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
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

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
    }
}