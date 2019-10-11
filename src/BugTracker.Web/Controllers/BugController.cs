/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Controllers
{
    using BugTracker.Web.Core;
    using BugTracker.Web.Models;
    using BugTracker.Web.Models.Bug;
    using System;
    using System.Web.Mvc;

    public class BugController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;

        public BugController(
            IApplicationSettings applicationSettings,
            ISecurity security)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanDeleteBug;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var permissionLevel = Bug.GetBugPermissionLevel(Convert.ToInt32(id), this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                return Content("You are not allowed to edit this item");
            }

            var sql = @"select bg_short_desc from bugs where bg_id = $1"
                .Replace("$1", id.ToString());

            var dr = DbUtil.GetDataRow(sql);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete {this.applicationSettings.SingularBugLabel}",
                SelectedItem = ApplicationSettings.PluralBugLabelDefault
            };

            var model = new DeleteModel
            {
                Id = id,
                Name = (string)dr["bg_short_desc"]
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(DeleteModel model)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanDeleteBug;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var permissionLevel = Bug.GetBugPermissionLevel(model.Id, this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                return Content("You are not allowed to edit this item");
            }

            Bug.DeleteBug(model.Id);

            return Redirect("~/Bugs/List.aspx");
        }
    }
}