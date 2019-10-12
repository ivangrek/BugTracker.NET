/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Controllers
{
    using BugTracker.Web.Core;
    using BugTracker.Web.Core.Controls;
    using BugTracker.Web.Models;
    using BugTracker.Web.Models.Query;
    using System.Web.Mvc;

    public class QueryController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;
        private readonly IQueryService queryService;

        public QueryController(
            IApplicationSettings applicationSettings,
            ISecurity security,
            IQueryService queryService)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
            this.queryService = queryService;
        }

        [HttpGet]
        public ActionResult Index(bool? showAll)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanUseReports
                || this.security.User.CanEditReports;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - reports",
                SelectedItem = MainMenuSections.Queries
            };

            var model = new SortableTableModel
            {
                DataSet = this.queryService.LoadList(showAll ?? false),
                HtmlEncode = false
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditSql;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var (valid, name) = this.queryService.CheckDeleting(id);

            if (!valid && !isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete query",
                SelectedItem = MainMenuSections.Queries
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

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditSql;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var (valid, name) = this.queryService.CheckDeleting(model.Id);

            if (!valid && !isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            // do delete here
            this.queryService.Delete(model.Id);

            return RedirectToAction(nameof(Index));
        }
    }
}