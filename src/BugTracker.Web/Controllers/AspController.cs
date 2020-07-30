/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Controllers
{
    using Core;
    using Models;
    using Models.Asp;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Globalization;
    using System.Text;
    using System.Threading;
    using System.Web.Mvc;
    using System.Web.UI;

    [Authorize]
    [OutputCache(Location = OutputCacheLocation.None)]
    public class AspController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;

        public AspController(
            IApplicationSettings applicationSettings,
            ISecurity security)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
        }

        [HttpGet]
        public void Ajax(int bugid)
        {
            // check permission
            var permissionLevel = Bug.GetBugPermissionLevel(/*Convert.ToInt32(*/bugid/*)*/, this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionNone)
            {
                Response.Write(@"
                    <style>
                        .cmt_text {
                            font-family: courier new;
                            font-size: 8pt;
                        }
                        
                        .pst {
                            font-size: 7pt;
                        }
                    </style>");

                using (var dsPosts = PrintBug.GetBugPosts(bugid, this.security.User.ExternalUser, false))
                {
                    var (postCnt, html) = PrintBug.WritePostsNew(
                        dsPosts,
                        bugid,
                        permissionLevel,
                        false, // write links
                        false, // images inline
                        false, // history inline
                        true, // internal posts
                        this.security.User);

                    // We can't unwrite what we wrote, but let's tell javascript to ignore it.
                    if (postCnt == 0)
                    {
                        Response.Write("<!--zeroposts-->");
                    }
                    else
                    {
                        System.Web.HttpContext.Current.Response.Write(html);
                    }
                }
            }
            else
            {
                Response.Write(string.Empty);
            }
        }

        [HttpGet]
        public void Ajax2(string q)
        {
            // will this be too slow?
            // we could index on bg_short_desc and then do '$str%' rather than '%$str%'

            try
            {
                var sql = @"
                    select distinct top 10
                        bg_short_desc
                    from bugs
                    where bg_short_desc like '%$str%'
                    order by 1";

                // if you don't use permissions, comment out this line for speed?
                sql = Util.AlterSqlPerProjectPermissions(sql, this.security);

                var text = q ?? string.Empty;
                sql = sql.Replace("$str", text.Replace("'", "''"));

                using (var ds = DbUtil.GetDataSet(sql))
                {
                    if (ds.Tables[0].Rows.Count > 0)
                    {
                        Response.Write("<select id='suggest_select' class='suggest_select'	size=6 ");
                        Response.Write(" onclick='select_suggestion(this)' onkeydown='return suggest_sel_onkeydown(this, event)'>");
                        foreach (DataRow dr in ds.Tables[0].Rows)
                        {
                            Response.Write("<option>");
                            Response.Write(dr[0]);
                            Response.Write("</option>");
                        }

                        Response.Write("</select>");
                    }
                    else
                    {
                        Response.Write(string.Empty);
                    }
                }
            }
            catch
            {
                Response.Write(string.Empty);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Hello()
        {
            var currentCultureSeparator = new CultureInfo(Thread.CurrentThread.CurrentCulture.Name)
                .NumberFormat.NumberDecimalSeparator;

            var deSeparator = new CultureInfo("de-DE")
                .NumberFormat.NumberDecimalSeparator;

            return Content($@"Hello<br>{Thread.CurrentThread.CurrentCulture.Name}<br>CurrentCulture NumberDecimalSeparator: {currentCultureSeparator}<br>DE NumberDecimalSeparator: {deSeparator}");
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Install(string dbname)
        {
            if (!string.IsNullOrEmpty(dbname))
            {
                dbname = dbname.Replace("'", "''");

                try
                {
                    // don't allow lots of dbs to be created by somebody malicious
                    if (Util.Dbs == null)
                    {
                        Util.Dbs = 0;
                    }

                    var dbs = (int)Util.Dbs;

                    if (dbs > 10)
                    {
                        return Content(string.Empty);
                    }

                    Util.Dbs = ++dbs;

                    using (DbUtil.GetSqlConnection())
                    { }

                    var sql = @"
                        use master
                        create database [$db]"
                        .Replace("$db", dbname);

                    DbUtil.ExecuteNonQuery(sql);

                    return Content("<font color=red><b>Database Created.</b></font>");
                }
                catch (Exception ex)
                {
                    return Content($"<font color='red'><b>{ex.Message}</b></font>");
                }
            }

            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Upgrade273To274()
        {
            var sql = "select us_username, us_id, us_password from users where len(us_password) < 32";

            var ds = DbUtil.GetDataSet(sql);
            var stringBuilder = new StringBuilder();

            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                Thread.Sleep(10); // give time for the random number to seed differently;

                var usUsername = (string)dr["us_username"];
                var usId = (int)dr["us_id"];
                var usPassword = (string)dr["us_password"];

                Util.UpdateUserPassword(usId, usPassword);

                stringBuilder.Append("encrypting " + usUsername + "<br>");
            }

            stringBuilder.Append("done encrypting unencrypted passwords");

            return Content(stringBuilder.ToString());
        }

        [HttpGet]
        public ActionResult ViewMemoryLog()
        {
            if (!this.applicationSettings.MemoryLogEnabled)
            {
                return Content(string.Empty);
            }

            Response.ContentType = "text/plain";
            Response.AddHeader("content-disposition", "inline; filename=\"memory_log.txt\"");

            var list = Util.MemoryLog;

            if (list == null)
            {
                return Content("list is null");
            }

            var stringBuilder = new StringBuilder();

            stringBuilder.Append(DateTime.Now.ToString("yyy-MM-dd HH:mm:ss:fff"));
            stringBuilder.Append("\n\n");

            for (var i = 0; i < list.Count; i++)
            {
                stringBuilder.Append(list[i]);
                stringBuilder.Append("\n");
            }

            return Content(stringBuilder.ToString());
        }

        [HttpGet]
        public ActionResult GenerateBtnetscReg()
        {
            var stringBuilder = new StringBuilder();

            Response.ContentType = "text/reg";
            Response.AddHeader("content-disposition", "attachment; filename=\"btnetsc.reg\"");

            stringBuilder.Append("Windows Registry Editor Version 5.00");
            stringBuilder.Append("\n\n");
            stringBuilder.Append("[HKEY_CURRENT_USER\\Software\\BugTracker.NET\\btnetsc\\SETTINGS]" + "\n");

            var url = "http://" + Request.ServerVariables["SERVER_NAME"] + Request.ServerVariables["URL"];

            url = url.Replace("generate_btnetsc_reg", "insert_bug");

            stringBuilder.Append("\"" + "Url" + "\"=\"" + url + "\"\n");
            stringBuilder.Append("\"" + "Project" + "\"=\"" + "0" + "\"\n");
            stringBuilder.Append("\"" + "Email" + "\"=\"" + this.security.User.Username + "\"\n");
            stringBuilder.Append("\"" + "Username" + "\"=\"" + this.security.User.Username + "\"\n");

            var nvcSrvElements = Request.ServerVariables;
            var array1 = nvcSrvElements.AllKeys;

            return Content(stringBuilder.ToString());
        }

        [HttpGet]
        public ActionResult Translate(int? bugId, int? postId)
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - translate",
                SelectedItem = this.applicationSettings.PluralBugLabel
            };

            var sql = string.Empty;
            var model = new TranslateModel
            {
                BugId = bugId.Value
            };

            if (postId.HasValue)
            {
                sql = @"select bp_bug, bp_comment
                        from bug_posts
                        where bp_id = $id";

                sql = sql.Replace("$id", postId.Value.ToString());

                var dr = DbUtil.GetDataRow(sql);

                bugId = (int)dr["bp_bug"];

                var obj = dr["bp_comment"];

                if (dr["bp_comment"] != DBNull.Value)
                {
                    model.Source = obj.ToString();
                }
            }
            else if (bugId.HasValue)
            {
                sql = @"select bg_short_desc
                        from bugs
                        where bg_id = $id";

                sql = sql.Replace("$id", bugId.Value.ToString());

                var obj = DbUtil.ExecuteScalar(sql);

                if (obj != DBNull.Value)
                {
                    model.Source = obj.ToString();
                }
            }

            // added check for permission level - corey
            var permissionLevel = Bug.GetBugPermissionLevel(bugId ?? 0, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content("You are not allowed to view this item");
            }

            using (var ts = new TranslationService())
            {
                ViewBag.TranslationModes = new List<SelectListItem>();

                foreach (var tm in ts.GetAllTranslationModes())
                {
                    ViewBag.TranslationModes.Add(new SelectListItem
                    {
                        Value = tm.ObjectID,
                        Text = tm.VisualNameEN
                    });
                }

                model.TranslationMode = "fr_nl";
            }

            ViewBag.Result = TempData["Result"];

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Translate(TranslateModel model)
        {
            var ts = new TranslationService();
            var tm = ts.GetTranslationModeByObjectID(model.TranslationMode);
            var result = ts.Translate(tm, model.Source);

            result = result.Replace("\n", "<br>");

            TempData["Result"] = result;

            tm = null;
            ts = null;

            return RedirectToAction(nameof(Translate));
        }
    }
}