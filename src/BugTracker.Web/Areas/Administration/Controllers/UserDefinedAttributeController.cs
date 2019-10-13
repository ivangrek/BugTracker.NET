/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Controllers
{
    using BugTracker.Web.Areas.Administration.Models.UserDefinedAttribute;
    using BugTracker.Web.Core;
    using BugTracker.Web.Core.Administration;
    using BugTracker.Web.Core.Controls;
    using BugTracker.Web.Models;
    using System.Collections.Generic;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;

    [OutputCache(Location = OutputCacheLocation.None)]
    public class UserDefinedAttributeController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;
        private readonly IUserDefinedAttributeService userDefinedAttributeService;

        public UserDefinedAttributeController(
            IApplicationSettings applicationSettings,
            ISecurity security,
            IUserDefinedAttributeService userDefinedAttributeService)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
            this.userDefinedAttributeService = userDefinedAttributeService;
        }

        [HttpGet]
        public ActionResult Index()
        {
            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - user defined attribute values",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new SortableTableModel
            {
                DataSet = this.userDefinedAttributeService.LoadList(),
                EditUrl = VirtualPathUtility.ToAbsolute("~/Administration/UserDefinedAttribute/Update/"),
                DeleteUrl = VirtualPathUtility.ToAbsolute("~/Administration/UserDefinedAttribute/Delete/")
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Create()
        {
            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - create user defined attribute value",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new EditModel
            {
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EditModel model)
        {
            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Message", "User defined attribute value was not created.");

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - create user defined attribute value",
                    SelectedItem = MainMenuSections.Administration
                };

                return View("Edit", model);
            }

            var parameters = new Dictionary<string, string>
            {
                { "$id", model.Id.ToString() },
                { "$na", model.Name},
                { "$ss", model.SortSequence.ToString() },
                { "$df", Util.BoolToString(model.Default)},
            };

            this.userDefinedAttributeService.Create(parameters);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Update(int id)
        {
            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            // Get this entry's data from the db and fill in the form
            var dataRow = this.userDefinedAttributeService.LoadOne(id);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - update user defined attribute value",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new EditModel
            {
                Id = id,
                Name = dataRow.Name,
                SortSequence = dataRow.SortSequence,
                Default = dataRow.Default == 1
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(EditModel model)
        {
            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Message", "User defined attribute value was not updated.");

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - update user defined attribute value",
                    SelectedItem = MainMenuSections.Administration
                };

                return View("Edit", model);
            }

            var parameters = new Dictionary<string, string>
            {
                { "$id", model.Id.ToString() },
                { "$na", model.Name},
                { "$ss", model.SortSequence.ToString() },
                { "$df", Util.BoolToString(model.Default)},
            };

            this.userDefinedAttributeService.Update(parameters);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            var (valid, name) = this.userDefinedAttributeService.CheckDeleting(id);

            if (!valid)
            {
                return Content($"You can't delete value \"{name}\" because some bugs still reference it.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete user defined attribute value",
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
            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            this.userDefinedAttributeService.Delete(model.Id);

            return RedirectToAction(nameof(Index));
        }
    }
}