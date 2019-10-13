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
    using System.Text;
    using System.Web.Mvc;
    using System.Web.UI;

    [OutputCache(Location = OutputCacheLocation.None)]
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
            // TODO
            //if (Request["format"] != "excel")
            //{
            //    Util.DoNotCache(System.Web.HttpContext.Current.Response);
            //};

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

        [HttpPost]
        public ActionResult Subscribe(int id, string ses, string actn)
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var permissionLevel = Bug.GetBugPermissionLevel(id, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone) Response.End();

            if (ses != (string)Session["session_cookie"])
            {
                return Content("session in URL doesn't match session cookie");
            }

            string sql;

            if (actn == "1")
            {
                sql = @"insert into bug_subscriptions (bs_bug, bs_user) values($bg, $us)";
            }
            else
            {
                sql = @"delete from bug_subscriptions where bs_bug = $bg and bs_user = $us";
            }

            sql = sql.Replace("$bg", id.ToString());
            sql = sql.Replace("$us", Convert.ToString(this.security.User.Usid));

            DbUtil.ExecuteNonQuery(sql);

            return Content("Ok");
        }

        [HttpGet]
        public ActionResult WritePosts(int id, bool imagesInline, bool historyInline)
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            var permissionLevel = Bug.GetBugPermissionLevel(id, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content("You are not allowed to view this item");
            }

            var dsPosts = PrintBug.GetBugPosts(id, this.security.User.ExternalUser, historyInline);
            var (_, html) = PrintBug.WritePosts(
                dsPosts,
                id,
                permissionLevel,
                true, // write links
                imagesInline,
                historyInline,
                true, // internal_posts
                this.security.User);

            return Content(html);
        }

        [HttpGet]
        public ActionResult Merge(int id)
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var isAutorized = this.security.User.IsAdmin
                || this.security.User.CanMergeBugs;

            if (!isAutorized)
            {
                return Content("You are not allowed to use this page.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - merge {this.applicationSettings.SingularBugLabel}",
                SelectedItem = ApplicationSettings.PluralBugLabelDefault
            };

            ViewBag.Confirm = false;

            var model = new MergeModel
            {
                Id = id,
                FromBugId = id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Merge(MergeModel model)
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var isAutorized = this.security.User.IsAdmin
                || this.security.User.CanMergeBugs;

            if (!isAutorized)
            {
                return Content("You are not allowed to use this page.");
            }

            if (model.FromBugId == model.IntoBugId)
            {
                ModelState.AddModelError("IntoBugId", "\"Into\" bug cannot be the same as \"From\" bug.");
            }

            // Continue and see if from and to exist in db

            var sql = @"
                declare @from_desc nvarchar(200)
                declare @into_desc nvarchar(200)
                declare @from_id int
                declare @into_id int
                set @from_id = -1
                set @into_id = -1
                select @from_desc = bg_short_desc, @from_id = bg_id from bugs where bg_id = $from
                select @into_desc = bg_short_desc, @into_id = bg_id from bugs where bg_id = $into
                select @from_desc, @into_desc, @from_id, @into_id"
                .Replace("$from", model.FromBugId.ToString())
                .Replace("$into", model.IntoBugId.ToString());

            var dataRow = DbUtil.GetDataRow(sql);

            if ((int)dataRow[2] == -1)
            {
                ModelState.AddModelError("FromBugId", "\"From\" bug not found.");
            }

            if ((int)dataRow[3] == -1)
            {
                ModelState.AddModelError("IntoBugId", "\"Into\" bug not found.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - merge {this.applicationSettings.SingularBugLabel}",
                    SelectedItem = ApplicationSettings.PluralBugLabelDefault
                };

                return View(model);
            }

            if (model.Confirm)
            {
                // rename the attachments
                var uploadFolder = Util.GetUploadFolder();

                if (uploadFolder != null)
                {
                    sql = @"select bp_id, bp_file from bug_posts
                        where bp_type = 'file' and bp_bug = $from"
                        .Replace("$from", model.FromBugId.ToString());

                    var ds = DbUtil.GetDataSet(sql);

                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        // create path
                        var path = new StringBuilder(uploadFolder);

                        path.Append("\\");
                        path.Append(model.FromBugId);
                        path.Append("_");
                        path.Append(Convert.ToString(dr["bp_id"]));
                        path.Append("_");
                        path.Append(Convert.ToString(dr["bp_file"]));

                        if (System.IO.File.Exists(path.ToString()))
                        {
                            var path2 = new StringBuilder(uploadFolder);

                            path2.Append("\\");
                            path2.Append(model.IntoBugId);
                            path2.Append("_");
                            path2.Append(Convert.ToString(dr["bp_id"]));
                            path2.Append("_");
                            path2.Append(Convert.ToString(dr["bp_file"]));

                            System.IO.File.Move(path.ToString(), path2.ToString());
                        }
                    }
                }

                // copy the from db entries to the to
                sql = @"
                    insert into bug_subscriptions
                    (bs_bug, bs_user)
                    select $into, bs_user
                    from bug_subscriptions
                    where bs_bug = $from
                    and bs_user not in (select bs_user from bug_subscriptions where bs_bug = $into)

                    insert into bug_user
                    (bu_bug, bu_user, bu_flag, bu_flag_datetime, bu_seen, bu_seen_datetime, bu_vote, bu_vote_datetime)
                    select $into, bu_user, bu_flag, bu_flag_datetime, bu_seen, bu_seen_datetime, bu_vote, bu_vote_datetime
                    from bug_user
                    where bu_bug = $from
                    and bu_user not in (select bu_user from bug_user where bu_bug = $into)

                    update bug_posts     set bp_bug     = $into	where bp_bug = $from
                    update bug_tasks     set tsk_bug    = $into where tsk_bug = $from
                    update svn_revisions set svnrev_bug = $into where svnrev_bug = $from
                    update hg_revisions  set hgrev_bug  = $into where hgrev_bug = $from
                    update git_commits   set gitcom_bug = $into where gitcom_bug = $from"
                    .Replace("$from", model.FromBugId.ToString())
                    .Replace("$into", model.IntoBugId.ToString());

                DbUtil.ExecuteNonQuery(sql);

                // record the merge itself

                sql = @"insert into bug_posts
                    (bp_bug, bp_user, bp_date, bp_type, bp_comment, bp_comment_search)
                    values($into,$us,getdate(), 'comment', 'merged bug $from into this bug:', 'merged bug $from into this bug:')
                    select scope_identity()"
                    .Replace("$from", model.FromBugId.ToString())
                    .Replace("$into", model.IntoBugId.ToString())
                    .Replace("$us", Convert.ToString(this.security.User.Usid));

                var commentId = Convert.ToInt32(DbUtil.ExecuteScalar(sql));

                // update bug comments with info from old bug
                sql = @"update bug_posts
                    set bp_comment = convert(nvarchar,bp_comment) + char(10) + bg_short_desc
                    from bugs where bg_id = $from
                    and bp_id = $bc"
                    .Replace("$from", model.FromBugId.ToString())
                    .Replace("$bc", Convert.ToString(commentId));

                DbUtil.ExecuteNonQuery(sql);

                // delete the from bug
                Bug.DeleteBug(model.FromBugId);

                // delete the from bug from the list, if there is a list
                var dvBugs = (DataView)Session["bugs"];

                if (dvBugs != null)
                {
                    // read through the list of bugs looking for the one that matches the from
                    var index = 0;
                    foreach (DataRowView drv in dvBugs)
                    {
                        if (model.FromBugId == (int)drv[1])
                        {
                            dvBugs.Delete(index);
                            break;
                        }

                        index++;
                    }
                }

                Bug.SendNotifications(Bug.Update, model.FromBugId, security);

                return Redirect($"~/Bugs/Edit.aspx?id={model.IntoBugId}");
            }

            ModelState.Clear();

            ModelState.AddModelError("StaticFromBug", model.FromBugId.ToString());
            ModelState.AddModelError("StaticIntoBug", model.IntoBugId.ToString());
            ModelState.AddModelError("StaticFromBugDescription", (string)dataRow[0]);
            ModelState.AddModelError("StaticIntoBugDescription", (string)dataRow[1]);

            model.Confirm = true;

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - merge {this.applicationSettings.SingularBugLabel}",
                SelectedItem = ApplicationSettings.PluralBugLabelDefault
            };

            return View(model);
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