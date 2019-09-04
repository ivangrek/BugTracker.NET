/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Bugs
{
    using System;
    using System.IO;
    using System.Web;
    using System.Web.UI;
    using anmar.SharpMimeTools;
    using Core;

    public partial class Insert : Page
    {
        public void Page_Load(object sender, EventArgs e)
        {
            Util.SetContext(HttpContext.Current);
            Util.DoNotCache(Response);

            var username = Request["username"];
            var password = Request["password"];
            var projectidString = Request["projectid"];
            var comment = Request["comment"];
            var fromAddr = Request["from"];
            var cc = "";
            var message = Request["message"];
            var attachmentAsBase64 = Request["attachment"];
            var attachmentContentType = Request["attachment_content_type"];
            var attachmentFilename = Request["attachment_filename"];
            var attachmentDesc = Request["attachment_desc"];
            var bugidString = Request["bugid"];
            var shortDesc = Request["short_desc"];

            // this could also be the email subject
            if (shortDesc == null)
                shortDesc = "";
            else if (shortDesc.Length > 200) shortDesc = shortDesc.Substring(0, 200);

            SharpMimeMessage mimeMessage = null;

            if (message != null && message.Length > 0)
            {
                mimeMessage = MyMime.GetSharpMimeMessage(message);

                comment = MyMime.GetComment(mimeMessage);

                var headers = MyMime.GetHeadersForComment(mimeMessage);
                if (headers != "") comment = headers + "\n" + comment;

                fromAddr = MyMime.GetFromAddr(mimeMessage);
            }
            else
            {
                if (comment == null) comment = "";
            }

            if (username == null
                || username == "")
            {
                Response.AddHeader("BTNET", "ERROR: username required");
                Response.Write("ERROR: username required");
                Response.End();
            }

            if (password == null
                || password == "")
            {
                Response.AddHeader("BTNET", "ERROR: password required");
                Response.Write("ERROR: password required");
                Response.End();
            }

            // authenticate user

            var authenticated = Authenticate.CheckPassword(username, password);

            if (!authenticated)
            {
                Response.AddHeader("BTNET", "ERROR: invalid username or password");
                Response.Write("ERROR: invalid username or password");
                Response.End();
            }

            var security = MyMime.GetSynthesizedSecurity(mimeMessage, fromAddr, username);

            var projectid = 0;
            if (Util.IsInt(projectidString)) projectid = Convert.ToInt32(projectidString);

            var bugid = 0;

            if (Util.IsInt(bugidString)) bugid = Convert.ToInt32(bugidString);

            // Even though btnet_service.exe has already parsed out the bugid,
            // we can do a better job here with SharpMimeTools.dll
            var subject = "";

            if (mimeMessage != null)
            {
                subject = MyMime.GetSubject(mimeMessage);

                if (subject != "[No Subject]") bugid = MyMime.GetBugidFromSubject(ref subject);

                cc = MyMime.GetCc(mimeMessage);
            }

            var sql = "";

            if (bugid != 0)
            {
                // Check if the bug is still in the database
                // No comment can be added to merged or deleted bugids
                // In this case a new bug is created, this to prevent possible loss of information

                sql = @"select count(bg_id)
			from bugs
			where bg_id = $id";

                sql = sql.Replace("$id", Convert.ToString(bugid));

                if (Convert.ToInt32(DbUtil.ExecuteScalar(sql)) == 0) bugid = 0;
            }

            // Either insert a new bug or append a commment to existing bug
            // based on presence, absence of bugid
            if (bugid == 0)
            {
                // insert a new bug

                if (mimeMessage != null)
                {
                    // in case somebody is replying to a bug that has been deleted or merged
                    subject = subject.Replace(Util.GetSetting("TrackingIdString", "DO NOT EDIT THIS:"), "PREVIOUS:");

                    shortDesc = subject;
                    if (shortDesc.Length > 200) shortDesc = shortDesc.Substring(0, 200);
                }

                var orgid = 0;
                var categoryid = 0;
                var priorityid = 0;
                var assignedid = 0;
                var statusid = 0;
                var udfid = 0;

                // You can control some more things from the query string
                if (Request["$ORGANIZATION$"] != null && Request["$ORGANIZATION$"] != "")
                    orgid = Convert.ToInt32(Request["$ORGANIZATION$"]);
                if (Request["$CATEGORY$"] != null && Request["$CATEGORY$"] != "")
                    categoryid = Convert.ToInt32(Request["$CATEGORY$"]);
                if (Request["$PROJECT$"] != null && Request["$PROJECT$"] != "")
                    projectid = Convert.ToInt32(Request["$PROJECT$"]);
                if (Request["$PRIORITY$"] != null && Request["$PRIORITY$"] != "")
                    priorityid = Convert.ToInt32(Request["$PRIORITY$"]);
                if (Request["$ASSIGNEDTO$"] != null && Request["$ASSIGNEDTO$"] != "")
                    assignedid = Convert.ToInt32(Request["$ASSIGNEDTO$"]);
                if (Request["$STATUS$"] != null && Request["$STATUS$"] != "")
                    statusid = Convert.ToInt32(Request["$STATUS$"]);
                if (Request["$UDF$"] != null && Request["$UDF$"] != "") udfid = Convert.ToInt32(Request["$UDF$"]);

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
                    "", // tags
                    projectid,
                    orgid,
                    categoryid,
                    priorityid,
                    statusid,
                    assignedid,
                    udfid,
                    "", "", "", // project specific dropdown values
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
                    if (attachmentDesc == null) attachmentDesc = "";
                    if (attachmentContentType == null) attachmentContentType = "";
                    if (attachmentFilename == null) attachmentFilename = "";

                    var byteArray = Convert.FromBase64String(attachmentAsBase64);
                    Stream stream = new MemoryStream(byteArray);

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
                Response.Write("OK:" + Convert.ToString(newIds.Bugid));
                Response.End();
            }
            else // update existing bug
            {
                var statusResultingFromIncomingEmail = Util.GetSetting("StatusResultingFromIncomingEmail", "0");

                sql = "";

                if (statusResultingFromIncomingEmail != "0")
                {
                    sql = @"update bugs
				set bg_status = $st
				where bg_id = $bg
				";

                    sql = sql.Replace("$st", statusResultingFromIncomingEmail);
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
                    if (attachmentDesc == null) attachmentDesc = "";
                    if (attachmentContentType == null) attachmentContentType = "";
                    if (attachmentFilename == null) attachmentFilename = "";

                    var byteArray = Convert.FromBase64String(attachmentAsBase64);
                    Stream stream = new MemoryStream(byteArray);

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

                Bug.SendNotifications(Bug.Update, bugid, security);
                WhatsNew.AddNews(bugid, (string)dr2["bg_short_desc"], "updated", security);

                Response.AddHeader("BTNET", "OK:" + Convert.ToString(bugid));
                Response.Write("OK:" + Convert.ToString(bugid));

                Response.End();
            }
        }
    }
}