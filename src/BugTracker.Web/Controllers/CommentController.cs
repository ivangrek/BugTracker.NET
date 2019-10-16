/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Controllers
{
    using BugTracker.Web.Core;
    using BugTracker.Web.Models;
    using BugTracker.Web.Models.Comment;
    using System;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;

    [Authorize]
    [OutputCache(Location = OutputCacheLocation.None)]
    public class CommentController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;

        public CommentController(
            IApplicationSettings applicationSettings,
            ISecurity security)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
        }

        [HttpGet]
        public ActionResult Update(int id, int bugId)
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditAndDeletePosts;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var sql = @"select bp_comment, bp_type,
                isnull(bp_comment_search,bp_comment) bp_comment_search,
                isnull(bp_content_type,'') bp_content_type,
                bp_bug, bp_hidden_from_external_users
                from bug_posts where bp_id = $id"
                .Replace("$id", Convert.ToString(id));

            var dr = DbUtil.GetDataRow(sql);

            var permissionLevel = Bug.GetBugPermissionLevel(bugId, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone
                || permissionLevel == SecurityPermissionLevel.PermissionReadonly
                || (string)dr["bp_type"] != "comment")
            {
                return Content("You are not allowed to use this page.");
            }

            var contentType = (string)dr["bp_content_type"];

            if (this.security.User.UseFckeditor && contentType == "text/html" &&
                !this.applicationSettings.DisableFCKEditor)
            {
                ViewBag.UseFckeditor = true;
            }
            else
            {
                ViewBag.UseFckeditor = false;
            }

            if (!this.security.User.ExternalUser && this.applicationSettings.EnableInternalOnlyPosts)
            {
                ViewBag.ShowInternalOnly = true;
            }
            else
            {
                ViewBag.ShowInternalOnly = false;
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit comment",
                SelectedItem = this.applicationSettings.PluralBugLabel
            };

            var model = new UpdateModel
            {
                Id = id,
                BugId = bugId,
                Comment = ViewBag.UseFckeditor
                    ? (string)dr["bp_comment"]
                    : (string)dr["bp_comment_search"],
                InternalOnly = Convert.ToBoolean((int)dr["bp_hidden_from_external_users"])
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(UpdateModel model)
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditAndDeletePosts;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var sql = @"select bp_bug, bp_type,
                isnull(bp_content_type,'') bp_content_type,
                bp_hidden_from_external_users
                from bug_posts where bp_id = $id"
                .Replace("$id", Convert.ToString(model.Id));

            var dr = DbUtil.GetDataRow(sql);

            var permissionLevel = Bug.GetBugPermissionLevel(model.BugId, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone
                || permissionLevel == SecurityPermissionLevel.PermissionReadonly
                || (string)dr["bp_type"] != "comment")
            {
                return Content("You are not allowed to use this page.");
            }

            var contentType = (string)dr["bp_content_type"];

            if (this.security.User.UseFckeditor && contentType == "text/html" &&
                !this.applicationSettings.DisableFCKEditor)
            {
                ViewBag.UseFckeditor = true;
            }
            else
            {
                ViewBag.UseFckeditor = false;
            }

            if (!ModelState.IsValid)
            {
                if (!this.security.User.ExternalUser && this.applicationSettings.EnableInternalOnlyPosts)
                {
                    ViewBag.ShowInternalOnly = true;
                }
                else
                {
                    ViewBag.ShowInternalOnly = false;
                }

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - edit comment",
                    SelectedItem = this.applicationSettings.PluralBugLabel
                };

                return View(model);
            }

            sql = @"update bug_posts set
                    bp_comment = N'$cm',
                    bp_comment_search = N'$cs',
                    bp_content_type = N'$cn',
                    bp_hidden_from_external_users = $internal
                where bp_id = $id
                select bg_short_desc from bugs where bg_id = $bugid";

            if (ViewBag.UseFckeditor)
            {
                var text = Util.StripDangerousTags(model.Comment);

                sql = sql.Replace("$cm", text.Replace("'", "&#39;"));
                sql = sql.Replace("$cs", Util.StripHtml(model.Comment).Replace("'", "''"));
                sql = sql.Replace("$cn", "text/html");
            }
            else
            {
                sql = sql.Replace("$cm", HttpUtility.HtmlDecode(model.Comment).Replace("'", "''"));
                sql = sql.Replace("$cs", model.Comment.Replace("'", "''"));
                sql = sql.Replace("$cn", "text/plain");
            }

            sql = sql.Replace("$id", Convert.ToString(model.Id));
            sql = sql.Replace("$bugid", Convert.ToString(model.BugId));
            sql = sql.Replace("$internal", Util.BoolToString(model.InternalOnly));

            dr = DbUtil.GetDataRow(sql);

            // Don't send notifications for internal only comments.
            // We aren't putting them the email notifications because it that makes it
            // easier for them to accidently get forwarded to the "wrong" people...
            if (!model.InternalOnly)
            {
                Bug.SendNotifications(Bug.Update, model.BugId, this.security);
                WhatsNew.AddNews(model.BugId, (string)dr["bg_short_desc"], "updated", security);
            }

            return Redirect($"~/Bugs/Edit.aspx?id={model.BugId}");
        }

        [HttpGet]
        public ActionResult Delete(int id, int bugId)
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditAndDeletePosts;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var permissionLevel = Bug.GetBugPermissionLevel(Convert.ToInt32(bugId), this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                Response.Write("You are not allowed to edit this item");
                Response.End();
            }

            var sql = @"select bp_comment from bug_posts where bp_id = $1"
                .Replace("$1", id.ToString());

            var dr = DbUtil.GetDataRow(sql);

            // show the first few chars of the comment
            var comment = Convert.ToString(dr["bp_comment"]);
            var len = 20;

            if (comment.Length < len)
            {
                len = comment.Length;
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete comment",
                SelectedItem = this.applicationSettings.PluralBugLabel
            };

            var model = new DeleteModel
            {
                Id = id,
                BugId = bugId,
                Comment = comment.Substring(0, len)
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(DeleteModel model)
        {
            this.security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditAndDeletePosts;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var permissionLevel = Bug.GetBugPermissionLevel(Convert.ToInt32(model.BugId), this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                Response.Write("You are not allowed to edit this item");
                Response.End();
            }

            // do delete here
            var sql = @"delete bug_posts where bp_id = $1"
                .Replace("$1", model.Id.ToString());

            DbUtil.ExecuteNonQuery(sql);

            return Redirect($"~/Bugs/Edit.aspx?id={model.BugId}");
        }
    }
}