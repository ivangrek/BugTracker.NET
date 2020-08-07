/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Controllers
{
    using Core;
    using Models;
    using Models.Attachment;
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Text;
    using System.Web.Mvc;
    using System.Web.UI;
    using Core.Identification;

    [Authorize]
    [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
    public class AttachmentController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;
        private readonly IReportService reportService;

        public AttachmentController(
            IApplicationSettings applicationSettings,
            ISecurity security,
            IReportService reportService)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
            this.reportService = reportService;
        }

        [HttpGet]
        public ActionResult Show(int id, int bugId, bool download)
        {
            var sql = @"
                select bp_file, isnull(bp_content_type,'') [bp_content_type] 
                from bug_posts 
                where bp_id = $bp_id 
                and bp_bug = $bug_id"
                .Replace("$bp_id", id.ToString())
                .Replace("$bug_id", bugId.ToString());

            var dr = DbUtil.GetDataRow(sql);

            if (dr == null)
            {
                return Content(string.Empty);
            }

            var permissionLevel = Bug.GetBugPermissionLevel(Convert.ToInt32(bugId), this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content("You are not allowed to view this item");
            }

            var filename = (string)dr["bp_file"];
            var contentType = (string)dr["bp_content_type"];

            // First, try to find it in the bug_post_attachments table.
            sql = @"select bpa_content
                from bug_post_attachments
                where bpa_post = @bp_id";

            var foundInDatabase = false;
            string foundAtPath = null;

            using (var cmd = new SqlCommand(sql))
            {
                cmd.Parameters.AddWithValue("@bp_id", id);

                // Use an SqlDataReader so that we can write out the blob data in chunks.
                using (var reader = DbUtil.ExecuteReader(cmd, CommandBehavior.CloseConnection | CommandBehavior.SequentialAccess))
                {
                    if (reader.Read()) // Did we find the content in the database?
                    {
                        foundInDatabase = true;
                    }
                    else
                    {
                        // Otherwise, try to find the content in the UploadFolder.
                        var uploadFolder = Util.GetUploadFolder();

                        if (uploadFolder != null)
                        {
                            var path = new StringBuilder(uploadFolder);

                            path.Append("\\");
                            path.Append(bugId);
                            path.Append("_");
                            path.Append(id);
                            path.Append("_");
                            path.Append(filename);

                            if (System.IO.File.Exists(path.ToString()))
                            {
                                foundAtPath = path.ToString();
                            }
                        }
                    }

                    // We must have found the content in the database or on the disk to proceed.
                    if (!foundInDatabase && foundAtPath == null)
                    {
                        return Content($"File not found:<br>{filename}");
                    }

                    // Write the ContentType header.

                    if (string.IsNullOrEmpty(contentType))
                    {
                        contentType = Util.FilenameToContentType(filename);
                    }

                    if (download)
                    {
                        Response.AddHeader("content-disposition", $"attachment; filename=\"{filename}\"");
                    }
                    else
                    {
                        Response.Cache.SetExpires(DateTime.Now.AddDays(3));
                        Response.AddHeader("content-disposition", $"inline; filename=\"{filename}\"");
                    }

                    // Write the data.
                    if (foundInDatabase)
                    {
                        return File((byte[])reader[0], contentType);
                    }
                    else if (foundAtPath != null)
                    {
                        if (this.applicationSettings.UseTransmitFileInsteadOfWriteFile)
                        {
                            //Response.TransmitFile(foundAtPath);
                            return File(System.IO.File.ReadAllBytes(foundAtPath), contentType);
                        }
                        else
                        {
                            //Response.WriteFile(foundAtPath);
                            return File(foundAtPath, contentType);
                        }
                    }
                    else
                    {
                        return Content($"File not found:<br>{filename}");
                    }
                }
            }
        }

        [HttpGet]
        public ActionResult Create(int id)
        {
            var bugId = id;

            if (bugId == 0)
            {
                var message = BuildMsg("Invalid id.", false, bugId);

                return Content(message);
            }

            var permissionLevel = Bug.GetBugPermissionLevel(bugId, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone
                || permissionLevel == SecurityPermissionLevel.PermissionReadonly)
            {
                var message = BuildMsg("You are not allowed to edit this item", false, bugId);

                return Content(message);
            }

            if (this.security.User.ExternalUser || !this.applicationSettings.EnableInternalOnlyPosts)
            {
                ViewBag.ShowInternalOnly = false;
            }
            else
            {
                ViewBag.ShowInternalOnly = true;
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - add attachment"
            };

            var model = new CreateModel
            {
                BugId = id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateModel model)
        {
            if (model.BugId == 0)
            {
                var message = BuildMsg("Invalid id.", false, model.BugId);

                return Content(message);
            }

            var permissionLevel = Bug.GetBugPermissionLevel(model.BugId, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone
                || permissionLevel == SecurityPermissionLevel.PermissionReadonly)
            {
                var message = BuildMsg("You are not allowed to edit this item", false, model.BugId);

                return Content(message);
            }

            if (model.File == null)
            {
                var message = BuildMsg("Please select file", false);

                return Content(message);
            }

            var filename = Path.GetFileName(model.File.FileName);

            if (string.IsNullOrEmpty(filename))
            {
                var message = BuildMsg("Please select file", false);

                return Content(message);
            }

            var maxUploadSize = this.applicationSettings.MaxUploadSize;
            var contentLength = model.File.ContentLength;

            if (contentLength > maxUploadSize)
            {
                var message = BuildMsg("File exceeds maximum allowed length of "
                          + Convert.ToString(maxUploadSize)
                          + ".", false);

                return Content(message);
            }

            if (contentLength == 0)
            {
                var message = BuildMsg("No data was uploaded.", false);

                return Content(message);
            }

            var good = false;

            try
            {
                Bug.InsertPostAttachment(this.security, model.BugId, model.File.InputStream,
                    contentLength,
                    filename, model.Description ?? string.Empty, model.File.ContentType,
                    -1, // parent
                    model.InternalOnly,
                    true);

                good = true;
            }
            catch (Exception ex)
            {
                var message = BuildMsg("caught exception:" + ex.Message, false);

                return Content(message);
            }

            if (good)
            {
                var message = BuildMsg(
                    filename
                    + " was successfully upload ("
                    + model.File.ContentType
                    + "), "
                    + Convert.ToString(contentLength)
                    + " bytes"
                    , true, model.BugId);

                return Content(message);
            }
            else
            {
                // This should never happen....
                var message = BuildMsg("Unexpected error with file upload.", false);

                return Content(message);
            }
        }

        public static string BuildMsg(string msg, bool rewritePosts, int bugId = 0)
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.Append("<html><script>");
            stringBuilder.Append("function foo() {");
            stringBuilder.Append("parent.set_msg('");
            stringBuilder.Append(msg);
            stringBuilder.Append("'); ");

            if (rewritePosts)
            {
                stringBuilder.Append($"parent.opener.rewrite_posts({bugId})");
            }

            stringBuilder.Append("}</script>");
            stringBuilder.Append("<body onload='foo()'>");
            stringBuilder.Append("</body></html>");

            return stringBuilder.ToString();
        }

        [HttpGet]
        [Authorize(Roles = ApplicationRole.Member)]
        public ActionResult Update(int id, int bugId)
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditAndDeletePosts;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var permissionLevel = Bug.GetBugPermissionLevel(bugId, this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                return Content("You are not allowed to edit this item");
            }

            if (this.security.User.ExternalUser || !this.applicationSettings.EnableInternalOnlyPosts)
            {
                ViewBag.ShowInternalOnly = false;
            }
            else
            {
                ViewBag.ShowInternalOnly = true;
            }

            // Get this entry's data from the db and fill in the form

            var sql = @"select bp_comment, bp_file, bp_hidden_from_external_users from bug_posts where bp_id = $1"
                .Replace("$1", Convert.ToString(id));

            var dr = DbUtil.GetDataRow(sql);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit attachment",
                SelectedItem = this.applicationSettings.PluralBugLabel
            };

            // Fill in this form
            ViewBag.FileName = (string)dr["bp_file"];

            var model = new UpdateModel
            {
                Id = id,
                BugId = bugId,
                Description = (string)dr["bp_comment"],
                InternalOnly = Convert.ToBoolean((int)dr["bp_hidden_from_external_users"])
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ApplicationRole.Member)]
        public ActionResult Update(UpdateModel model)
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditAndDeletePosts;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var permissionLevel = Bug.GetBugPermissionLevel(model.BugId, this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                return Content("You are not allowed to edit this item");
            }

            if (this.security.User.ExternalUser || !this.applicationSettings.EnableInternalOnlyPosts)
            {
                ViewBag.ShowInternalOnly = false;
            }
            else
            {
                ViewBag.ShowInternalOnly = true;
            }

            var sql = @"update bug_posts set
                bp_comment = N'$1',
                bp_hidden_from_external_users = $internal
                where bp_id = $3"
            .Replace("$3", Convert.ToString(model.Id))
            .Replace("$1", model.Description.Replace("'", "''"))
            .Replace("$internal", Util.BoolToString(model.InternalOnly));

            DbUtil.ExecuteNonQuery(sql);

            if (!model.InternalOnly)
            {
                Bug.SendNotifications(Bug.Update, model.BugId, this.security);
            }

            return RedirectToAction("Update", "Bug", new { id = model.BugId });
        }

        [HttpGet]
        [Authorize(Roles = ApplicationRole.Member)]
        public ActionResult Delete(int id, int bugId)
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditAndDeletePosts;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var permissionLevel = Bug.GetBugPermissionLevel(bugId, this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                return Content("You are not allowed to edit this item.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete attachment",
                SelectedItem = this.applicationSettings.PluralBugLabel
            };

            var sql = @"select bp_file from bug_posts where bp_id = $1"
                    .Replace("$1", id.ToString());

            var dataRow = DbUtil.GetDataRow(sql);

            var model = new DeleteModel
            {
                Id = id,
                BugId = bugId,
                Name = Convert.ToString(dataRow["bp_file"]),
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ApplicationRole.Member)]
        public ActionResult Delete(DeleteModel model)
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditAndDeletePosts;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var permissionLevel = Bug.GetBugPermissionLevel(model.BugId, this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                return Content("You are not allowed to edit this item.");
            }

            // save the filename before deleting the row
            var sql = @"select bp_file from bug_posts where bp_id = $ba"
                .Replace("$ba", model.Id.ToString());

            var filename = (string)DbUtil.ExecuteScalar(sql);

            // delete the row representing the attachment
            sql = @"delete bug_post_attachments where bpa_post = $ba
                delete bug_posts where bp_id = $ba"
                .Replace("$ba", model.Id.ToString());

            DbUtil.ExecuteNonQuery(sql);

            // delete the file too
            var uploadFolder = Util.GetUploadFolder();

            if (uploadFolder != null)
            {
                var path = new StringBuilder(uploadFolder);

                path.Append("\\");
                path.Append(model.BugId);
                path.Append("_");
                path.Append(model.Id);
                path.Append("_");
                path.Append(filename);

                if (System.IO.File.Exists(path.ToString()))
                {
                    System.IO.File.Delete(path.ToString());
                }
            }

            return RedirectToAction("Update", "Bug", new { id = model.BugId });
        }
    }
}