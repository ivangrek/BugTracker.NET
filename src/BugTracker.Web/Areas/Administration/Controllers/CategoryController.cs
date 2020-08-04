/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Web.Mvc;
    using System.Web.UI;
    using Changing.Results;
    using Core;
    using Core.Controls;
    using Core.Identification;
    using Models.Category;
    using Querying;
    using Tracking.Changing.Categories.Commands;
    using Tracking.Querying.Categories;
    using Web.Models;

    [Authorize(Roles = ApplicationRole.Administrator)]
    [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
    public class CategoryController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;
        private readonly IApplicationFacade applicationFacade;
        private readonly IQueryBuilder queryBuilder;

        public CategoryController(
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
                Title = $"{this.applicationSettings.AppTitle} - categories",
                SelectedItem = MainMenuSections.Administration
            };

            var query = this.queryBuilder
                .From<ICategorySource>()
                .To<ICategoryListResult>()
                //TODO: uncomment when manual sorting
                //.Sort()
                //.AscendingBy(x => x.SortSequence)
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
                Title = $"{this.applicationSettings.AppTitle} - new category",
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
                Title = $"{this.applicationSettings.AppTitle} - edit category",
                SelectedItem = MainMenuSections.Administration
            };

            if (TempData["Errors"] is IReadOnlyCollection<IFailError> failErrors)
                foreach (var failError in failErrors)
                    ModelState.AddModelError(failError.Property, failError.Message);

            if (TempData["Model"] is EditModel model) return View("Edit", model);

            var query = this.queryBuilder
                .From<ICategorySource>()
                .To<ICategoryStateResult>()
                .Filter()
                .Equal(x => x.Id, id)
                .Build();

            var result = this.applicationFacade
                .Run(query);

            model = new EditModel
            {
                Id = result.Id,
                Name = result.Name,
                SortSequence = result.SortSequence,
                Default = Convert.ToBoolean(result.Default)
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
                Title = $"{this.applicationSettings.AppTitle} - delete category",
                SelectedItem = MainMenuSections.Administration
            };

            if (TempData["Errors"] is IReadOnlyCollection<IFailError> failErrors)
                foreach (var failError in failErrors)
                    ModelState.AddModelError(failError.Property, failError.Message);

            if (TempData["Model"] is DeleteModel model) return View(model);

            var query = this.queryBuilder
                .From<ICategorySource>()
                .To<ICategoryDeletePreviewResult>()
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

                    return RedirectToAction(nameof(Delete), new { id = model.Id });
                case INotAuthorizedCommandResult _:
                    return new HttpStatusCodeResult(HttpStatusCode.Unauthorized);
                default:
                    return RedirectToAction(nameof(Index));
            }
        }
    }
}