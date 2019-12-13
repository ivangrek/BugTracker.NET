/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Controllers
{
    using System.Collections.Generic;
    using System.Data;
    using System.Net;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;
    using Changing.Results;
    using Core;
    using Core.Controls;
    using Models.UserDefinedAttribute;
    using Querying;
    using Tracking.Changing.UserDefinedAttributes.Commands;
    using Tracking.Querying.UserDefinedAttributes;
    using Web.Models;

    [Authorize(Roles = ApplicationRoles.Administrator)]
    [OutputCache(Location = OutputCacheLocation.None)]
    public class UserDefinedAttributeController : Controller
    {
        private readonly IApplicationFacade applicationFacade;
        private readonly IApplicationSettings applicationSettings;
        private readonly IQueryBuilder queryBuilder;
        private readonly ISecurity security;

        public UserDefinedAttributeController(
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
                Title = $"{this.applicationSettings.AppTitle} - user defined attributes",
                SelectedItem = MainMenuSections.Administration
            };

            var query = this.queryBuilder
                .From<IUserDefinedAttributeSource>()
                .To<IUserDefinedAttributeListResult>()
                .Sort()
                .AscendingBy(x => x.SortSequence)
                .AscendingBy(x => x.Name)
                .Build();

            var result = this.applicationFacade
                .Run(query);

            var dataTable = new DataTable();

            dataTable.Columns.Add("id");
            dataTable.Columns.Add("user defined attribute value");
            dataTable.Columns.Add("sort seq");
            dataTable.Columns.Add("default");
            dataTable.Columns.Add("hidden");

            foreach (var userDefinedAttribute in result)
            {
                var defaultValue = userDefinedAttribute.Default == 1
                    ? "Y"
                    : "N";

                dataTable.Rows.Add(userDefinedAttribute.Id, userDefinedAttribute.Name,
                    userDefinedAttribute.SortSequence, defaultValue, userDefinedAttribute.Id);
            }

            var model = new SortableTableModel
            {
                DataTable = dataTable,
                EditUrl = VirtualPathUtility.ToAbsolute("~/Administration/UserDefinedAttribute/Update/"),
                DeleteUrl = VirtualPathUtility.ToAbsolute("~/Administration/UserDefinedAttribute/Delete/")
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
                Title = $"{this.applicationSettings.AppTitle} - new user defined attribute",
                SelectedItem = MainMenuSections.Administration
            };

            if (TempData["Errors"] is IReadOnlyCollection<IFailError> failErrors)
                foreach (var failError in failErrors)
                    ModelState.AddModelError(failError.Property, failError.Message);

            if (TempData["Model"] is EditModel model) return View("Edit", model);

            model = new EditModel();

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
                Title = $"{this.applicationSettings.AppTitle} - edit user defined attribute",
                SelectedItem = MainMenuSections.Administration
            };

            if (TempData["Errors"] is IReadOnlyCollection<IFailError> failErrors)
                foreach (var failError in failErrors)
                    ModelState.AddModelError(failError.Property, failError.Message);

            if (TempData["Model"] is EditModel model) return View("Edit", model);

            var query = this.queryBuilder
                .From<IUserDefinedAttributeSource>()
                .To<IUserDefinedAttributeStateResult>()
                .Filter()
                .Equal(x => x.Id, id)
                .Build();

            var result = this.applicationFacade
                .Run(query);

            model = new EditModel
            {
                Id = id,
                Name = result.Name,
                SortSequence = result.SortSequence,
                Default = result.Default == 1
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
        public ActionResult Delete(int id)
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete user defined attribute",
                SelectedItem = MainMenuSections.Administration
            };

            if (TempData["Errors"] is IReadOnlyCollection<IFailError> failErrors)
                foreach (var failError in failErrors)
                    ModelState.AddModelError(failError.Property, failError.Message);

            if (TempData["Model"] is DeleteModel model) return View(model);

            var query = this.queryBuilder
                .From<IUserDefinedAttributeSource>()
                .To<IUserDefinedAttributeDeletePreviewResult>()
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