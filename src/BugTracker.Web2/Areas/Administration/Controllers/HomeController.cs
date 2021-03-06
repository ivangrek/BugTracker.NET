﻿/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Controllers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Data;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;
    using System.Xml;
    using Core;
    using Core.Identification;
    using Models.Home;
    using Web.Models;

    [Authorize(Roles = ApplicationRole.Administrator)]
    [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
    public class HomeController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly IReportService reportService;
        private readonly ISecurity security;

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
            if (false) // change this to if(true) to make the donation nag message go away
            {
            }

            var bugs = Convert.ToInt32(DbUtil.ExecuteScalar(new SqlString("select count(1) from bugs")));

            if (bugs > 100)
                ViewBag.Nag = true;
            else
                ViewBag.Nag = false;

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - administration",
                SelectedItem = MainMenuSection.Administration
            };

            return View();
        }

        [HttpGet]
        public ActionResult DownloadFile(string which, string filename)
        {
            if (string.IsNullOrEmpty(filename) || string.IsNullOrEmpty(which)) return Content(string.Empty);

            string path;

            if (which == "backup")
                path = Path.Combine(Util.ServerRootForlder, "App_Data", filename);
            else if (which == "log")
                path = Path.Combine(Util.ServerRootForlder, "App_Data", "logs", filename);
            else
                return Content(string.Empty);

            Response.AddHeader("content-disposition", $"attachment; filename=\"{filename}\"");

            var contentType = Util.FilenameToContentType(filename);

            if (this.applicationSettings.UseTransmitFileInsteadOfWriteFile)
                //Response.TransmitFile(path);
                return File(System.IO.File.ReadAllBytes(path), contentType);
            return File(path, contentType);
        }

        // TODO to bugs
        [HttpGet]
        public ActionResult GetDbDateTime()
        {
            var dt = (DateTime)DbUtil.ExecuteScalar(new SqlString("select getdate()"));

            return Content(dt.ToString("yyyyMMdd HH\\:mm\\:ss\\:fff"));
        }

        [HttpGet]
        public ActionResult ServerVariables()
        {
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
                    stringBuilder.Append("Value " + loop2 + ": " + arr2[loop2] + "<br>");
            }

            return Content(stringBuilder.ToString());
        }

        [HttpGet]
        public ActionResult EditCustomHtml(string which)
        {
            // default to footer
            if (string.IsNullOrEmpty(which)) which = "footer";

            var fileName = GetFileName(which);
            var path = Path.Combine(Util.ServerRootForlder, "Content", "custom", fileName);
            var text = string.Empty;

            using (var sr = System.IO.File.OpenText(path))
            {
                text = sr.ReadToEnd();
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit custom html",
                SelectedItem = MainMenuSection.Administration
            };

            var model = new EditCustomHtmlModel
            {
                Which = which,
                Text = text
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditCustomHtml(EditCustomHtmlModel model)
        {
            // default to footer
            if (string.IsNullOrEmpty(model.Which)) model.Which = "footer";

            if (string.IsNullOrEmpty(model.Which)) return Content(string.Empty);

            var fileName = GetFileName(model.Which);

            if (string.IsNullOrEmpty(fileName)) return Content(string.Empty);

            // save to disk
            var path = Path.Combine(Util.ServerRootForlder, "Content", "custom", fileName);

            using (var sw = System.IO.File.CreateText(path))
            {
                sw.Write(model.Text);
            }

            // save in memory
            var cacheKey = Path.GetFileNameWithoutExtension(fileName);

            switch (cacheKey)
            {
                case Util.CustomHeaderHtmlCacheKey:
                    Util.CustomHeaderHtml = model.Text;
                    break;
                case Util.CustomFooterHtmlCacheKey:
                    Util.CustomFooterHtml = model.Text;
                    break;
                case Util.CustomLogoHtmlCacheKey:
                    Util.CustomLogoHtml = model.Text;
                    break;
                case Util.CustomWelcomeHtmlCacheKey:
                    Util.CustomWelcomeHtml = model.Text;
                    break;
            }

            ModelState.AddModelError(string.Empty, fileName + " was saved.");

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit custom html",
                SelectedItem = MainMenuSection.Administration
            };

            return View(model);
        }

        // not in menu
        [HttpGet]
        public ActionResult ViewWebConfig()
        {
            // create path
            var path = Path.Combine(Util.ServerRootForlder, "Web.config");

            return File(path, "application/xml");
        }

        [HttpGet]
        public ActionResult Query()
        {
            // If there is a users table, then authenticate this page
            try
            {
                DbUtil.ExecuteNonQuery("select count(1) from users");
            }
            catch (Exception)
            {
            }

            var ds = DbUtil.GetDataSet("select name from sysobjects where type = 'u' order by 1");

            ViewBag.DbTables = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = "Select Table",
                    Value = string.Empty
                }
            };

            foreach (DataRow dr in ds.Tables[0].Rows)
                ViewBag.DbTables.Add(new SelectListItem
                {
                    Text = (string)dr[0],
                    Value = (string)dr[0]
                });

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - run query",
                SelectedItem = MainMenuSection.Administration
            };

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Query(QueryModel model)
        {
            // If there is a users table, then authenticate this page
            try
            {
                DbUtil.ExecuteNonQuery("select count(1) from users");
            }
            catch (Exception)
            {
            }

            if (!string.IsNullOrEmpty(model.Text))
                try
                {
                    ViewBag.Table = new SortableTableModel
                    {
                        DataTable = DbUtil.GetDataSet(Server.HtmlDecode(model.Text)).Tables[0]
                    };
                }
                catch (Exception e2)
                {
                    ViewBag.ExceptionMessage = e2.Message;
                    //exception_message = e2.ToString();  // uncomment this if you need more error info.
                }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - run query",
                SelectedItem = MainMenuSection.Administration
            };

            var ds = DbUtil.GetDataSet("select name from sysobjects where type = 'u' order by 1");

            ViewBag.DbTables = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Text = "Select Table",
                    Value = string.Empty
                }
            };

            foreach (DataRow dr in ds.Tables[0].Rows)
                ViewBag.DbTables.Add(new SelectListItem
                {
                    Text = (string)dr[0],
                    Value = (string)dr[0]
                });

            return View(model);
        }

        [HttpGet]
        public ActionResult EditWebConfig()
        {
            if (!this.applicationSettings.EnableEditWebConfigPage)
            {
                return HttpNotFound();
            }

            var path = Path.Combine(Util.ServerRootForlder, "Web.config");
            var model = new EditWebConfigModel();

            using (var sr = System.IO.File.OpenText(path))
            {
                model.Text = sr.ReadToEnd();
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit Web.config",
                SelectedItem = MainMenuSection.Administration
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult EditWebConfig(EditWebConfigModel model)
        {
            var path = Path.Combine(Util.ServerRootForlder, "Web.config");

            try
            {
                var doc = new XmlDocument();

                using (var stringReader = new StringReader(model.Text))
                {
                    doc.Load(stringReader);

                    using (var sw = System.IO.File.CreateText(path))
                    {
                        sw.Write(model.Text);
                    }

                    ModelState.AddModelError(string.Empty, "Web.config was saved.");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"ERROR:{ex.Message}");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit Web.config",
                SelectedItem = MainMenuSection.Administration
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult BackupDb()
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - backup db",
                SelectedItem = MainMenuSection.Administration
            };

            ViewBag.Table = new SortableTableModel
            {
                DataTable = GetBackupFiles().Tables[0],
                HtmlEncode = false
            };

            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BackupDb(BackupDbModel model)
        {
            if (string.IsNullOrEmpty(model.FileName))
            {
                var date = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var db = (string)DbUtil.ExecuteScalar(new SqlString("select db_name()"));
                var backupFile = Path.Combine(Util.ServerRootForlder, "App_Data",
                    $"db_backup_{date}.bak");
                var sql = "backup database " + db + " to disk = '" + backupFile + "'";

                DbUtil.ExecuteNonQuery(sql);
            }
            else
            {
                var backupFile = Path.Combine(Util.ServerRootForlder, "App_Data",
                    model.FileName);

                System.IO.File.Delete(backupFile);
            }

            return Redirect(nameof(BackupDb));
        }

        [HttpGet]
        public ActionResult ManageLogs()
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - manage logs",
                SelectedItem = MainMenuSection.Administration
            };

            ViewBag.Table = new SortableTableModel
            {
                DataTable = GetLogFiles().Tables[0],
                HtmlEncode = false
            };

            var model = new ManageLogsModel();

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ManageLogs(ManageLogsModel model)
        {
            if (!string.IsNullOrEmpty(model.FileName))
            {
                var logFile = Path.Combine(Util.ServerRootForlder, "App_Data", "logs",
                    model.FileName);

                System.IO.File.Delete(logFile);
            }

            return Redirect(nameof(ManageLogs));
        }

        [HttpGet]
        public ActionResult Notification()
        {
            var dataSet = DbUtil.GetDataSet(
                @"select
                qn_id [id],
                qn_date_created [date created],
                qn_to [to],
                qn_bug [bug],
                qn_status [status],
                qn_retries [retries],
                qn_last_exception [last error]
                from queued_notifications
                order by id;");

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - queued notifications",
                SelectedItem = MainMenuSection.Administration
            };

            var model = new SortableTableModel
            {
                DataTable = dataSet.Tables[0]
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Notification(NotificationModel model)
        {
            if (model.Action == "delete")
            {
                var sql = @"delete from queued_notifications where qn_status = N'not sent'";

                DbUtil.ExecuteNonQuery(sql);
            }
            else if (model.Action == "reset")
            {
                var sql = @"update queued_notifications set qn_retries = 0 where qn_status = N'not sent'";

                DbUtil.ExecuteNonQuery(sql);
            }
            else if (model.Action == "resend")
            {
                // spawn a worker thread to send the emails
                var thread = new Thread(Bug.ThreadProcNotifications);

                thread.Start();
            }

            return RedirectToAction(nameof(Notification));
        }

        [HttpGet]
        public ActionResult EditStyles()
        {
            var dataSet = DbUtil.GetDataSet(
                @"select
                '<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Administration/Priority/Update/") +
                @"' + convert(varchar,pr_id) + '>' + pr_name + '</a>' [priority],
                '<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Administration/Status/Update/") +
                @"' + convert(varchar,st_id) + '>' + st_name + '</a>' [status],
                isnull(pr_style,'') [priority CSS class],
                isnull(st_style,'') [status CSS class],
                isnull(pr_style + st_style,'datad') [combo CSS class - priority + status ],
                '<span class=''' + isnull(pr_style,'') + isnull(st_style,'')  +'''>The quick brown fox</span>' [text sample]
                from priorities, statuses /* intentioanl cartesian join */
                order by pr_sort_seq, st_sort_seq;

                select distinct isnull(pr_style + st_style,'datad')
                from priorities, statuses;");

            var classesList = new ArrayList();

            foreach (DataRow drStyles in dataSet.Tables[1].Rows) classesList.Add("." + (string)drStyles[0]);

            // create path
            var path = Path.Combine(Util.ServerRootForlder, "Content", "custom", "btnet_custom.css");
            var relevantCssLines = new StringBuilder();
            var lines = new ArrayList();

            if (System.IO.File.Exists(path))
            {
                string line;
                var stream = System.IO.File.OpenText(path);

                while ((line = stream.ReadLine()) != null)
                    for (var i = 0; i < classesList.Count; i++)
                        if (line.IndexOf((string)classesList[i]) > -1)
                        {
                            relevantCssLines.Append(line);
                            relevantCssLines.Append("<br>");
                            lines.Add(line);
                            break;
                        }

                stream.Close();
            }

            ViewBag.RelevantLines = relevantCssLines.ToString();

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - styles",
                SelectedItem = MainMenuSection.Administration
            };

            var model = new SortableTableModel
            {
                DataTable = dataSet.Tables[0],
                HtmlEncode = false
            };

            return View(model);
        }

        private static string GetFileName(string whichFile)
        {
            var fileName = string.Empty;

            if (whichFile == "css")
                fileName = "btnet_custom.css";
            else if (whichFile == "footer")
                fileName = "custom_footer.html";
            else if (whichFile == "header")
                fileName = "custom_header.html";
            else if (whichFile == "logo")
                fileName = "custom_logo.html";
            else if (whichFile == "welcome") fileName = "custom_welcome.html";

            return fileName;
        }

        private static DataSet GetBackupFiles()
        {
            var path = Path.Combine(Util.ServerRootForlder, "App_Data");
            var backupFiles = Directory.GetFiles(path, "*.bak");

            // sort the files
            var list = new ArrayList();

            list.AddRange(backupFiles);
            list.Sort();

            var dataTable = new DataTable();

            dataTable.Columns.Add(new DataColumn("File", typeof(string)));
            dataTable.Columns.Add(new DataColumn("$no_sort_Download", typeof(string)));
            dataTable.Columns.Add(new DataColumn("$no_sort_Delete", typeof(string)));

            for (var i = 0; i < list.Count; i++)
            {
                var dataRow = dataTable.NewRow();
                var justFile = Path.GetFileName((string)list[i]);

                dataRow[0] = justFile;
                dataRow[1] = "<a href='" +
                             VirtualPathUtility.ToAbsolute(
                                 $"~/Administration/Home/DownloadFile?which=backup&filename={justFile}") +
                             "'>Download</a>";
                dataRow[2] = $"<a href='#' onclick='onDelete(\"{justFile}\")'>Delete</a>";

                dataTable.Rows.Add(dataRow);
            }

            var dataSet = new DataSet();

            dataSet.Tables.Add(dataTable);

            return dataSet;
        }

        private static DataSet GetLogFiles()
        {
            var path = Path.Combine(Util.ServerRootForlder, "App_Data", "logs");
            var logFiles = Directory.GetFiles(path, "*.txt");

            // sort the files
            var list = new ArrayList();

            list.AddRange(logFiles);
            list.Sort();

            var dataTable = new DataTable();

            dataTable.Columns.Add(new DataColumn("File", typeof(string)));
            dataTable.Columns.Add(new DataColumn("$no_sort_Download", typeof(string)));
            dataTable.Columns.Add(new DataColumn("$no_sort_Delete", typeof(string)));

            for (var i = list.Count - 1; i != -1; i--)
            {
                var dataRow = dataTable.NewRow();
                var justFile = Path.GetFileName((string)list[i]);

                dataRow[0] = justFile;
                dataRow[1] = "<a href='" +
                             VirtualPathUtility.ToAbsolute(
                                 $"~/Administration/Home/DownloadFile?which=log&filename={justFile}") +
                             "'>Download</a>";
                dataRow[2] = $"<a href='#' onclick='onDelete(\"{justFile}\")'>Delete</a>";

                dataTable.Rows.Add(dataRow);
            }

            var dataSet = new DataSet();

            dataSet.Tables.Add(dataTable);

            return dataSet;
        }
    }
}