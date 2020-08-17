namespace BugTracker.Web.Controllers
{
    using System;
    using System.IO;
    using System.Web.Http;
    using btnet.Models;
    using Core;
    using Core.Identification;
    using Core.Mail;
    using OpenPop.Mime;

    [Authorize]
    public class BugFromEmailController : ApiController
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;

        public BugFromEmailController(
            IApplicationSettings applicationSettings,
            ISecurity security)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
        }

        [HttpPost]
        public IHttpActionResult Post([FromBody] BugFromEmail bugFromEmail)
        {
            if (bugFromEmail != null && ModelState.IsValid)
            {
                if (bugFromEmail.ShortDescription == null)
                {
                    bugFromEmail.ShortDescription = string.Empty;
                }
                else if (bugFromEmail.ShortDescription.Length > 200)
                {
                    bugFromEmail.ShortDescription = bugFromEmail.ShortDescription.Substring(0, 200);
                }

                Message mimeMessage = null;

                if (!string.IsNullOrEmpty(bugFromEmail.Message))
                {
                    mimeMessage = Mime.GetMimeMessage(bugFromEmail.Message);

                    bugFromEmail.Comment = Mime.GetComment(mimeMessage);

                    var headers = Mime.GetHeadersForComment(mimeMessage);

                    if (headers != string.Empty)
                    {
                        bugFromEmail.Comment = string.Format("{0}{1}{2}", headers, Environment.NewLine, bugFromEmail.Comment);
                    }

                    bugFromEmail.FromAddress = Mime.GetFromAddr(mimeMessage);
                }
                else
                {
                    if (bugFromEmail.Comment == null)
                    {
                        bugFromEmail.Comment = string.Empty;
                    }
                }

                // Even though btnet_service.exe has already parsed out the bugid,
                // we can do a better job here with SharpMimeTools.dll
                var subject = string.Empty;

                if (mimeMessage != null)
                {
                    subject = Mime.GetSubject(mimeMessage);

                    if (subject != "[No Subject]")
                    {
                        bugFromEmail.BugId = Mime.GetBugIdFromSubject(ref subject);
                    }

                    bugFromEmail.CcAddress = Mime.GetCc(mimeMessage);
                }

                SqlString sql;

                if (bugFromEmail.BugId != 0)
                {
                    // Check if the bug is still in the database
                    // No comment can be added to merged or deleted bugids
                    // In this case a new bug is created, this to prevent possible loss of information

                    sql = new SqlString(@"
                        select count(bg_id)
                        from bugs
                        where bg_id = @id");

                    sql = sql.AddParameterWithValue("id", Convert.ToString(bugFromEmail.BugId));

                    if (Convert.ToInt32(DbUtil.ExecuteScalar(sql)) == 0)
                    {
                        bugFromEmail.BugId = 0;
                    }
                }

                // Either insert a new bug or append a commment to existing bug
                // based on presence, absence of bugid
                if (bugFromEmail.BugId == 0)
                {
                    // insert a new bug
                    if (mimeMessage != null)
                    {
                        // in case somebody is replying to a bug that has been deleted or merged
                        subject = subject.Replace(this.applicationSettings.TrackingIdString, "PREVIOUS:");

                        bugFromEmail.ShortDescription = subject;

                        if (bugFromEmail.ShortDescription.Length > 200)
                        {
                            bugFromEmail.ShortDescription = bugFromEmail.ShortDescription.Substring(0, 200);
                        }
                    }

                    var defaults = Bug.GetBugDefaults();

                    // If you didn't set these from the query string, we'll give them default values
                    if (!bugFromEmail.ProjectId.HasValue || bugFromEmail.ProjectId == 0)
                    {
                        bugFromEmail.ProjectId = (int)defaults["pj"];
                    }

                    bugFromEmail.OrganizationId = bugFromEmail.OrganizationId ?? this.security.User.Org;
                    bugFromEmail.CategoryId = bugFromEmail.CategoryId ?? (int)defaults["ct"];
                    bugFromEmail.PriorityId = bugFromEmail.PriorityId ?? (int)defaults["pr"];
                    bugFromEmail.StatusId = bugFromEmail.StatusId ?? (int)defaults["st"];
                    bugFromEmail.UdfId = bugFromEmail.UdfId ?? (int)defaults["udf"];

                    // but forced project always wins
                    if (this.security.User.ForcedProject != 0)
                    {
                        bugFromEmail.ProjectId = this.security.User.ForcedProject;
                    }

                    var newIds = Bug.InsertBug(
                        bugFromEmail.ShortDescription,
                        this.security,
                        string.Empty, // tags
                        bugFromEmail.ProjectId.Value,
                        bugFromEmail.OrganizationId.Value,
                        bugFromEmail.CategoryId.Value,
                        bugFromEmail.PriorityId.Value,
                        bugFromEmail.StatusId.Value,
                        bugFromEmail.AssignedTo ?? 0,
                        bugFromEmail.UdfId.Value,
                        null,
                        null,
                        null,
                        bugFromEmail.Comment,
                        bugFromEmail.Comment,
                        bugFromEmail.FromAddress,
                        bugFromEmail.CcAddress,
                        "text/plain",
                        false, // internal only
                        null, // custom columns
                        false);  // suppress notifications for now - wait till after the attachments

                    if (mimeMessage != null)
                    {
                        Mime.AddAttachments(mimeMessage, newIds.Bugid, newIds.Postid, this.security);

                        Email.AutoReply(newIds.Bugid, bugFromEmail.FromAddress, bugFromEmail.ShortDescription, bugFromEmail.ProjectId.Value);
                    }
                    else if (bugFromEmail.Attachment != null && bugFromEmail.Attachment.Length > 0)
                    {
                        Stream stream = new MemoryStream(bugFromEmail.Attachment);

                        Bug.InsertPostAttachment(
                            this.security,
                            newIds.Bugid,
                            stream,
                            bugFromEmail.Attachment.Length,
                            bugFromEmail.AttachmentFileName ?? string.Empty,
                            bugFromEmail.AttachmentDescription ?? string.Empty,
                            bugFromEmail.AttachmentContentType ?? string.Empty,
                            -1, // parent
                            false, // internal_only
                            false); // don't send notification yet
                    }

                    // your customizations
                    Bug.ApplyPostInsertRules(newIds.Bugid);

                    Bug.SendNotifications(Bug.Insert, newIds.Bugid, this.security);
                    WhatsNew.AddNews(newIds.Bugid, bugFromEmail.ShortDescription, "added", this.security);

                    return Ok(newIds.Bugid);
                }
                else // update existing bug
                {
                    if (this.applicationSettings.StatusResultingFromIncomingEmail != 0)
                    {
                        sql = new SqlString(@"update bugs
                            set bg_status = @st
                            where bg_id = @bg");

                        sql = sql.AddParameterWithValue("st", this.applicationSettings.StatusResultingFromIncomingEmail);
                        sql = sql.AddParameterWithValue("bg", bugFromEmail.BugId);
                        DbUtil.ExecuteNonQuery(sql);
                    }

                    sql = new SqlString("select bg_short_desc from bugs where bg_id = @bg");

                    sql = sql.AddParameterWithValue("bg", bugFromEmail.BugId);

                    var dr2 = DbUtil.GetDataRow(sql);

                    // Add a comment to existing bug.
                    var postid = Bug.InsertComment(
                        bugFromEmail.BugId,
                        this.security.User.Usid, // (int) dr["us_id"],
                        bugFromEmail.Comment,
                        bugFromEmail.Comment,
                        bugFromEmail.FromAddress,
                        bugFromEmail.CcAddress,
                        "text/plain",
                        false); // internal only

                    if (mimeMessage != null)
                    {
                        Mime.AddAttachments(mimeMessage, bugFromEmail.BugId, postid, this.security);
                    }
                    else if (bugFromEmail.Attachment != null && bugFromEmail.Attachment.Length > 0)
                    {
                        Stream stream = new MemoryStream(bugFromEmail.Attachment);
                        Bug.InsertPostAttachment(
                            this.security,
                            bugFromEmail.BugId,
                            stream,
                            bugFromEmail.Attachment.Length,
                            bugFromEmail.AttachmentFileName ?? string.Empty,
                            bugFromEmail.AttachmentDescription ?? string.Empty,
                            bugFromEmail.AttachmentContentType ?? string.Empty,
                            -1, // parent
                            false, // internal_only
                            false); // don't send notification yet
                    }

                    Bug.SendNotifications(Bug.Update, bugFromEmail.BugId, this.security);
                    WhatsNew.AddNews(bugFromEmail.BugId, (string)dr2["bg_short_desc"], "updated", this.security);

                    return Ok(bugFromEmail.BugId);
                }
            }
            else
            {
                return BadRequest(ModelState);
            }
        }
    }
}
