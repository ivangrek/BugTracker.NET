/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Controllers
{
    using anmar.SharpMimeTools;
    using BugTracker.Web.Core;
    using BugTracker.Web.Models;
    using BugTracker.Web.Models.Bug;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Text;
    using System.Web.Mvc;
    using System.Web.UI;

    [Authorize]
    [OutputCache(Location = OutputCacheLocation.None)]
    public class BugController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;
        private readonly IAuthenticate authenticate;

        public BugController(
            IApplicationSettings applicationSettings,
            ISecurity security,
            IAuthenticate authenticate)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
            this.authenticate = authenticate;
        }

        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - {this.applicationSettings.PluralBugLabel}",
                SelectedItem = this.applicationSettings.PluralBugLabel
            };

            LoadQueryDropdown();

            var model = (IndexModel)TempData["IndexModel"] ?? new IndexModel
            {
                QueryId = 0,
                Action = string.Empty,
                NewPage = 0,
                Filter = string.Empty,
                Sort = -1,
                PrevSort = -1,
                PrevDir = "ASC"
            };

            ViewBag.PostBack = false;

            if (!string.IsNullOrEmpty(model.Action) && model.Action != "query")
            {
                // sorting, paging, filtering, so don't go back to the database
                ViewBag.DataView = (DataView)Session["bugs"];

                if (ViewBag.DataView == null)
                {
                    DoQuery(model);
                }
                else if (model.Action == "sort")
                {
                    model.NewPage = 0;
                }

                ViewBag.PostBack = true;
            }
            else
            {
                if (Session["just_did_text_search"] == null)
                {
                    var message = DoQuery(model);

                    if (!string.IsNullOrEmpty(message))
                    {
                        return Content(message);
                    }
                }
                // from search page
                else
                {
                    Session["just_did_text_search"] = null;

                    ViewBag.DataView = (DataView)Session["bugs"];
                }
            }

            CallSortAndFilterBuglistDataview(model, ViewBag.PostBack);

            model.Action = string.Empty;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(IndexModel model)
        {
            // posting back a query change?
            // posting back a filter change?
            // posting back a sort change?

            if (model.Action == "query")
            {
                TempData["IndexModel"] = new IndexModel
                {
                    QueryId = model.QueryId,
                    Action = model.Action,
                    NewPage = 0,
                    Filter = string.Empty,
                    Sort = -1,
                    PrevSort = -1,
                    PrevDir = "ASC"
                };
            }
            else
            {
                TempData["IndexModel"] = model;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Insert()
        {
            var username = Request["username"];
            var password = Request["password"];
            var projectidString = Request["projectid"];
            var comment = Request["comment"];
            var fromAddr = Request["from"];
            var cc = string.Empty;
            var message = Request["message"];
            var attachmentAsBase64 = Request["attachment"];
            var attachmentContentType = Request["attachment_content_type"];
            var attachmentFilename = Request["attachment_filename"];
            var attachmentDesc = Request["attachment_desc"];
            var bugidString = Request["bugid"];
            var shortDesc = Request["short_desc"];

            // this could also be the email subject
            if (shortDesc == null)
            {
                shortDesc = string.Empty;
            }
            else if (shortDesc.Length > 200)
            {
                shortDesc = shortDesc.Substring(0, 200);
            }

            SharpMimeMessage mimeMessage = null;

            if (message != null && message.Length > 0)
            {
                mimeMessage = MyMime.GetSharpMimeMessage(message);

                comment = MyMime.GetComment(mimeMessage);

                var headers = MyMime.GetHeadersForComment(mimeMessage);

                if (!string.IsNullOrEmpty(headers))
                {
                    comment = headers + "\n" + comment;
                }

                fromAddr = MyMime.GetFromAddr(mimeMessage);
            }
            else
            {
                if (comment == null) comment = string.Empty;
            }

            if (string.IsNullOrEmpty(username))
            {
                Response.AddHeader("BTNET", "ERROR: username required");

                return Content("ERROR: username required");
            }

            if (string.IsNullOrEmpty(password))
            {
                Response.AddHeader("BTNET", "ERROR: password required");

                return Content("ERROR: password required");
            }

            // authenticate user

            var authenticated = this.authenticate.CheckPassword(username, password);

            if (!authenticated)
            {
                Response.AddHeader("BTNET", "ERROR: invalid username or password");

                return Content("ERROR: invalid username or password");
            }

            var security = MyMime.GetSynthesizedSecurity(mimeMessage, fromAddr, username);

            var projectid = 0;
            if (Util.IsInt(projectidString)) projectid = Convert.ToInt32(projectidString);

            var bugid = 0;

            if (Util.IsInt(bugidString))
            {
                bugid = Convert.ToInt32(bugidString);
            }

            // Even though btnet_service.exe has already parsed out the bugid,
            // we can do a better job here with SharpMimeTools.dll
            var subject = string.Empty;

            if (mimeMessage != null)
            {
                subject = MyMime.GetSubject(mimeMessage);

                if (subject != "[No Subject]") bugid = MyMime.GetBugidFromSubject(ref subject);

                cc = MyMime.GetCc(mimeMessage);
            }

            var sql = string.Empty;

            if (bugid != 0)
            {
                // Check if the bug is still in the database
                // No comment can be added to merged or deleted bugids
                // In this case a new bug is created, this to prevent possible loss of information

                sql = @"select count(bg_id)
                    from bugs
                    where bg_id = $id";

                sql = sql.Replace("$id", Convert.ToString(bugid));

                if (Convert.ToInt32(DbUtil.ExecuteScalar(sql)) == 0)
                {
                    bugid = 0;
                }
            }

            // Either insert a new bug or append a commment to existing bug
            // based on presence, absence of bugid
            if (bugid == 0)
            {
                // insert a new bug

                if (mimeMessage != null)
                {
                    // in case somebody is replying to a bug that has been deleted or merged
                    subject = subject.Replace(this.applicationSettings.TrackingIdString, "PREVIOUS:");

                    shortDesc = subject;
                    if (shortDesc.Length > 200)
                    {
                        shortDesc = shortDesc.Substring(0, 200);
                    }
                }

                var orgid = 0;
                var categoryid = 0;
                var priorityid = 0;
                var assignedid = 0;
                var statusid = 0;
                var udfid = 0;

                // You can control some more things from the query string
                if (!string.IsNullOrEmpty(Request["$ORGANIZATION$"]))
                {
                    orgid = Convert.ToInt32(Request["$ORGANIZATION$"]);
                }

                if (!string.IsNullOrEmpty(Request["$CATEGORY$"]))
                {
                    categoryid = Convert.ToInt32(Request["$CATEGORY$"]);
                }

                if (!string.IsNullOrEmpty(Request["$PROJECT$"]))
                {
                    projectid = Convert.ToInt32(Request["$PROJECT$"]);
                }

                if (!string.IsNullOrEmpty(Request["$PRIORITY$"]))
                {
                    priorityid = Convert.ToInt32(Request["$PRIORITY$"]);
                }

                if (!string.IsNullOrEmpty(Request["$ASSIGNEDTO$"]))
                {
                    assignedid = Convert.ToInt32(Request["$ASSIGNEDTO$"]);
                }

                if (!string.IsNullOrEmpty(Request["$STATUS$"]))
                {
                    statusid = Convert.ToInt32(Request["$STATUS$"]);
                }

                if (!string.IsNullOrEmpty(Request["$UDF$"]))
                {
                    udfid = Convert.ToInt32(Request["$UDF$"]);
                }

                var defaults = Bug.GetBugDefaults();

                // If you didn't set these from the query string, we'll give them default values
                if (projectid == 0) projectid = (int)defaults["pj"];
                if (orgid == 0) orgid = security.User.Org;
                if (categoryid == 0) categoryid = (int)defaults["ct"];
                if (priorityid == 0) priorityid = (int)defaults["pr"];
                if (statusid == 0) statusid = (int)defaults["st"];
                if (udfid == 0) udfid = (int)defaults["udf"];

                // but forced project always wins
                if (security.User.ForcedProject != 0) projectid = security.User.ForcedProject;

                var newIds = Bug.InsertBug(
                    shortDesc,
                    security,
                    string.Empty, // tags
                    projectid,
                    orgid,
                    categoryid,
                    priorityid,
                    statusid,
                    assignedid,
                    udfid,
                    string.Empty, string.Empty, string.Empty, // project specific dropdown values
                    comment,
                    comment,
                    fromAddr,
                    cc,
                    "text/plain",
                    false, // internal only
                    null, // custom columns
                    false); // suppress notifications for now - wait till after the attachments

                if (mimeMessage != null)
                {
                    MyMime.AddAttachments(mimeMessage, newIds.Bugid, newIds.Postid, security);

                    MyPop3.AutoReply(newIds.Bugid, fromAddr, shortDesc, projectid);
                }
                else if (attachmentAsBase64 != null && attachmentAsBase64.Length > 0)
                {
                    if (attachmentDesc == null) attachmentDesc = string.Empty;
                    if (attachmentContentType == null) attachmentContentType = string.Empty;
                    if (attachmentFilename == null) attachmentFilename = string.Empty;

                    var byteArray = Convert.FromBase64String(attachmentAsBase64);
                    var stream = new MemoryStream(byteArray);

                    Bug.InsertPostAttachment(
                        security,
                        newIds.Bugid,
                        stream,
                        byteArray.Length,
                        attachmentFilename,
                        attachmentDesc,
                        attachmentContentType,
                        -1, // parent
                        false, // internal_only
                        false); // don't send notification yet
                }

                // your customizations
                Bug.ApplyPostInsertRules(newIds.Bugid);

                Bug.SendNotifications(Bug.Insert, newIds.Bugid, security);
                WhatsNew.AddNews(newIds.Bugid, shortDesc, "added", security);

                Response.AddHeader("BTNET", "OK:" + Convert.ToString(newIds.Bugid));

                return Content("OK:" + Convert.ToString(newIds.Bugid));
            }
            else // update existing bug
            {
                var statusResultingFromIncomingEmail = this.applicationSettings.StatusResultingFromIncomingEmail;

                sql = string.Empty;

                if (statusResultingFromIncomingEmail != 0)
                {
                    sql = @"update bugs
                        set bg_status = $st
                        where bg_id = $bg";

                    sql = sql.Replace("$st", statusResultingFromIncomingEmail.ToString());
                }

                sql += "select bg_short_desc from bugs where bg_id = $bg";

                sql = sql.Replace("$bg", Convert.ToString(bugid));
                var dr2 = DbUtil.GetDataRow(sql);

                // Add a comment to existing bug.
                var postid = Bug.InsertComment(
                    bugid,
                    security.User.Usid, // (int) dr["us_id"],
                    comment,
                    comment,
                    fromAddr,
                    cc,
                    "text/plain",
                    false); // internal only

                if (mimeMessage != null)
                {
                    MyMime.AddAttachments(mimeMessage, bugid, postid, security);
                }
                else if (attachmentAsBase64 != null && attachmentAsBase64.Length > 0)
                {
                    if (attachmentDesc == null) attachmentDesc = string.Empty;
                    if (attachmentContentType == null) attachmentContentType = string.Empty;
                    if (attachmentFilename == null) attachmentFilename = string.Empty;

                    var byteArray = Convert.FromBase64String(attachmentAsBase64);

                    using (var stream = new MemoryStream(byteArray))
                    {
                        Bug.InsertPostAttachment(
                            security,
                            bugid,
                            stream,
                            byteArray.Length,
                            attachmentFilename,
                            attachmentDesc,
                            attachmentContentType,
                            -1, // parent
                            false, // internal_only
                            false); // don't send notification yet
                    }
                }

                Bug.SendNotifications(Bug.Update, bugid, security);
                WhatsNew.AddNews(bugid, (string)dr2["bg_short_desc"], "updated", security);

                Response.AddHeader("BTNET", "OK:" + Convert.ToString(bugid));

                return Content("OK:" + Convert.ToString(bugid));
            }
        }

        [HttpGet]
        [Authorize(Roles = ApplicationRoles.Member)]
        public ActionResult Delete(int id)
        {
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
        [Authorize(Roles = ApplicationRoles.Member)]
        public ActionResult Delete(DeleteModel model)
        {
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

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public void Print(string format, int? queryId)
        {
            // TODO
            //if (Request["format"] != "excel")
            //{
            //    Util.DoNotCache(System.Web.HttpContext.Current.Response);
            //};

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
        [Authorize(Roles = ApplicationRoles.Member)]
        public ActionResult Subscribe(int id, string actn)
        {
            var permissionLevel = Bug.GetBugPermissionLevel(id, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content(string.Empty);
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
        [Authorize(Roles = ApplicationRoles.Member)]
        public ActionResult Merge(int id)
        {
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
        [Authorize(Roles = ApplicationRoles.Member)]
        public ActionResult Merge(MergeModel model)
        {
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

        [HttpGet]
        public ActionResult ViewSubscriber(int id)
        {
            var permissionLevel = Bug.GetBugPermissionLevel(id, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content("You are not allowed to view this item");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - view subscribers",
                SelectedItem = ApplicationSettings.PluralBugLabelDefault
            };

            // clean up bug subscriptions that no longer fit the security restrictions

            Bug.AutoSubscribe(id);

            // show who is subscribed

            var sql = string.Empty;
            var token = new HtmlHelper(new ViewContext(), new ViewPage()).AntiForgeryToken().ToString();

            if (this.security.User.IsAdmin)
            {
                sql = @"
                    select
                        '<form action=/Bug/DeleteSubscriber method=post id=f_$bg_' + convert(varchar, us_id) + '>
                            $token
                            <input type=hidden name=id value=$bg>
                            <input type=hidden name=userId value=' + convert(varchar, us_id) + '>

                            <a href=# onclick=document.getElementById(''f_$bg_' + convert(varchar, us_id) + ''').submit();>unsubscribe</a>
                        </form>' [$no_sort_unsubscriber],
                    us_username [user],
                    us_lastname + ', ' + us_firstname [name],
                    us_email [email],
                    case when us_reported_notifications < 4 or us_assigned_notifications < 4 or us_subscribed_notifications < 4 then 'Y' else 'N' end [user is<br>filtering<br>notifications]
                    from bug_subscriptions
                    inner join users on bs_user = us_id
                    where bs_bug = $bg
                    and us_enable_notifications = 1
                    and us_active = 1
                    order by 1";
            }
            else
            {
                sql = @"
                    select
                    us_username [user],
                    us_lastname + ', ' + us_firstname [name],
                    us_email [email],
                    case when us_reported_notifications < 4 or us_assigned_notifications < 4 or us_subscribed_notifications < 4 then 'Y' else 'N' end [user is<br>filtering<br>notifications]
                    from bug_subscriptions
                    inner join users on bs_user = us_id
                    where bs_bug = $bg
                    and us_enable_notifications = 1
                    and us_active = 1
                    order by 1";
            }

            sql = sql.Replace("$token", token);
            sql = sql.Replace("$bg", Convert.ToString(id));

            ViewBag.Table = new SortableTableModel
            {
                DataSet = DbUtil.GetDataSet(sql),
                HtmlEncode = false
            };

            // Get list of users who could be subscribed to this bug.

            sql = @"
                declare @project int;
                declare @org int;
                select @project = bg_project, @org = bg_org from bugs where bg_id = $bg;";

            // Only users explicitly allowed will be listed
            if (this.applicationSettings.DefaultPermissionLevel == 0)
            {
                sql +=
                    @"select us_id, case when $fullnames then us_lastname + ', ' + us_firstname else us_username end us_username
                    from users
                    where us_active = 1
                    and us_enable_notifications = 1
                    and us_id in
                        (select pu_user from project_user_xref
                        where pu_project = @project
                        and pu_permission_level <> 0)
                    and us_id not in (
                        select us_id
                        from bug_subscriptions
                        inner join users on bs_user = us_id
                        where bs_bug = $bg
                        and us_enable_notifications = 1
                        and us_active = 1)
                    and us_id not in (
                        select us_id from users
                        inner join orgs on us_org = og_id
                        where us_org <> @org
                        and og_other_orgs_permission_level = 0)
                    order by us_username; ";
                // Only users explictly DISallowed will be omitted
            }
            else
            {
                sql +=
                    @"select us_id, case when $fullnames then us_lastname + ', ' + us_firstname else us_username end us_username
                    from users
                    where us_active = 1
                    and us_enable_notifications = 1
                    and us_id not in
                        (select pu_user from project_user_xref
                        where pu_project = @project
                        and pu_permission_level = 0)
                    and us_id not in (
                        select us_id
                        from bug_subscriptions
                        inner join users on bs_user = us_id
                        where bs_bug = $bg
                        and us_enable_notifications = 1
                        and us_active = 1)
                    and us_id not in (
                        select us_id from users
                        inner join orgs on us_org = og_id
                        where us_org <> @org
                        and og_other_orgs_permission_level = 0)
                    order by us_username; ";
            }

            if (!this.applicationSettings.UseFullNames)
            {
                // false condition
                sql = sql.Replace("$fullnames", "0 = 1");
            }
            else
            {
                // true condition
                sql = sql.Replace("$fullnames", "1 = 1");
            }

            sql = sql.Replace("$bg", Convert.ToString(id));

            ViewBag.Users = new List<SelectListItem>();

            var usersDataView = DbUtil.GetDataView(sql);

            foreach (DataRowView row in usersDataView)
            {
                ViewBag.Users.Add(new SelectListItem
                {
                    Value = ((int)row["us_id"]).ToString(),
                    Text = (string)row["us_username"]
                });
            }

            if (ViewBag.Users.Count == 0)
            {
                ViewBag.Users.Insert(0, new SelectListItem
                {
                    Value = "0",
                    Text = "[no users to select]"
                });
            }
            else
            {
                ViewBag.Users.Insert(0, new SelectListItem
                {
                    Value = "0",
                    Text = "[select to add]"
                });
            }

            var model = new CreateSubscriberModel
            {
                Id = id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateSubscriber(CreateSubscriberModel model)
        {
            var permissionLevel = Bug.GetBugPermissionLevel(model.Id, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content("You are not allowed to view this item");
            }

            if (permissionLevel == SecurityPermissionLevel.PermissionReadonly)
            {
                return Content("You are not allowed to edit this item");
            }

            if (model.UserId != 0)
            {
                var newSubscriberUserid = Convert.ToInt32(Request["userid"]);

                var sql = @"delete from bug_subscriptions where bs_bug = $bg and bs_user = $us;
                        insert into bug_subscriptions (bs_bug, bs_user) values($bg, $us)";

                sql = sql.Replace("$bg", Convert.ToString(model.UserId));
                sql = sql.Replace("$us", Convert.ToString(newSubscriberUserid));

                DbUtil.ExecuteNonQuery(sql);

                // send a notification to this user only
                Bug.SendNotifications(Bug.Update, model.Id, this.security, newSubscriberUserid);
            }

            return RedirectToAction(nameof(ViewSubscriber), new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ApplicationRoles.Administrator)]
        public ActionResult DeleteSubscriber(DeleteSubscriberModel model)
        {
            var sql = "delete from bug_subscriptions where bs_bug = $bg_id and bs_user = $us_id";

            sql = sql.Replace("$bg_id", model.Id.ToString());
            sql = sql.Replace("$us_id", model.UserId.ToString());

            DbUtil.ExecuteNonQuery(sql);

            return RedirectToAction(nameof(ViewSubscriber), new { id = model.Id });
        }

        [HttpGet]
        public ActionResult Vote(int bugid, int vote)
        {
            var dv = (DataView)Session["bugs"];

            if (dv == null)
            {
                return Content(string.Empty);
            }

            var permissionLevel = Bug.GetBugPermissionLevel(bugid, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content(string.Empty);
            }

            for (var i = 0; i < dv.Count; i++)
            {
                if ((int)dv[i][1] == bugid)
                {
                    // treat it like a delta and update the cached vote count.
                    var objVoteCount = HttpContext.ApplicationInstance.Application[Convert.ToString(bugid)];
                    var voteCount = 0;

                    if (objVoteCount != null)
                    {
                        voteCount = (int)objVoteCount;
                    }

                    voteCount += vote;

                    HttpContext.ApplicationInstance.Application[Convert.ToString(bugid)] = voteCount;

                    // now treat it more like a boolean
                    if (vote == -1)
                    {
                        vote = 0;
                    }

                    dv[i]["$VOTE"] = vote;

                    var sql = @"
                        if not exists (select bu_bug from bug_user where bu_bug = $bg and bu_user = $us)
                        insert into bug_user (bu_bug, bu_user, bu_flag, bu_seen, bu_vote) values($bg, $us, 0, 0, 1) 
                        update bug_user set bu_vote = $vote, bu_vote_datetime = getdate() where bu_bug = $bg and bu_user = $us and bu_vote <> $vote"
                        .Replace("$vote", Convert.ToString(vote))
                        .Replace("$bg", Convert.ToString(bugid))
                        .Replace("$us", Convert.ToString(this.security.User.Usid));

                    DbUtil.ExecuteNonQuery(sql);

                    break;
                }
            }

            return Content("OK");
        }

        [HttpGet]
        public ActionResult Flag(int bugid, int flag)
        {
            var dv = (DataView)Session["bugs"];

            if (dv == null)
            {
                return Content(string.Empty);
            }

            var permissionLevel = Bug.GetBugPermissionLevel(bugid, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content(string.Empty);
            }

            for (var i = 0; i < dv.Count; i++)
            {
                if ((int)dv[i][1] == bugid)
                {
                    dv[i]["$FLAG"] = flag;

                    var sql = @"
                        if not exists (select bu_bug from bug_user where bu_bug = $bg and bu_user = $us)
                        insert into bug_user (bu_bug, bu_user, bu_flag, bu_seen, bu_vote) values($bg, $us, 1, 0, 0) 
                        update bug_user set bu_flag = $fl, bu_flag_datetime = getdate() where bu_bug = $bg and bu_user = $us and bu_flag <> $fl"
                    .Replace("$bg", Convert.ToString(bugid))
                    .Replace("$us", Convert.ToString(this.security.User.Usid))
                    .Replace("$fl", Convert.ToString(flag));

                    DbUtil.ExecuteNonQuery(sql);
                    break;
                }
            }

            return Content("OK");
        }

        [HttpGet]
        public ActionResult Seen(int bugid, int seen)
        {
            var dv = (DataView)Session["bugs"];

            if (dv == null)
            {
                return Content(string.Empty);
            }

            var permissionLevel = Bug.GetBugPermissionLevel(bugid, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content(string.Empty);
            }

            for (var i = 0; i < dv.Count; i++)
            {
                if ((int)dv[i][1] == bugid)
                {
                    dv[i]["$SEEN"] = seen;

                    var sql = @"
                        if not exists (select bu_bug from bug_user where bu_bug = $bg and bu_user = $us)
                        insert into bug_user (bu_bug, bu_user, bu_flag, bu_seen, bu_vote) values($bg, $us, 0, 1, 0) 
                        update bug_user set bu_seen = $seen, bu_seen_datetime = getdate() where bu_bug = $bg and bu_user = $us and bu_seen <> $seen"
                    .Replace("$seen", Convert.ToString(seen))
                    .Replace("$bg", Convert.ToString(bugid))
                    .Replace("$us", Convert.ToString(this.security.User.Usid));

                    DbUtil.ExecuteNonQuery(sql);

                    break;
                }
            }

            return Content("OK");
        }

        private void LoadQueryDropdown()
        {
            // populate query drop down
            var sql = @"/* query dropdown */
                select qu_id, qu_desc
                from queries
                where (isnull(qu_user,0) = 0 and isnull(qu_org,0) = 0)
                or isnull(qu_user,0) = $us
                or isnull(qu_org,0) = $org
                order by qu_desc";

            sql = sql.Replace("$us", Convert.ToString(this.security.User.Usid));
            sql = sql.Replace("$org", Convert.ToString(this.security.User.Org));

            ViewBag.Queries = new List<SelectListItem>();

            foreach (DataRowView row in DbUtil.GetDataView(sql))
            {
                ViewBag.Queries.Add(new SelectListItem
                {
                    Value = ((int)row["qu_id"]).ToString(),
                    Text = (string)row["qu_desc"],
                });
            }
        }

        private string DoQuery(IndexModel model)
        {
            // figure out what SQL to run and run it.
            string bugSql = null;

            // From the URL
            if (model.QueryId == 0)
            {
                // specified in URL?
                model.QueryId = Convert.ToInt32(Util.SanitizeInteger(Request["qu_id"]));
            }

            // From a previous viewing of this page
            if (model.QueryId == 0 && Session["SelectedBugQuery"] != null)
            {
                // Is there a previously selected query, from a use of this page
                // earlier in this session?
                model.QueryId = (int)Session["SelectedBugQuery"];
            }

            if (model.QueryId != 0)
            {
                // Use sql specified in query string.
                // This is the normal path from the queries page.
                var sql = @"select qu_sql from queries where qu_id = $quid";

                sql = sql.Replace("$quid", Convert.ToString(model.QueryId));

                bugSql = (string)DbUtil.ExecuteScalar(sql);
            }

            if (bugSql == null)
            {
                // This is the normal path after logging in.
                // Use sql associated with user
                var sql = @"select qu_id, qu_sql from queries where qu_id in
                    (select us_default_query from users where us_id = $us)";

                sql = sql.Replace("$us", Convert.ToString(this.security.User.Usid));

                var dr = DbUtil.GetDataRow(sql);

                if (dr != null)
                {
                    model.QueryId = (int)dr["qu_id"];

                    bugSql = (string)dr["qu_sql"];
                }
            }

            // As a last resort, grab some query.
            if (bugSql == null)
            {
                var sql =
                    @"select top 1 qu_id, qu_sql from queries order by case when qu_default = 1 then 1 else 0 end desc";

                var dr = DbUtil.GetDataRow(sql);

                bugSql = (string)dr["qu_sql"];

                if (dr != null)
                {
                    model.QueryId = (int)dr["qu_id"];

                    bugSql = (string)dr["qu_sql"];
                }
            }

            if (bugSql == null)
            {
                return "Error!. No queries available for you to use!<p>Please contact your BugTracker.NET administrator.";
            }

            // replace magic variables
            bugSql = bugSql.Replace("$ME", Convert.ToString(this.security.User.Usid));

            bugSql = Util.AlterSqlPerProjectPermissions(bugSql, security);

            if (!this.applicationSettings.UseFullNames)
            {
                // false condition
                bugSql = bugSql.Replace("$fullnames", "0 = 1");
            }
            else
            {
                // true condition
                bugSql = bugSql.Replace("$fullnames", "1 = 1");
            }

            // run the query
            DataSet ds = null;

            try
            {
                ds = DbUtil.GetDataSet(bugSql);

                ViewBag.DataView = new DataView(ds.Tables[0]);
            }
            catch (SqlException e)
            {
                ViewBag.SqlError = e.Message;
                ViewBag.DataView = null;
            }

            // Save it.
            Session["bugs"] = ViewBag.DataView;
            Session["SelectedBugQuery"] = model.QueryId;

            // Save it again.  We use the unfiltered query to determine the
            // values that go in the filter dropdowns.
            if (ds != null)
            {
                Session["bugs_unfiltered"] = ds.Tables[0];
            }
            else
            {
                Session["bugs_unfiltered"] = null;
            }

            return string.Empty;
        }

        private void CallSortAndFilterBuglistDataview(IndexModel model, bool postBack)
        {
            var filterVal = model.Filter;
            var sortVal = model.Sort.ToString();
            var prevSortVal = model.PrevSort.ToString();
            var prevDirVal = model.PrevDir;

            BugList.SortAndFilterBugListDataView((DataView)ViewBag.DataView, postBack, model.Action,
                ref filterVal,
                ref sortVal,
                ref prevSortVal,
                ref prevDirVal);

            model.Filter = filterVal;
            model.Sort = Convert.ToInt32(sortVal);
            model.PrevSort = Convert.ToInt32(prevSortVal);
            model.PrevDir = prevDirVal;
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