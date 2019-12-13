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
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;
    using Changing.Results;
    using Core;
    using Core.Controls;
    using Models.Project;
    using Querying;
    using Tracking.Changing.Projects.Commands;
    using Tracking.Querying.Projects;
    using Web.Models;

    [Authorize(Roles = ApplicationRoles.Administrator)]
    [OutputCache(Location = OutputCacheLocation.None)]
    public class ProjectController : Controller
    {
        private readonly IApplicationFacade applicationFacade;
        private readonly IApplicationSettings applicationSettings;
        private readonly IQueryBuilder queryBuilder;
        private readonly ISecurity security;

        public ProjectController(
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
                Title = $"{this.applicationSettings.AppTitle} - projects",
                SelectedItem = MainMenuSections.Administration
            };

            var query = this.queryBuilder
                .From<IProjectSource>()
                .To<IProjectListResult>()
                .Sort()
                .AscendingBy(x => x.Name)
                .Build();

            var result = this.applicationFacade
                .Run(query);

            var dataTable = new DataTable();

            dataTable.Columns.Add("id");
            dataTable.Columns.Add("$no_sort_edit");
            dataTable.Columns.Add("$no_sort_per user<br>permissions");
            dataTable.Columns.Add("$no_sort_delete");
            dataTable.Columns.Add("name");
            dataTable.Columns.Add("active");
            dataTable.Columns.Add("default user");
            dataTable.Columns.Add("auto assign<br>default user");
            dataTable.Columns.Add("auto subscribe<br>default user");
            dataTable.Columns.Add("receive items<br>via pop3");
            dataTable.Columns.Add("pop3 username");
            dataTable.Columns.Add("from email address");
            dataTable.Columns.Add("default");

            foreach (var row in result)
            {
                var editUrl =
                    $"<a href='{VirtualPathUtility.ToAbsolute($"~/Administration/Project/Update/{row.Id}")}'>edit</a>";
                var permissionsUrl =
                    $"<a href='{VirtualPathUtility.ToAbsolute($"~/Administration/Project/UpdateUserPermission/{row.Id}?projects=true")}'>permissions</a>";
                var deleteUrl =
                    $"<a href='{VirtualPathUtility.ToAbsolute($"~/Administration/Project/Delete/{row.Id}")}'>delete</a>";
                var activeValue = row.Active == 1
                    ? "Y"
                    : "N";

                var autoAssignDefaultUserValue = row.AutoAssignDefaultUser == 1
                    ? "Y"
                    : "N";

                var autoSubscribeDefaultUserValue = row.AutoSubscribeDefaultUser == 1
                    ? "Y"
                    : "N";

                var enablePop3Value = row.EnablePop3 == 1
                    ? "Y"
                    : "N";

                var defaultValue = row.Default == 1
                    ? "Y"
                    : "N";

                dataTable.Rows.Add(row.Id, editUrl, permissionsUrl, deleteUrl, row.Name, activeValue,
                    row.DefaultUserName, autoAssignDefaultUserValue, autoSubscribeDefaultUserValue, enablePop3Value,
                    row.Pop3Username,
                    row.Pop3EmailFrom, defaultValue);
            }

            var model = new SortableTableModel
            {
                DataTable = dataTable,
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
                Title = $"{this.applicationSettings.AppTitle} - new project",
                SelectedItem = MainMenuSections.Administration
            };

            //var userQuery = this.queryBuilderFactory
            //    .New<IUserListQuery>()
            //    .Build();

            //var userResult = this.idNamesOfUserQueryHandler
            //    .Handle(userQuery);

            ViewBag.DefaultUsers = new List<SelectListItem>();

            //foreach (var user in userResult)
            //{
            //    ViewBag.DefaultUsers.Add(new SelectListItem
            //    {
            //        Value = user.Id.ToString(),
            //        Text = user.Name,
            //    });
            //}

            if (TempData["Errors"] is IReadOnlyCollection<IFailError> failErrors)
                foreach (var failError in failErrors)
                    ModelState.AddModelError(failError.Property, failError.Message);

            if (TempData["Model"] is EditModel model) return View("Edit", model);

            model = new EditModel
            {
                Active = true
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EditModel model)
        {
            this.applicationFacade
                .Run<ICreateCommand>(model, out var commandResult);

            switch (commandResult)
            {
                case IFailCommandResult fail:
                    TempData["Model"] = model;
                    TempData["Errors"] = fail.Errors;

                    return RedirectToAction(nameof(Create));
                case INotAuthorizedCommandResult _:
                    return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
                default:
                    return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public ActionResult Update(int id)
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit project",
                SelectedItem = MainMenuSections.Administration
            };

            //var userQuery = this.queryBuilderFactory
            //    .New<IUserListQuery>()
            //    .Build();

            //var userResult = this.idNamesOfUserQueryHandler
            //    .Handle(userQuery);

            ViewBag.DefaultUsers = new List<SelectListItem>();

            //foreach (var user in userResult)
            //{
            //    ViewBag.DefaultUsers.Add(new SelectListItem
            //    {
            //        Value = user.Id.ToString(),
            //        Text = user.Name,
            //    });
            //}

            if (TempData["Errors"] is IReadOnlyCollection<IFailError> failErrors)
                foreach (var failError in failErrors)
                    ModelState.AddModelError(failError.Property, failError.Message);

            if (TempData["Model"] is EditModel model) return View("Edit", model);

            var query = this.queryBuilder
                .From<IProjectSource>()
                .To<IProjectStateResult>()
                .Filter()
                .Equal(x => x.Id, id)
                .Build();

            var result = this.applicationFacade
                .Run(query);

            model = new EditModel
            {
                Id = result.Id,
                Name = result.Name,
                Active = Convert.ToBoolean(result.Active),
                Default = Convert.ToBoolean(result.Default),
                DefaultUserId = result.DefaultUserId,

                AutoAssignDefaultUser = Convert.ToBoolean(result.AutoAssignDefaultUser),
                AutoSubscribeDefaultUser = Convert.ToBoolean(result.AutoSubscribeDefaultUser),

                EnablePop3 = Convert.ToBoolean(result.EnablePop3),
                Pop3Username = result.Pop3Username,
                Pop3EmailFrom = result.Pop3EmailFrom,

                Description = result.Description,

                EnableCustomDropdown1 = Convert.ToBoolean(result.EnableCustomDropdown1),
                CustomDropdown1Label = result.CustomDropdown1Label,
                CustomDropdown1Values = result.CustomDropdown1Values,

                EnableCustomDropdown2 = Convert.ToBoolean(result.EnableCustomDropdown2),
                CustomDropdown2Label = result.CustomDropdown2Label,
                CustomDropdown2Values = result.CustomDropdown2Values,

                EnableCustomDropdown3 = Convert.ToBoolean(result.EnableCustomDropdown3),
                CustomDropdown3Label = result.CustomDropdown3Label,
                CustomDropdown3Values = result.CustomDropdown3Values
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(EditModel model)
        {
            this.applicationFacade
                .Run<IUpdateCommand>(model, out var commandResult);

            switch (commandResult)
            {
                case IFailCommandResult fail:
                    TempData["Model"] = model;
                    TempData["Errors"] = fail.Errors;

                    return RedirectToAction(nameof(Update));
                case INotAuthorizedCommandResult _:
                    return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
                default:
                    return RedirectToAction(nameof(Index));
            }
        }

        [HttpGet]
        public ActionResult UpdateUserPermission(int id, bool projects = false)
        {
            var sql = @"Select us_username, us_id, isnull(pu_permission_level,$dpl) [pu_permission_level]
                from users
                left outer join project_user_xref on pu_user = us_id
                and pu_project = $pj
                order by us_username;
                select pj_name from projects where pj_id = $pj;"
                .Replace("$pj", id.ToString())
                .Replace("$dpl", this.applicationSettings.DefaultPermissionLevel.ToString());

            ViewBag.DataSet = DbUtil.GetDataSet(sql);

            ViewBag.Caption = "Permissions for " + (string) ViewBag.DataSet.Tables[1].Rows[0][0];

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit project per-user permissions",
                SelectedItem = MainMenuSections.Administration
            };

            var modle = new UpdateUserPermissionModel
            {
                Id = id,
                ToProjects = projects
            };

            return View(modle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateUserPermission(UpdateUserPermissionModel model)
        {
            // now update all the recs
            var sqlBatch = string.Empty;
            //RadioButton rb;
            //string permissionLevel;

            foreach (var permission in model.Permission)
            {
                var sq = @" if exists (select * from project_user_xref where pu_user = $us and pu_project = $pj)
                    update project_user_xref set pu_permission_level = $pu
                    where pu_user = $us and pu_project = $pj
                 else
                    insert into project_user_xref (pu_user, pu_project, pu_permission_level)
                    values ($us, $pj, $pu); ";

                sq = sq.Replace("$pj", model.Id.ToString());
                sq = sq.Replace("$us", Util.SanitizeInteger(permission.Key) /*Convert.ToString(dgi.Cells[1].Text)*/);

                //rb = (RadioButton)dgi.FindControl("none");
                //if (rb.Checked)
                //{
                //    permissionLevel = "0";
                //}
                //else
                //{
                //    rb = (RadioButton)dgi.FindControl("readonly");
                //    if (rb.Checked)
                //    {
                //        permissionLevel = "1";
                //    }
                //    else
                //    {
                //        rb = (RadioButton)dgi.FindControl("reporter");
                //        if (rb.Checked)
                //            permissionLevel = "3";
                //        else
                //            permissionLevel = "2";
                //    }
                //}

                sq = sq.Replace("$pu", permission.Value[0]);

                // add to the batch
                sqlBatch += sq;
            }

            DbUtil.ExecuteNonQuery(sqlBatch);

            ModelState.AddModelError("Ok", "Permissions have been updated.");

            var sql = @"Select us_username, us_id, isnull(pu_permission_level,$dpl) [pu_permission_level]
                from users
                left outer join project_user_xref on pu_user = us_id
                and pu_project = $pj
                order by us_username;
                select pj_name from projects where pj_id = $pj;"
                .Replace("$pj", model.Id.ToString())
                .Replace("$dpl", this.applicationSettings.DefaultPermissionLevel.ToString());

            ViewBag.DataSet = DbUtil.GetDataSet(sql);

            ViewBag.Caption = "Permissions for " + (string) ViewBag.DataSet.Tables[1].Rows[0][0];

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit project per-user permissions",
                SelectedItem = MainMenuSections.Administration
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete project",
                SelectedItem = MainMenuSections.Administration
            };

            if (TempData["Errors"] is IReadOnlyCollection<IFailError> failErrors)
                foreach (var failError in failErrors)
                    ModelState.AddModelError(failError.Property, failError.Message);

            if (TempData["Model"] is DeleteModel model) return View(model);

            var query = this.queryBuilder
                .From<IProjectSource>()
                .To<IProjectDeletePreviewResult>()
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
                .Run<IDeleteCommand>(model, out var commandResult);

            switch (commandResult)
            {
                case IFailCommandResult fail:
                    TempData["Model"] = model;
                    TempData["Errors"] = fail.Errors;

                    return RedirectToAction(nameof(Delete), new {id = model.Id});
                case INotAuthorizedCommandResult _:
                    return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
                default:
                    return RedirectToAction(nameof(Index));
            }
        }
    }
}