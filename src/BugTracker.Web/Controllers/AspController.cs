/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Controllers
{
    using BugTracker.Web.Core;
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
                    var (postCnt, html) = PrintBug.WritePosts(
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
                    if (HttpContext.ApplicationInstance.Application["dbs"] == null)
                    {
                        HttpContext.ApplicationInstance.Application["dbs"] = 0;
                    }

                    var dbs = (int)HttpContext.ApplicationInstance.Application["dbs"];

                    if (dbs > 10)
                    {
                        return Content(string.Empty);
                    }

                    HttpContext.ApplicationInstance.Application["dbs"] = ++dbs;

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

            var list = (List<string>)System.Web.HttpContext.Current.Application["log"];

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
    }
}