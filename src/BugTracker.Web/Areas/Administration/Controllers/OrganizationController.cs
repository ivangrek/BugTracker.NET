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
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;

    [Authorize(Roles = ApplicationRoles.Administrator)]
    [OutputCache(Location = OutputCacheLocation.None)]
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
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - organizations",
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
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - new organization",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new EditModel
            {
                Active = true,

                OtherOrgsPermissionLevel = SecurityPermissionLevel.PermissionAll,

                CanSearch = true,

                CategoryFieldPermissionLevel = SecurityPermissionLevel.PermissionAll,
                PriorityFieldPermissionLevel = SecurityPermissionLevel.PermissionAll,
                AssignedToFieldPermissionLevel = SecurityPermissionLevel.PermissionAll,
                StatusFieldPermissionLevel = SecurityPermissionLevel.PermissionAll,
                ProjectFieldPermissionLevel = SecurityPermissionLevel.PermissionAll,
                OrgFieldPermissionLevel = SecurityPermissionLevel.PermissionAll,
                UdfFieldPermissionLevel = SecurityPermissionLevel.PermissionAll,
                TagsFieldPermissionLevel = SecurityPermissionLevel.PermissionAll,

                CanAssignToInternalUsers = true
            };

            ViewBag.CustomColumns = Util.GetCustomColumns();
            ViewBag.DictCustomFieldPermissionLevel = new Dictionary<string, int>();

            foreach (DataRow drCustom in ViewBag.CustomColumns.Tables[0].Rows)
            {
                var bgName = (string)drCustom["name"];

                ViewBag.DictCustomFieldPermissionLevel[bgName] = (int)SecurityPermissionLevel.PermissionAll;
            }

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EditModel model)
        {
            ViewBag.DCustom = Util.GetCustomColumns();

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Organization was not created.");

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - new organization",
                    SelectedItem = MainMenuSections.Administration
                };

                ViewBag.DictCustomFieldPermissionLevel = new Dictionary<string, int>();

                foreach (DataRow drCustom in ViewBag.DCustom.Tables[0].Rows)
                {
                    var bgName = (string)drCustom["name"];

                    ViewBag.DictCustomFieldPermissionLevel[bgName] = (int)SecurityPermissionLevel.PermissionAll;
                }

                return View("Edit", model);
            }

            var sql = @"
                insert into orgs
                    (og_name,
                    og_domain,
                    og_active,
                    og_non_admins_can_use,
                    og_external_user,
                    og_can_edit_sql,
                    og_can_delete_bug,
                    og_can_edit_and_delete_posts,
                    og_can_merge_bugs,
                    og_can_mass_edit_bugs,
                    og_can_use_reports,
                    og_can_edit_reports,
                    og_can_be_assigned_to,
                    og_can_view_tasks,
                    og_can_edit_tasks,
                    og_can_search,
                    og_can_only_see_own_reported,
                    og_can_assign_to_internal_users,
                    og_other_orgs_permission_level,
                    og_project_field_permission_level,
                    og_org_field_permission_level,
                    og_category_field_permission_level,
                    og_tags_field_permission_level,
                    og_priority_field_permission_level,
                    og_status_field_permission_level,
                    og_assigned_to_field_permission_level,
                    og_udf_field_permission_level
                    $custom1$
                    )
                    values (
                    N'$name', 
                    N'$domain',
                    $active,
                    $non_admins_can_use,
                    $external_user,
                    $can_edit_sql,
                    $can_delete_bug,
                    $can_edit_and_delete_posts,
                    $can_merge_bugs,
                    $can_mass_edit_bugs,
                    $can_use_reports,
                    $can_edit_reports,
                    $can_be_assigned_to,
                    $can_view_tasks,
                    $can_edit_tasks,
                    $can_search,
                    $can_only_see_own_reported,
                    $can_assign_to_internal_users,
                    $other_orgs,
                    $flp_project,
                    $flp_org,
                    $flp_category,
                    $flp_tags,
                    $flp_priority,
                    $flp_status,
                    $flp_assigned_to,
                    $flp_udf
                    $custom2$
                )";

            sql = sql.Replace("$name", model.Name);
            sql = sql.Replace("$domain", model.Domain);
            sql = sql.Replace("$active", Util.BoolToString(model.Active));

            sql = sql.Replace("$other_orgs", Convert.ToString((int)model.OtherOrgsPermissionLevel));

            sql = sql.Replace("$can_search", Util.BoolToString(model.CanSearch));
            sql = sql.Replace("$external_user", Util.BoolToString(model.ExternalUser));
            sql = sql.Replace("$can_only_see_own_reported", Util.BoolToString(model.CanOnlySeeOwnReported));
            sql = sql.Replace("$can_be_assigned_to", Util.BoolToString(model.CanBeAssignedTo));
            sql = sql.Replace("$non_admins_can_use", Util.BoolToString(model.NonAdminsCanUse));

            sql = sql.Replace("$flp_project", Convert.ToString((int)model.ProjectFieldPermissionLevel));
            sql = sql.Replace("$flp_org", Convert.ToString((int)model.OrgFieldPermissionLevel));
            sql = sql.Replace("$flp_category", Convert.ToString((int)model.CategoryFieldPermissionLevel));
            sql = sql.Replace("$flp_tags", Convert.ToString((int)model.TagsFieldPermissionLevel));
            sql = sql.Replace("$flp_priority", Convert.ToString((int)model.PriorityFieldPermissionLevel));
            sql = sql.Replace("$flp_status", Convert.ToString((int)model.StatusFieldPermissionLevel));
            sql = sql.Replace("$flp_assigned_to", Convert.ToString((int)model.AssignedToFieldPermissionLevel));
            sql = sql.Replace("$flp_udf", Convert.ToString((int)model.UdfFieldPermissionLevel));

            sql = sql.Replace("$can_edit_sql", Util.BoolToString(model.CanEditSql));
            sql = sql.Replace("$can_delete_bug", Util.BoolToString(model.CanDeleteBug));
            sql = sql.Replace("$can_edit_and_delete_posts", Util.BoolToString(model.CanEditAndDeletePosts));
            sql = sql.Replace("$can_merge_bugs", Util.BoolToString(model.CanMergeBugs));
            sql = sql.Replace("$can_mass_edit_bugs", Util.BoolToString(model.CanMassEditBugs));
            sql = sql.Replace("$can_use_reports", Util.BoolToString(model.CanUseReports));
            sql = sql.Replace("$can_edit_reports", Util.BoolToString(model.CanEditReports));
            sql = sql.Replace("$can_view_tasks", Util.BoolToString(model.CanViewTasks));
            sql = sql.Replace("$can_edit_tasks", Util.BoolToString(model.CanEditTasks));
            sql = sql.Replace("$can_assign_to_internal_users", Util.BoolToString(model.CanAssignToInternalUsers));

            var custom1 = string.Empty;
            var custom2 = string.Empty;

            foreach (DataRow drCustom in ViewBag.DCustom.Tables[0].Rows)
            {
                var bgName = (string)drCustom["name"];
                var ogColName = $"og_{bgName}_field_permission_level";

                custom1 += ",[" + ogColName + "]";
                custom2 += "," + Util.SanitizeInteger(Request[bgName]);
            }

            sql = sql.Replace("$custom1$", custom1);
            sql = sql.Replace("$custom2$", custom2);

            DbUtil.ExecuteNonQuery(sql);

            return Redirect(nameof(Index));
        }

        [HttpGet]
        public ActionResult Update(int id)
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit organization",
                SelectedItem = MainMenuSections.Administration
            };

            // Get this entry's data from the db and fill in the form
            var sql = @"select *,isnull(og_domain,'') og_domain2 from orgs where og_id = $og_id"
                .Replace("$og_id", Convert.ToString(id));

            var dr = DbUtil.GetDataRow(sql);

            var model = new EditModel
            {
                Id = id,
                Name = (string)dr["og_name"],
                Domain = (string)dr["og_domain2"],
                Active = Convert.ToBoolean((int)dr["og_active"]),

                OtherOrgsPermissionLevel = (SecurityPermissionLevel)(int)dr["og_other_orgs_permission_level"],

                CanSearch = Convert.ToBoolean((int)dr["og_can_search"]),
                ExternalUser = Convert.ToBoolean((int)dr["og_external_user"]),
                CanOnlySeeOwnReported = Convert.ToBoolean((int)dr["og_can_only_see_own_reported"]),
                CanBeAssignedTo = Convert.ToBoolean((int)dr["og_can_be_assigned_to"]),
                NonAdminsCanUse = Convert.ToBoolean((int)dr["og_non_admins_can_use"]),

                ProjectFieldPermissionLevel = (SecurityPermissionLevel)(int)dr["og_project_field_permission_level"],
                OrgFieldPermissionLevel = (SecurityPermissionLevel)(int)dr["og_org_field_permission_level"],
                CategoryFieldPermissionLevel = (SecurityPermissionLevel)(int)dr["og_category_field_permission_level"],
                PriorityFieldPermissionLevel = (SecurityPermissionLevel)(int)dr["og_priority_field_permission_level"],
                StatusFieldPermissionLevel = (SecurityPermissionLevel)(int)dr["og_status_field_permission_level"],
                AssignedToFieldPermissionLevel = (SecurityPermissionLevel)(int)dr["og_assigned_to_field_permission_level"],
                UdfFieldPermissionLevel = (SecurityPermissionLevel)(int)dr["og_udf_field_permission_level"],
                TagsFieldPermissionLevel = (SecurityPermissionLevel)(int)dr["og_tags_field_permission_level"],

                CanEditSql = Convert.ToBoolean((int)dr["og_can_edit_sql"]),
                CanDeleteBug = Convert.ToBoolean((int)dr["og_can_delete_bug"]),
                CanEditAndDeletePosts = Convert.ToBoolean((int)dr["og_can_edit_and_delete_posts"]),
                CanMergeBugs = Convert.ToBoolean((int)dr["og_can_merge_bugs"]),
                CanMassEditBugs = Convert.ToBoolean((int)dr["og_can_mass_edit_bugs"]),
                CanUseReports = Convert.ToBoolean((int)dr["og_can_use_reports"]),
                CanEditReports = Convert.ToBoolean((int)dr["og_can_edit_reports"]),
                CanViewTasks = Convert.ToBoolean((int)dr["og_can_view_tasks"]),
                CanEditTasks = Convert.ToBoolean((int)dr["og_can_edit_tasks"]),
                CanAssignToInternalUsers = Convert.ToBoolean((int)dr["og_can_assign_to_internal_users"])
            };

            ViewBag.CustomColumns = Util.GetCustomColumns();
            ViewBag.DictCustomFieldPermissionLevel = new Dictionary<string, int>();

            foreach (DataRow drCustom in ViewBag.CustomColumns.Tables[0].Rows)
            {
                var bgName = (string)drCustom["name"];
                var obj = dr[$"og_{bgName}_field_permission_level"];
                SecurityPermissionLevel permission;

                if (Convert.IsDBNull(obj))
                {
                    permission = SecurityPermissionLevel.PermissionAll;
                }
                else
                {
                    permission = (SecurityPermissionLevel)(int)obj;
                }

                ViewBag.DictCustomFieldPermissionLevel[bgName] = (int)permission;
            }

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(EditModel model)
        {
            ViewBag.CustomColumns = Util.GetCustomColumns();

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Organization was not updated.");

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - edit organization",
                    SelectedItem = MainMenuSections.Administration
                };

                ViewBag.DictCustomFieldPermissionLevel = new Dictionary<string, int>();

                foreach (DataRow drCustom in ViewBag.CustomColumns.Tables[0].Rows)
                {
                    var bgName = (string)drCustom["name"];

                    ViewBag.DictCustomFieldPermissionLevel[bgName] = (int)SecurityPermissionLevel.PermissionAll;
                }

                return View("Edit", model);
            }

            var sql = @"
                update orgs set
                    og_name = N'$name',
                    og_domain = N'$domain',
                    og_active = $active,
                    og_non_admins_can_use = $non_admins_can_use,
                    og_external_user = $external_user,
                    og_can_edit_sql = $can_edit_sql,
                    og_can_delete_bug = $can_delete_bug,
                    og_can_edit_and_delete_posts = $can_edit_and_delete_posts,
                    og_can_merge_bugs = $can_merge_bugs,
                    og_can_mass_edit_bugs = $can_mass_edit_bugs,
                    og_can_use_reports = $can_use_reports,
                    og_can_edit_reports = $can_edit_reports,
                    og_can_be_assigned_to = $can_be_assigned_to,
                    og_can_view_tasks = $can_view_tasks,
                    og_can_edit_tasks = $can_edit_tasks,
                    og_can_search = $can_search,
                    og_can_only_see_own_reported = $can_only_see_own_reported,
                    og_can_assign_to_internal_users = $can_assign_to_internal_users,
                    og_other_orgs_permission_level = $other_orgs,
                    og_project_field_permission_level = $flp_project,
                    og_org_field_permission_level = $flp_org,
                    og_category_field_permission_level = $flp_category,
                    og_tags_field_permission_level = $flp_tags,
                    og_priority_field_permission_level = $flp_priority,
                    og_status_field_permission_level = $flp_status,
                    og_assigned_to_field_permission_level = $flp_assigned_to,
                    og_udf_field_permission_level = $flp_udf
                    $custom3$
                    where og_id = $og_id";

            sql = sql.Replace("$og_id", Convert.ToString(model.Id));

            sql = sql.Replace("$name", model.Name);
            sql = sql.Replace("$domain", model.Domain);
            sql = sql.Replace("$active", Util.BoolToString(model.Active));

            sql = sql.Replace("$other_orgs", Convert.ToString((int)model.OtherOrgsPermissionLevel));

            sql = sql.Replace("$can_search", Util.BoolToString(model.CanSearch));
            sql = sql.Replace("$external_user", Util.BoolToString(model.ExternalUser));
            sql = sql.Replace("$can_only_see_own_reported", Util.BoolToString(model.CanOnlySeeOwnReported));
            sql = sql.Replace("$can_be_assigned_to", Util.BoolToString(model.CanBeAssignedTo));
            sql = sql.Replace("$non_admins_can_use", Util.BoolToString(model.NonAdminsCanUse));

            sql = sql.Replace("$flp_project", Convert.ToString((int)model.ProjectFieldPermissionLevel));
            sql = sql.Replace("$flp_org", Convert.ToString((int)model.OrgFieldPermissionLevel));
            sql = sql.Replace("$flp_category", Convert.ToString((int)model.CategoryFieldPermissionLevel));
            sql = sql.Replace("$flp_tags", Convert.ToString((int)model.TagsFieldPermissionLevel));
            sql = sql.Replace("$flp_priority", Convert.ToString((int)model.PriorityFieldPermissionLevel));
            sql = sql.Replace("$flp_status", Convert.ToString((int)model.StatusFieldPermissionLevel));
            sql = sql.Replace("$flp_assigned_to", Convert.ToString((int)model.AssignedToFieldPermissionLevel));
            sql = sql.Replace("$flp_udf", Convert.ToString((int)model.UdfFieldPermissionLevel));

            sql = sql.Replace("$can_edit_sql", Util.BoolToString(model.CanEditSql));
            sql = sql.Replace("$can_delete_bug", Util.BoolToString(model.CanDeleteBug));
            sql = sql.Replace("$can_edit_and_delete_posts", Util.BoolToString(model.CanEditAndDeletePosts));
            sql = sql.Replace("$can_merge_bugs", Util.BoolToString(model.CanMergeBugs));
            sql = sql.Replace("$can_mass_edit_bugs", Util.BoolToString(model.CanMassEditBugs));
            sql = sql.Replace("$can_use_reports", Util.BoolToString(model.CanUseReports));
            sql = sql.Replace("$can_edit_reports", Util.BoolToString(model.CanEditReports));
            sql = sql.Replace("$can_view_tasks", Util.BoolToString(model.CanViewTasks));
            sql = sql.Replace("$can_edit_tasks", Util.BoolToString(model.CanEditTasks));
            sql = sql.Replace("$can_assign_to_internal_users", Util.BoolToString(model.CanAssignToInternalUsers));

            var custom3 = string.Empty;

            foreach (DataRow drCustom in ViewBag.CustomColumns.Tables[0].Rows)
            {
                var bgName = (string)drCustom["name"];
                var ogColName = $"og_{bgName}_field_permission_level";

                custom3 += ",[" + ogColName + "]=" + Util.SanitizeInteger(Request[bgName]);
            }

            sql = sql.Replace("$custom3$", custom3);

            DbUtil.ExecuteNonQuery(sql);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
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
            // do delete here
            var sql = @"delete orgs where og_id = $1"
                .Replace("$1", model.Id.ToString());

            DbUtil.ExecuteNonQuery(sql);

            return RedirectToAction(nameof(Index));
        }
    }
}