/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Data;
    using System.IO;
    using System.Text;
    using anmar.SharpMimeTools;

    public class MyMime
    {
        public static SharpMimeMessage GetSharpMimeMessage(string messageRawString)
        {
            // feed a stream to MIME parser
            var bytes = Encoding.UTF8.GetBytes(messageRawString);
            var ms = new MemoryStream(bytes);
            return new SharpMimeMessage(ms);
        }

        public static int GetBugidFromSubject(ref string subject)
        {
            var bugid = 0;

            // Try to parse out the bugid from the subject line
            var bugidString = Util.GetSetting("TrackingIdString", "DO NOT EDIT THIS:");

            var pos = subject.IndexOf(bugidString);

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
                    if (Util.IsInt(bugidStringTemp)) bugid = Convert.ToInt32(bugidStringTemp);
                }
            }

            // maybe a deleted bug?
            if (bugid != 0)
            {
                var sql = "select count(1) from bugs where bg_id = $bg";
                sql = sql.Replace("$bg", Convert.ToString(bugid));
                var bugCount = (int)DbUtil.ExecuteScalar(sql);
                if (bugCount != 1)
                {
                    subject = subject.Replace(bugidString, "WAS #:");
                    bugid = 0;
                }
            }

            return bugid;
        }

        public static string GetFromAddr(SharpMimeMessage mimeMessage)
        {
            var fromAddr = "";

            if (mimeMessage.Header.From != null && mimeMessage.Header.From != "")
            {
                fromAddr = SharpMimeTools.parserfc2047Header(mimeMessage.Header.From);

                // handle multiline subject
                fromAddr = fromAddr.Replace("\t", " ");
            }
            else
            {
                fromAddr = "[No From]";
            }

            return fromAddr;
        }

        public static string GetSubject(SharpMimeMessage mimeMessage)
        {
            var subject = "";

            if (mimeMessage.Header.Subject != null && mimeMessage.Header.Subject != "")
            {
                subject = SharpMimeTools.parserfc2047Header(mimeMessage.Header.Subject);

                // handle multiline subject
                subject = subject.Replace("\t", " ");
            }
            else
            {
                subject = "[No Subject]";
            }

            return subject;
        }

        public static string GetCc(SharpMimeMessage mimeMessage)
        {
            var cc = "";
            if (mimeMessage.Header.Cc != null && mimeMessage.Header.Cc != "")
            {
                cc = SharpMimeTools.parserfc2047Header(mimeMessage.Header.Cc);

                // handle multiline
                cc = cc.Replace("\t", " ");
            }

            return cc;
        }

        public static string GetTo(SharpMimeMessage mimeMessage)
        {
            var to = "";
            if (mimeMessage.Header.To != null && mimeMessage.Header.To != "")
            {
                to = SharpMimeTools.parserfc2047Header(mimeMessage.Header.To);

                // handle multiline
                to = to.Replace("\t", " ");
            }

            return to;
        }

        public static string GetComment(SharpMimeMessage mimeMessage)
        {
            var comment = ExtractCommentTextFromEmail(mimeMessage, "text/plain");

            // Corey says... commenting this out 2014-04-03.  I simply can't remember what I was thinking here.
            // Why would I copy the HTML version of the email into the comment text?

            //if (comment == null)
            //{
            //    comment = MyMime.extract_comment_text_from_email(mime_message, "text/html");
            //}

            if (comment == null) comment = "NO PLAIN TEXT MESSAGE BODY FOUND";

            return comment;
        }

        public static DataRow GetUserDataRowMaybeUsingFromAddr(SharpMimeMessage mimeMessage, string fromAddr,
            string username)
        {
            DataRow dr = null;

            var sql = @"
select us_id, us_admin, us_username, us_org, og_other_orgs_permission_level, isnull(us_forced_project,0) us_forced_project
from users
inner join orgs on us_org = og_id
where us_username = N'$us'";

            // Create a new user from the "from" email address    
            var btnetServiceUsername = Util.GetSetting("CreateUserFromEmailAddressIfThisUsername", "");
            if (!string.IsNullOrEmpty(fromAddr) && username == btnetServiceUsername)
            {
                // We can do a better job of parsing the from_addr here than we did in btnet_service.exe    
                if (mimeMessage != null)
                    if (mimeMessage.Header.From != null && mimeMessage.Header.From != "")
                    {
                        fromAddr = SharpMimeTools.parserfc2047Header(mimeMessage.Header.From);

                        // handle multiline from
                        fromAddr = fromAddr.Replace("\t", " ");
                    }

                // See if there's already a username that matches this email address
                username = Email.SimplifyEmailAddress(fromAddr);

                // Does a user with this email already exist?
                sql = sql.Replace("$us", username.Replace("'", "''"));

                // We maybe found user@example.com, so let's use him as the user instead of the btnet_service.exe user
                dr = DbUtil.GetDataRow(sql);

                // We didn't find the user, so let's create him, using the email address as the username.	
                if (dr == null)
                {
                    var useDomainAsOrgName =
                        Util.GetSetting("UseEmailDomainAsNewOrgNameWhenCreatingNewUser", "0") == "1";

                    User.CopyUser(
                        username,
                        username,
                        "", "", "", // first, last, signature
                        0, // salt
                        Guid.NewGuid().ToString(), // random value for password,
                        Util.GetSetting("CreateUsersFromEmailTemplate", "[error - missing user template]"),
                        useDomainAsOrgName);

                    // now that we have created a user, try again
                    dr = DbUtil.GetDataRow(sql);
                }
            }
            else
            {
                // Use the btnet_service.exe user as the username
                sql = sql.Replace("$us", username.Replace("'", "''"));
                dr = DbUtil.GetDataRow(sql);
            }

            return dr;
        }

        // This should be rewritten with recursion...
        public static string ExtractCommentTextFromEmail(SharpMimeMessage mimeMessage, string mimetype)
        {
            string comment = null;

            // use the first plain text message body
            foreach (SharpMimeMessage part in mimeMessage)
                if (part.IsMultipart)
                {
                    foreach (SharpMimeMessage subpart in part)
                        if (subpart.IsMultipart)
                        {
                            foreach (SharpMimeMessage sub2 in subpart)
                                if (sub2.Header.ContentType.ToLower().IndexOf(mimetype) > -1
                                    && !IsAttachment(sub2))
                                {
                                    comment = sub2.BodyDecoded;
                                    break;
                                }
                        }
                        else
                        {
                            if (subpart.Header.ContentType.ToLower().IndexOf(mimetype) > -1
                                && !IsAttachment(subpart))
                            {
                                comment = subpart.BodyDecoded;
                                break;
                            }
                        }
                }
                else
                {
                    if (part.Header.ContentType.ToLower().IndexOf(mimetype) > -1
                        && !IsAttachment(part))
                    {
                        comment = part.BodyDecoded;
                        break;
                    }
                }

            if (comment == null)
                if (mimeMessage.Header.ContentType.ToLower().IndexOf(mimetype) > -1)
                    comment = mimeMessage.BodyDecoded;

            return comment;
        }

        public static bool IsAttachment(SharpMimeMessage part)
        {
            var filename = part.Header.ContentDispositionParameters["filename"];
            if (string.IsNullOrEmpty(filename)) filename = part.Header.ContentTypeParameters["name"];

            if (filename != null && filename != "")
                return true;
            return false;
        }

        public static string DeterminePartFilename(SharpMimeMessage part)
        {
            var filename = "";

            filename = part.Header.ContentDispositionParameters["filename"];

            // try again
            if (string.IsNullOrEmpty(filename)) filename = part.Header.ContentTypeParameters["name"];

            // Maybe it's still some sort of non-text part but without a filename.
            // Like an inline image, or the html alternative of a plain text body.
            if (string.IsNullOrEmpty(filename))
            {
                if (part.Header.ContentType.ToLower().IndexOf("text/plain") > -1)
                {
                    // The plain text body.  We don't want to
                    // add this as an attachment.
                }
                else
                {
                    // Some other mime part we don't understand.
                    // Let's make it an attachment with a synthesized filename, just so we don't loose it.

                    // Change text/html to text.html, etc
                    // so that downstream logic that reacts
                    // to the file extensions works.
                    filename = part.Header.ContentType;
                    filename = filename.Replace("/", ".");
                    var pos = filename.IndexOf(";");
                    if (pos > 0) filename = filename.Substring(0, pos);
                }
            }

            if (filename == null) filename = "";

            return filename;
        }

        public static void AddAttachments(SharpMimeMessage mimeMessage, int bugid, int parentPostid,
            Security security)
        {
            if (mimeMessage.IsMultipart)
            {
                foreach (SharpMimeMessage part in mimeMessage)
                    if (part.IsMultipart)
                    {
                        // recursive call to this function
                        AddAttachments(part, bugid, parentPostid, security);
                    }
                    else
                    {
                        var filename = DeterminePartFilename(part);

                        if (filename != "") AddCttachment(filename, part, bugid, parentPostid, security);
                    }
            }

            else
            {
                var filename = DeterminePartFilename(mimeMessage);

                if (filename != "") AddCttachment(filename, mimeMessage, bugid, parentPostid, security);
            }
        }

        public static void AddCttachment(string filename, SharpMimeMessage part, int bugid, int parentPostid,
            Security security)
        {
            Util.WriteToLog("attachment:" + filename);

            var missingAttachmentMsg = "";

            var maxUploadSize = Convert.ToInt32(Util.GetSetting("MaxUploadSize", "100000"));
            if (part.Size > maxUploadSize) missingAttachmentMsg = "ERROR: email attachment exceeds size limit.";

            var contentType = part.Header.TopLevelMediaType + "/" + part.Header.SubType;
            string desc;
            var attachmentStream = new MemoryStream();

            if (missingAttachmentMsg == "")
                desc = "email attachment";
            else
                desc = missingAttachmentMsg;

            part.DumpBody(attachmentStream);
            attachmentStream.Position = 0;
            Bug.InsertPostAttachment(
                security,
                bugid,
                attachmentStream,
                (int)attachmentStream.Length,
                filename,
                desc,
                contentType,
                parentPostid,
                false, // not hidden
                false); // don't send notifications
        }

        public static string GetHeadersForComment(SharpMimeMessage mimeMessage)
        {
            var headers = "";

            if (mimeMessage.Header.Subject != null && mimeMessage.Header.Subject != "")
                headers = "Subject: " + GetSubject(mimeMessage) + "\n";

            if (mimeMessage.Header.To != null && mimeMessage.Header.To != "")
                headers += "To: " + GetTo(mimeMessage) + "\n";

            if (mimeMessage.Header.Cc != null && mimeMessage.Header.Cc != "")
                headers += "Cc: " + GetCc(mimeMessage) + "\n";

            return headers;
        }

        public static Security GetSynthesizedSecurity(SharpMimeMessage mimeMessage, string fromAddr,
            string username)
        {
            // Get the btnet user, which might actually be a user that corresonds with the email sender, not the username above
            var dr = GetUserDataRowMaybeUsingFromAddr(mimeMessage, fromAddr, username);

            // simulate a user having logged in, for downstream code
            var security = new Security();

            security.User.Username = username;
            security.User.Usid = (int)dr["us_id"];
            security.User.IsAdmin = Convert.ToBoolean(dr["us_admin"]);
            security.User.Org = (int)dr["us_org"];
            security.User.OtherOrgsPermissionLevel = (int)dr["og_other_orgs_permission_level"];
            security.User.ForcedProject = (int)dr["us_forced_project"];

            return security;
        }
    }
}