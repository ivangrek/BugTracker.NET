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
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;

    [OutputCache(Location = OutputCacheLocation.None)]
    public class NewsController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;

        public NewsController(
            IApplicationSettings applicationSettings,
            ISecurity security)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
        }

        [HttpGet]
        public ActionResult Index()
        {
            if (!this.applicationSettings.EnableWhatsNewPage)
            {
                return Content(string.Empty);
            }

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - news?",
                SelectedItem = MainMenuSections.News
            };

            return View();
        }

        [HttpGet]
        public ActionResult WhatsNew(long since)
        {
            if (!this.applicationSettings.EnableWhatsNewPage)
            {
                return Content("Sorry, Web.config EnableWhatsNewPage is set to 0");
            }

            var list = (List<BugNews>)HttpContext.ApplicationInstance.Application["whatsnew"] ?? new List<BugNews>();
            var result = new
            {
                // The web server's time.  The client javascript will use this a a reference point.
                now = DateTime.Now.Ticks / Core.WhatsNew.TenMillion,
                news_list = list.Where(x => x.Seconds > since)
                    .Select(x => new
                    {
                        seconds = x.SecondsString,
                        bugid = x.Bugid,
                        desc = HttpUtility.HtmlEncode(x.Desc),
                        action = x.Action,
                        who = x.Who
                    })
            };

            return Json(result, JsonRequestBehavior.AllowGet);
        }
    }
}