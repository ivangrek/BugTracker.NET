/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Mail
{
    using System;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Text;
    using Identification;
    using OpenPop.Mime;

    public static class Mime
    {
        private static IApplicationSettings ApplicationSettings { get; set; } = new ApplicationSettings();

        public static Message GetMimeMessage(string messageRawString)
        {
            // feed a stream to MIME parser
            var bytes = Encoding.UTF8.GetBytes(messageRawString);

            return new Message(bytes);
        }

        public static int GetBugIdFromSubject(ref string subject)
        {
            var bugId = 0;

            // Try to parse out the bugid from the subject line
            var bugIdString = ApplicationSettings.TrackingIdString;

            var pos = subject.IndexOf(bugIdString);

            if (pos >= 0)
            {
                // position of colon
                pos = subject.IndexOf(":", pos);
                pos++;

                // position of close paren
                var pos2 = subject.IndexOf(")", pos);
                if (pos2 > pos)
                {
                    var bugidStringTemp = subject.Substring(pos, pos2 - pos);
                    if (Util.IsInt(bugidStringTemp)) bugId = Convert.ToInt32(bugidStringTemp);
                }
            }

            // maybe a deleted bug?
            if (bugId != 0)
            {
                var sql = "select count(1) from bugs where bg_id = $bg";
                sql = sql.Replace("$bg", Convert.ToString(bugId));
                var bugCount = (int)DbUtil.ExecuteScalar(sql);
                if (bugCount != 1)
                {
                    subject = subject.Replace(bugIdString, "WAS #:");
                    bugId = 0;
                }
            }

            return bugId;
        }

        public static string GetFromAddr(Message message)
        {
            return message.Headers.From.Address;
        }

        public static string GetSubject(Message message)
        {
            var subject = message.Headers.Subject;

            if (string.IsNullOrWhiteSpace(subject))
            {
                subject = "[No Subject]";
            }

            return subject;
        }

        public static string GetCc(Message message)
        {
            var cc = string.Join("; ", message.Headers.Cc.Select(c => c.Address));

            return cc;
        }

        public static string GetTo(Message message)
        {
            return string.Join("; ", message.Headers.To.Select(c => c.Address));
        }

        public static string GetComment(Message message)
        {
            string commentText = null;
            var comment = message.FindFirstPlainTextVersion();

            if (comment != null)
            {
                commentText = comment.GetBodyAsText();

                if (string.IsNullOrEmpty(commentText))
                {
                    comment = message.FindFirstHtmlVersion();

                    if (comment != null)
                    {
                        commentText = comment.GetBodyAsText();
                    }
                }
            }

            if (string.IsNullOrEmpty(commentText))
            {
                commentText = "NO PLAIN TEXT MESSAGE BODY FOUND";
            }

            return commentText;
        }

        public static DataRow GetUserDataRowMaybeUsingFromAddr(Message message, string fromAddr, string username)
        {
            var sql = new SqlString(@"
                select
                    us_id,
                    us_admin,
                    us_username,
                    us_org,
                    og_other_orgs_permission_level,
                    isnull(us_forced_project, 0) us_forced_project
                from
                    users
                    
                    inner join
                        orgs
                    on
                        us_org = og_id
                where
                    us_username = @us");

            // Create a new user from the "from" email address    
            var btnetServiceUsername = ApplicationSettings.CreateUserFromEmailAddressIfThisUsername;

            DataRow dr;

            if (!string.IsNullOrEmpty(fromAddr) && username == btnetServiceUsername)
            {
                fromAddr = GetFromAddr(message);

                // See if there's already a username that matches this email address
                username = Email.SimplifyEmailAddress(fromAddr);

                // Does a user with this email already exist?
                sql = sql.AddParameterWithValue("us", username);

                // We maybe found user@example.com, so let's use him as the user instead of the BugTracker.MailService.exe user
                dr = DbUtil.GetDataRow(sql);

                // We didn't find the user, so let's create him, using the email address as the username.	
                if (dr == null)
                {

                    var useDomainAsOrgName = ApplicationSettings.UseEmailDomainAsNewOrgNameWhenCreatingNewUser;

                    User.CopyUser(
                        username,
                        username,
                        string.Empty, string.Empty, string.Empty,  // first, last, signature
                        0,  // salt
                        Guid.NewGuid().ToString(), // random value for password,
                        ApplicationSettings.CreateUsersFromEmailTemplate,
                        useDomainAsOrgName);

                    // now that we have created a user, try again
                    dr = DbUtil.GetDataRow(sql);
                }
            }
            else
            {
                // Use the BugTracker.MailService.exe user as the username
                sql = sql.AddParameterWithValue("us", username);
                dr = DbUtil.GetDataRow(sql);
            }

            return dr;
        }

        public static void AddAttachments(Message message, int bugId, int parentPostId, ISecurity security)
        {
            foreach (var attachment in message.FindAllAttachments())
            {
                AddAttachment(attachment.FileName, attachment, bugId, parentPostId, security);
            }
        }

        public static void AddAttachment(string filename, MessagePart part, int bugId, int parentPostId, ISecurity security)
        {
            Util.WriteToLog("attachment:" + filename);

            var missingAttachmentMsg = string.Empty;
            var maxUploadSize = ApplicationSettings.MaxUploadSize;

            if (part.Body.Length > maxUploadSize)
            {
                missingAttachmentMsg = "ERROR: email attachment exceeds size limit.";
            }

            var contentType = part.ContentType.MediaType;
            string desc;
            var attachmentStream = new MemoryStream(part.Body);

            if (string.IsNullOrEmpty(missingAttachmentMsg))
            {
                desc = "email attachment";
            }
            else
            {
                desc = missingAttachmentMsg;
            }

            attachmentStream.Position = 0;

            Bug.InsertPostAttachment(
                security,
                bugId,
                attachmentStream,
                (int)attachmentStream.Length,
                filename,
                desc,
                contentType,
                parentPostId,
                false,  // not hidden
                false); // don't send notifications
        }

        public static string GetHeadersForComment(Message message)
        {
            var headers = string.Empty;
            var subject = GetSubject(message);

            if (!string.IsNullOrEmpty(subject))
            {
                headers = "Subject: " + subject + "\n";
            }

            var to = GetTo(message);

            if (!string.IsNullOrEmpty(to))
            {
                headers += "To: " + to + "\n";
            }

            var cc = GetCc(message);

            if (!string.IsNullOrEmpty(cc))
            {
                headers += "Cc: " + cc + "\n";
            }

            return headers;
        }

        public static Security GetSynthesizedSecurity(Message message, string fromAddr, string username)
        {
            // Get the btnet user, which might actually be a user that corresonds with the email sender, not the username above
            var dr = GetUserDataRowMaybeUsingFromAddr(message, fromAddr, username);

            // simulate a user having logged in, for downstream code
            var security = new Security(new ApplicationSettings());

            security.User.Username = username;
            security.User.Usid = (int)dr["us_id"];
            security.User.IsAdmin = Convert.ToBoolean(dr["us_admin"]);
            security.User.Org = (int)dr["us_org"];
            security.User.OtherOrgsPermissionLevel = (SecurityPermissionLevel)(int)dr["og_other_orgs_permission_level"];
            security.User.ForcedProject = (int)dr["us_forced_project"];

            return security;
        }
    }
}