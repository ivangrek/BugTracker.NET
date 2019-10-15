/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Controllers
{
    using BugTracker.Web.Areas.Administration.Models.Priority;
    using BugTracker.Web.Core;
    using BugTracker.Web.Core.Administration;
    using BugTracker.Web.Core.Controls;
    using BugTracker.Web.Models;
    using System.Collections.Generic;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;

    [Authorize(Roles = ApplicationRoles.Administrator)]
    [OutputCache(Location = OutputCacheLocation.None)]
    public class PriorityController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;
        private readonly IPriorityService priorityService;

        public PriorityController(
            IApplicationSettings applicationSettings,
            ISecurity security,
            IPriorityService priorityService)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
            this.priorityService = priorityService;
        }

        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - priorities",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new SortableTableModel
            {
                DataSet = this.priorityService.LoadList(),
                EditUrl = VirtualPathUtility.ToAbsolute("~/Administration/Priority/Update/"),
                DeleteUrl = VirtualPathUtility.ToAbsolute("~/Administration/Priority/Delete/"),
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
                Title = $"{this.applicationSettings.AppTitle} - new priority",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new EditModel
            {
                BackgroundColor = "#ffffff"
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EditModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Priority was not created.");

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - new priority",
                    SelectedItem = MainMenuSections.Administration
                };

                return View("Edit", model);
            }

            var parameters = new Dictionary<string, string>
            {
                { "$id", model.Id.ToString() },
                { "$na", model.Name.Replace("'", "''")},
                { "$ss", model.SortSequence.ToString() },
                { "$co", model.BackgroundColor.Replace("'", "''")},
                { "$st", (model.Style ?? string.Empty).Replace("'", "''")},
                { "$df", Util.BoolToString(model.Default)},
            };

            this.priorityService.Create(parameters);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Update(int id)
        {
            // Get this entry's data from the db and fill in the form
            var dataRow = this.priorityService.LoadOne(id);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit priority",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new EditModel
            {
                Id = id,
                Name = dataRow.Name,
                SortSequence = dataRow.SortSequence,
                Style = dataRow.Style,
                BackgroundColor = dataRow.BackgroundColor,
                Default = dataRow.Default == 1
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(EditModel model)
        {
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Priority was not created.");

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - edit priority",
                    SelectedItem = MainMenuSections.Administration
                };

                return View("Edit", model);
            }

            var parameters = new Dictionary<string, string>
            {
                { "$id", model.Id.ToString() },
                { "$na", model.Name.Replace("'", "''")},
                { "$ss", model.SortSequence.ToString() },
                { "$co", model.BackgroundColor.Replace("'", "''")},
                { "$st", (model.Style ?? string.Empty).Replace("'", "''")},
                { "$df", Util.BoolToString(model.Default)},
            };

            this.priorityService.Update(parameters);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            var (valid, name) = this.priorityService.CheckDeleting(id);

            if (!valid)
            {
                return Content($"You can't delete priority \"{name}\" because some bugs still reference it.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete priority",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new DeleteModel
            {
                Id = id,
                Name = name
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(DeleteModel model)
        {
            var (valid, name) = this.priorityService.CheckDeleting(model.Id);

            if (!valid)
            {
                return Content($"You can't delete priority \"{name}\" because some bugs still reference it.");
            }

            this.priorityService.Delete(model.Id);

            return RedirectToAction(nameof(Index));
        }
    }
}