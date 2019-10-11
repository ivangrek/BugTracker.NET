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
    using System.Data;
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

        [HttpGet]
        public void Print(string format, int? queryId)
        {
            if (Request["format"] != "excel")
            {
                Util.DoNotCache(System.Web.HttpContext.Current.Response);
            };

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            DataView dataView;

            if (queryId.HasValue)
            {
                // use sql specified in query string
                var sql = @"select qu_sql from queries where qu_id = $1"
                    .Replace("$1", queryId.Value.ToString());

                var bugSql = (string)DbUtil.ExecuteScalar(sql);

                // replace magic variables
                bugSql = bugSql.Replace("$ME", Convert.ToString(this.security.User.Usid));

                bugSql = Util.AlterSqlPerProjectPermissions(bugSql, this.security);

                DataSet dataSet = DbUtil.GetDataSet(bugSql);
                dataView = new DataView(dataSet.Tables[0]);
            }
            else
            {
                dataView = (DataView)Session["bugs"];
            }

            if (dataView == null)
            {
                Response.Write("Please recreate the list before trying to print...");
            }

            if (format != null && format == "excel")
            {
                Util.PrintAsExcel(System.Web.HttpContext.Current.Response, dataView);
            }
            else
            {
                PrintAsHtml(dataView);
            }
        }

        [HttpGet]
        public ActionResult PrintDetail(int? id, int? queryId)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            if (id.HasValue)
            {
                ViewBag.DataRow = Bug.GetBugDataRow(id.Value, this.security);

                if (ViewBag.DataRow == null)
                {
                    return Content($"{Util.CapitalizeFirstLetter(this.applicationSettings.SingularBugLabel)}not found:&nbsp;{id}");
                }

                if (ViewBag.DataRow["pu_permission_level"] == 0)
                {
                    return Content($"You are not allowed to view this {this.applicationSettings.SingularBugLabel} not found:&nbsp;{id}");
                }

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - {Util.CapitalizeFirstLetter(this.applicationSettings.SingularBugLabel)} ID {id} {(string)ViewBag.DataRow["short_desc"]}"
                };
            }
            else
            {
                if (queryId.HasValue)
                {
                    var sql = @"select qu_sql from queries where qu_id = $1"
                        .Replace("$1", queryId.Value.ToString());

                    var bugSql = (string)DbUtil.ExecuteScalar(sql);

                    // replace magic variables
                    bugSql = bugSql.Replace("$ME", Convert.ToString(this.security.User.Usid));
                    bugSql = Util.AlterSqlPerProjectPermissions(bugSql, this.security);

                    // all we really need is the bugid, but let's do the same query as Bug/Print
                    ViewBag.DataSet = DbUtil.GetDataSet(bugSql);
                }
                else
                {
                    ViewBag.DataView = (DataView)Session["bugs"];
                }

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - print {this.applicationSettings.SingularBugLabel}"
                };
            }

            var cookie = Request.Cookies["images_inline"];

            if (cookie == null || cookie.Value == "0")
            {
                ViewBag.ImagesInline = false;
            }
            else
            {
                ViewBag.ImagesInline = true;
            }

            cookie = Request.Cookies["history_inline"];

            if (cookie == null || cookie.Value == "0")
            {
                ViewBag.HistoryInline = false;
            }
            else
            {
                ViewBag.HistoryInline = true;
            }

            return View();
        }

        private void PrintAsHtml(DataView dataView)
        {
            Response.Write("<html><head><link rel='StyleSheet' href='Content/btnet.css' type='text/css'></head><body>");
            Response.Write("<table class=bugt border=1>");
            int col;

            for (col = 1; col < dataView.Table.Columns.Count; col++)
            {
                Response.Write("<td class=bugh>\n");

                if (dataView.Table.Columns[col].ColumnName == "$FLAG")
                {
                    Response.Write("flag");
                }
                else if (dataView.Table.Columns[col].ColumnName == "$SEEN")
                {
                    Response.Write("new");
                }
                else
                {
                    Response.Write(dataView.Table.Columns[col].ColumnName);
                }

                Response.Write("</td>");
            }

            foreach (DataRowView drv in dataView)
            {
                Response.Write("<tr>");
                for (col = 1; col < dataView.Table.Columns.Count; col++)
                {
                    if (dataView.Table.Columns[col].ColumnName == "$FLAG")
                    {
                        var flag = (int)drv[col];
                        var cls = "wht";

                        if (flag == 1)
                        {
                            cls = "red";
                        }
                        else if (flag == 2)
                        {
                            cls = "grn";
                        }

                        Response.Write("<td class=datad><span class=" + cls + ">&nbsp;</span>");
                    }
                    else if (dataView.Table.Columns[col].ColumnName == "$SEEN")
                    {
                        var seen = (int)drv[col];
                        var cls = "old";

                        if (seen == 0)
                        {
                            cls = "new";
                        }
                        else
                        {
                            cls = "old";
                        }

                        Response.Write("<td class=datad><span class=" + cls + ">&nbsp;</span>");
                    }
                    else
                    {
                        var datatype = dataView.Table.Columns[col].DataType;

                        if (Util.IsNumericDataType(datatype))
                        {
                            Response.Write("<td class=bugd align=right>");
                        }
                        else
                        {
                            Response.Write("<td class=bugd>");
                        }

                        // write the data
                        if (string.IsNullOrEmpty(drv[col].ToString()))
                        {
                            Response.Write("&nbsp;");
                        }
                        else
                        {
                            Response.Write(Server.HtmlEncode(drv[col].ToString()).Replace("\n", "<br>"));
                        }
                    }

                    Response.Write("</td>");
                }

                Response.Write("</tr>");
            }

            Response.Write("</table></body></html>");
        }
    }
}