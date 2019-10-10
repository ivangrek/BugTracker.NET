/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Controllers
{
    using BugTracker.Web.Core;
    using BugTracker.Web.Core.Controls;
    using BugTracker.Web.Models;
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Text;
    using System.Web.Mvc;

    public class HomeController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;
        private readonly IReportService reportService;

        public HomeController(
            IApplicationSettings applicationSettings,
            ISecurity security,
            IReportService reportService)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
            this.reportService = reportService;
        }

        [HttpGet]
        public ActionResult Index()
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            if (false) // change this to if(true) to make the donation nag message go away
            {
            }

            var bugs = Convert.ToInt32(DbUtil.ExecuteScalar("select count(1) from bugs"));

            if (bugs > 100)
            {
                ViewBag.Nag = true;
            }
            else
            {
                ViewBag.Nag = false;
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - administration",
                SelectedItem = MainMenuSections.Administration
            };

            return View();
        }

        [HttpGet]
        public ActionResult DownloadFile(string which, string filename)
        {
            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(which))
            {
                return Content(string.Empty);
            }

            string path;

            if (which == "backup")
            {
                path = Path.Combine(System.Web.HttpContext.Current.Server.MapPath("~/"), "App_Data", filename);
            }
            else if (which == "log")
            {
                path = Path.Combine(System.Web.HttpContext.Current.Server.MapPath("~/"), "App_Data", "logs", filename);
            }
            else
            {
                return Content(string.Empty);
            }

            Response.AddHeader("content-disposition", $"attachment; filename=\"{filename}\"");

            var contentType = Util.FilenameToContentType(filename);

            if (this.applicationSettings.UseTransmitFileInsteadOfWriteFile)
            {
                //Response.TransmitFile(path);
                return File(System.IO.File.ReadAllBytes(path), contentType);
            }
            else
            {
                //Response.WriteFile(path);
                return File(path, contentType);
            }
        }

        [HttpGet]
        public ActionResult GetDbDateTime()
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            var dt = (DateTime)DbUtil.ExecuteScalar("select getdate()");

            return Content(dt.ToString("yyyyMMdd HH\\:mm\\:ss\\:fff"));
        }

        [HttpGet]
        public ActionResult ServerVariables()
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            int loop1, loop2;
            NameValueCollection coll;

            // Load ServerVariable collection into NameValueCollection object.
            coll = Request.ServerVariables;
            // Get names of all keys into a string array.
            var arr1 = coll.AllKeys;
            var stringBuilder = new StringBuilder();

            for (loop1 = 0; loop1 < arr1.Length; loop1++)
            {
                stringBuilder.Append("Key: " + arr1[loop1] + "<br>");

                var arr2 = coll.GetValues(arr1[loop1]);

                for (loop2 = 0; loop2 < arr2.Length; loop2++)
                {
                    stringBuilder.Append("Value " + loop2 + ": " + arr2[loop2] + "<br>");
                }
            }

            return Content(stringBuilder.ToString());
        }
    }
}