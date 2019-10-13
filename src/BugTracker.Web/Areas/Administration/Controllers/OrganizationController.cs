/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Controllers
{
    using BugTracker.Web.Areas.Administration.Models.Organization;
    using BugTracker.Web.Core;
    using BugTracker.Web.Core.Controls;
    using BugTracker.Web.Models;
    using System.Web;
    using System.Web.Mvc;

    public class OrganizationController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;

        public OrganizationController(
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

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - priorities",
                SelectedItem = MainMenuSections.Administration
            };

            var dataSet = DbUtil.GetDataSet(
                @"select og_id [id],
                '<a href=" + VirtualPathUtility.ToAbsolute("~/Administration/Organization/Update/") + @"' + convert(varchar,og_id) + '>edit</a>' [$no_sort_edit],
                '<a href=" + VirtualPathUtility.ToAbsolute("~/Administration/Organization/Delete/") + @"' + convert(varchar,og_id) + '>delete</a>' [$no_sort_delete],
                og_name[desc],
                case when og_active = 1 then 'Y' else 'N' end [active],
                case when og_can_search = 1 then 'Y' else 'N' end [can<br>search],
                case when og_non_admins_can_use = 1 then 'Y' else 'N' end [non-admin<br>can use],
                case when og_can_only_see_own_reported = 1 then 'Y' else 'N' end [can see<br>only own bugs],
                case
                    when og_other_orgs_permission_level = 0 then 'None'
                    when og_other_orgs_permission_level = 1 then 'Read Only'
                    else 'Add/Edit' end [other orgs<br>permission<br>level],
                case when og_external_user = 1 then 'Y' else 'N' end [external],
                case when og_can_be_assigned_to = 1 then 'Y' else 'N' end [can<br>be assigned to],
                case
                    when og_status_field_permission_level = 0 then 'None'
                    when og_status_field_permission_level = 1 then 'Read Only'
                    else 'Add/Edit' end [status<br>permission<br>level],
                case
                    when og_assigned_to_field_permission_level = 0 then 'None'
                    when og_assigned_to_field_permission_level = 1 then 'Read Only'
                    else 'Add/Edit' end [assigned to<br>permission<br>level],
                case
                    when og_priority_field_permission_level = 0 then 'None'
                    when og_priority_field_permission_level = 1 then 'Read Only'
                    else 'Add/Edit' end [priority<br>permission<br>level],
                isnull(og_domain,'')[domain]
                from orgs order by og_name");

            var model = new SortableTableModel
            {
                DataSet = dataSet,
                HtmlEncode = false
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Create()
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - create organization",
                SelectedItem = MainMenuSections.Administration
            };

            //this.og_active.Checked = true;
            ////other_orgs_permission_level.SelectedIndex = 2;
            //this.can_search.Checked = true;
            //this.can_be_assigned_to.Checked = true;
            //this.other_orgs.SelectedValue = "2";

            //this.project_field.SelectedValue = "2";
            //this.org_field.SelectedValue = "2";
            //this.category_field.SelectedValue = "2";
            //this.tags_field.SelectedValue = "2";
            //this.priority_field.SelectedValue = "2";
            //this.status_field.SelectedValue = "2";
            //this.assigned_to_field.SelectedValue = "2";
            //this.udf_field.SelectedValue = "2";

            //foreach (DataRow drCustom in this.DsCustom.Tables[0].Rows)
            //{
            //    var bgName = (string)drCustom["name"];
            //    this.DictCustomFieldPermissionLevel[bgName] = SecurityPermissionLevel.PermissionAll;
            //}

            var model = new EditModel
            {
                //BackgroundColor = "#ffffff"
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EditModel model)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            if (!ModelState.IsValid)
            {
                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - create organization",
                    SelectedItem = MainMenuSections.Administration
                };

                return View("Edit", model);
            }

            //var parameters = new Dictionary<string, string>
            //{
            //    { "$id", model.Id.ToString() },
            //    { "$na", model.Name.Replace("'", "''")},
            //    { "$ss", model.SortSequence.ToString() },
            //    { "$co", model.BackgroundColor.Replace("'", "''")},
            //    { "$st", (model.Style ?? string.Empty).Replace("'", "''")},
            //    { "$df", Util.BoolToString(model.Default)},
            //};

            //this.priorityService.Create(parameters);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Update(int id)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            // Get this entry's data from the db and fill in the form
            //var dataRow = this.priorityService.LoadOne(id);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - update organization",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new EditModel
            {
                //Id = id,
                //Name = dataRow.Name,
                //SortSequence = dataRow.SortSequence,
                //Style = dataRow.Style,
                //BackgroundColor = dataRow.BackgroundColor,
                //Default = dataRow.Default == 1
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(EditModel model)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            if (!ModelState.IsValid)
            {
                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - update organization",
                    SelectedItem = MainMenuSections.Administration
                };

                return View("Edit", model);
            }

            //var parameters = new Dictionary<string, string>
            //{
            //    { "$id", model.Id.ToString() },
            //    { "$na", model.Name.Replace("'", "''")},
            //    { "$ss", model.SortSequence.ToString() },
            //    { "$co", model.BackgroundColor.Replace("'", "''")},
            //    { "$st", (model.Style ?? string.Empty).Replace("'", "''")},
            //    { "$df", Util.BoolToString(model.Default)},
            //};

            //this.priorityService.Update(parameters);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            var sql = @"declare @cnt int
                select @cnt = count(1) from users where us_org = $1;
                select @cnt = @cnt + count(1) from queries where qu_org = $1;
                select @cnt = @cnt + count(1) from bugs where bg_org = $1;
                select og_name, @cnt [cnt] from orgs where og_id = $1"
                .Replace("$1", id.ToString());

            var dr = DbUtil.GetDataRow(sql);

            if ((int)dr["cnt"] > 0)
            {
                return Content($"You can't delete organization \"{dr["og_name"]}\" because some bugs still reference it.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete organization",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new DeleteModel
            {
                Id = id,
                Name = (string)dr["og_name"]
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(DeleteModel model)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            // do delete here
            var sql = @"delete orgs where og_id = $1"
                .Replace("$1", model.Id.ToString());

            DbUtil.ExecuteNonQuery(sql);

            return RedirectToAction(nameof(Index));
        }
    }
}