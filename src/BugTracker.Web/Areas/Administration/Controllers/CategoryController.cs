/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Controllers
{
    using BugTracker.Web.Areas.Administration.Models.Category;
    using BugTracker.Web.Core;
    using BugTracker.Web.Core.Administration;
    using BugTracker.Web.Core.Controls;
    using BugTracker.Web.Models;
    using System.Web;
    using System.Web.Mvc;

    public class CategoryController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;
        private readonly ICategoryService categoryService;

        public CategoryController(
            IApplicationSettings applicationSettings,
            ISecurity security,
            ICategoryService categoryService)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
            this.categoryService = categoryService;
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
                Title = $"{this.applicationSettings.AppTitle} - categories",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new SortableTableModel
            {
                DataSet = this.categoryService.LoadList(),
                EditUrl = VirtualPathUtility.ToAbsolute("~/Admin/Categories/Edit.aspx?id="),
                DeleteUrl = VirtualPathUtility.ToAbsolute("~/Administration/Category/Delete?id=")
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Create()
        {
            return Content("Create");
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            var (valid, name) = this.categoryService.CheckDeleting(id);

            if (!valid)
            {
                return Content($"You can't delete category \"{name}\" because some bugs still reference it.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete category",
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
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            var (valid, name) = this.categoryService.CheckDeleting(model.Id);

            if (!valid)
            {
                return Content($"You can't delete category \"{name}\" because some bugs still reference it.");
            }

            this.categoryService.Delete(model.Id);

            return RedirectToAction("Index");
        }
    }
}