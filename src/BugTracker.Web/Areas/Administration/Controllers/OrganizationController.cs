/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Net;
    using System.Web.Mvc;
    using System.Web.UI;
    using Changing.Results;
    using Core;
    using Core.Controls;
    using Models.Organization;
    using Querying;
    using Tracking.Querying.Organizations;
    using Web.Models;

    [Authorize(Roles = ApplicationRoles.Administrator)]
    [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
    public class OrganizationController : Controller
    {
        private readonly IApplicationFacade applicationFacade;
        private readonly IApplicationSettings applicationSettings;
        private readonly IQueryBuilder queryBuilder;
        private readonly ISecurity security;

        public OrganizationController(
            IApplicationSettings applicationSettings,
            ISecurity security,
            IApplicationFacade applicationFacade,
            IQueryBuilder queryBuilder)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;

            this.applicationFacade = applicationFacade;
            this.queryBuilder = queryBuilder;
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

            var query = this.queryBuilder
                .From<IOrganizationSource>()
                .To<IOrganizationListResult>()
                //TODO: uncomment when manual sorting
                //.Sort()
                //.AscendingBy(x => x.Name)
                .Build();

            var result = this.applicationFacade
                .Run(query);

            return View(result);
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

            var query = this.queryBuilder
                .From<IOrganizationSource>()
                .To<IOrganizationStateResult>()
                .Filter()
                .Equal(x => x.Id, id)
                .Build();

            var result = this.applicationFacade
                .Run(query);

            var model = new EditModel
            {
                Id = id,
                Name = result.Name,
                Domain = result.Domain,
                Active = Convert.ToBoolean(result.Active),

                OtherOrgsPermissionLevel = (SecurityPermissionLevel)result.OtherOrgsPermissionLevel,

                CanSearch = Convert.ToBoolean(result.CanSearch),
                ExternalUser = Convert.ToBoolean(result.ExternalUser),
                CanOnlySeeOwnReported = Convert.ToBoolean(result.CanOnlySeeOwnReported),
                CanBeAssignedTo = Convert.ToBoolean(result.CanBeAssignedTo),
                NonAdminsCanUse = Convert.ToBoolean(result.NonAdminsCanUse),

                ProjectFieldPermissionLevel = (SecurityPermissionLevel)result.ProjectFieldPermissionLevel,
                OrgFieldPermissionLevel = (SecurityPermissionLevel)result.OrgFieldPermissionLevel,
                CategoryFieldPermissionLevel = (SecurityPermissionLevel)result.CategoryFieldPermissionLevel,
                PriorityFieldPermissionLevel = (SecurityPermissionLevel)result.PriorityFieldPermissionLevel,
                StatusFieldPermissionLevel = (SecurityPermissionLevel)result.StatusFieldPermissionLevel,
                AssignedToFieldPermissionLevel = (SecurityPermissionLevel)result.AssignedToFieldPermissionLevel,
                UdfFieldPermissionLevel = (SecurityPermissionLevel)result.UdfFieldPermissionLevel,
                TagsFieldPermissionLevel = (SecurityPermissionLevel)result.TagsFieldPermissionLevel,

                CanEditSql = Convert.ToBoolean(result.CanEditSql),
                CanDeleteBug = Convert.ToBoolean(result.CanDeleteBug),
                CanEditAndDeletePosts = Convert.ToBoolean(result.CanEditAndDeletePosts),
                CanMergeBugs = Convert.ToBoolean(result.CanMergeBugs),
                CanMassEditBugs = Convert.ToBoolean(result.CanMassEditBugs),
                CanUseReports = Convert.ToBoolean(result.CanUseReports),
                CanEditReports = Convert.ToBoolean(result.CanEditReports),
                CanViewTasks = Convert.ToBoolean(result.CanViewTasks),
                CanEditTasks = Convert.ToBoolean(result.CanEditTasks),
                CanAssignToInternalUsers = Convert.ToBoolean(result.CanAssignToInternalUsers)
            };

            ViewBag.CustomColumns = Util.GetCustomColumns();
            ViewBag.DictCustomFieldPermissionLevel = new Dictionary<string, int>();

            // Get this entry's data from the db and fill in the form
            var sql = @"select *,isnull(og_domain,'') og_domain2 from orgs where og_id = $og_id"
                .Replace("$og_id", Convert.ToString(id));

            var dr = DbUtil.GetDataRow(sql);

            foreach (DataRow drCustom in ViewBag.CustomColumns.Tables[0].Rows)
            {
                var bgName = (string)drCustom["name"];
                var obj = dr[$"og_{bgName}_field_permission_level"];
                SecurityPermissionLevel permission;

                if (Convert.IsDBNull(obj))
                    permission = SecurityPermissionLevel.PermissionAll;
                else
                    permission = (SecurityPermissionLevel)(int)obj;

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
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete organization",
                SelectedItem = MainMenuSections.Administration
            };

            if (TempData["Errors"] is IReadOnlyCollection<IFailError> failErrors)
                foreach (var failError in failErrors)
                    ModelState.AddModelError(failError.Property, failError.Message);

            if (TempData["Model"] is DeleteModel model) return View(model);

            var query = this.queryBuilder
                .From<IOrganizationSource>()
                .To<IOrganizationDeletePreviewResult>()
                .Filter()
                .Equal(x => x.Id, id)
                .Build();

            var result = this.applicationFacade
                .Run(query);

            model = new DeleteModel
            {
                Id = result.Id,
                Name = result.Name
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(DeleteModel model)
        {
            this.applicationFacade
                .Run(model, out var commandResult);

            switch (commandResult)
            {
                case IFailCommandResult fail:
                    TempData["Model"] = model;
                    TempData["Errors"] = fail.Errors;

                    return RedirectToAction(nameof(Delete), new { id = model.Id });
                case INotAuthorizedCommandResult _:
                    return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
                default:
                    return RedirectToAction(nameof(Index));
            }
        }
    }
}