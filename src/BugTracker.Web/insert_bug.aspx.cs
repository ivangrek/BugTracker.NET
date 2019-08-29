/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.IO;
    using System.Web;
    using System.Web.UI;
    using anmar.SharpMimeTools;
    using Core;

    public partial class insert_bug : Page
    {
        public void Page_Load(object sender, EventArgs e)
        {
            Util.set_context(HttpContext.Current);
            Util.do_not_cache(Response);

            var username = Request["username"];
            var password = Request["password"];
            var projectid_string = Request["projectid"];
            var comment = Request["comment"];
            var from_addr = Request["from"];
            var cc = "";
            var message = Request["message"];
            var attachment_as_base64 = Request["attachment"];
            var attachment_content_type = Request["attachment_content_type"];
            var attachment_filename = Request["attachment_filename"];
            var attachment_desc = Request["attachment_desc"];
            var bugid_string = Request["bugid"];
            var short_desc = Request["short_desc"];

            // this could also be the email subject
            if (short_desc == null)
                short_desc = "";
            else if (short_desc.Length > 200) short_desc = short_desc.Substring(0, 200);

            SharpMimeMessage mime_message = null;

            if (message != null && message.Length > 0)
            {
                mime_message = MyMime.get_sharp_mime_message(message);

                comment = MyMime.get_comment(mime_message);

                var headers = MyMime.get_headers_for_comment(mime_message);
                if (headers != "") comment = headers + "\n" + comment;

                from_addr = MyMime.get_from_addr(mime_message);
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

            var authenticated = Authenticate.check_password(username, password);

            if (!authenticated)
            {
                Response.AddHeader("BTNET", "ERROR: invalid username or password");
                Response.Write("ERROR: invalid username or password");
                Response.End();
            }

            var security = MyMime.get_synthesized_security(mime_message, from_addr, username);

            var projectid = 0;
            if (Util.is_int(projectid_string)) projectid = Convert.ToInt32(projectid_string);

            var bugid = 0;

            if (Util.is_int(bugid_string)) bugid = Convert.ToInt32(bugid_string);

            // Even though btnet_service.exe has already parsed out the bugid,
            // we can do a better job here with SharpMimeTools.dll
            var subject = "";

            if (mime_message != null)
            {
                subject = MyMime.get_subject(mime_message);

                if (subject != "[No Subject]") bugid = MyMime.get_bugid_from_subject(ref subject);

                cc = MyMime.get_cc(mime_message);
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

                if (Convert.ToInt32(DbUtil.execute_scalar(sql)) == 0) bugid = 0;
            }

            // Either insert a new bug or append a commment to existing bug
            // based on presence, absence of bugid
            if (bugid == 0)
            {
                // insert a new bug

                if (mime_message != null)
                {
                    // in case somebody is replying to a bug that has been deleted or merged
                    subject = subject.Replace(Util.get_setting("TrackingIdString", "DO NOT EDIT THIS:"), "PREVIOUS:");

                    short_desc = subject;
                    if (short_desc.Length > 200) short_desc = short_desc.Substring(0, 200);
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

                var defaults = Bug.get_bug_defaults();

                // If you didn't set these from the query string, we'll give them default values
                if (projectid == 0) projectid = (int) defaults["pj"];
                if (orgid == 0) orgid = security.user.org;
                if (categoryid == 0) categoryid = (int) defaults["ct"];
                if (priorityid == 0) priorityid = (int) defaults["pr"];
                if (statusid == 0) statusid = (int) defaults["st"];
                if (udfid == 0) udfid = (int) defaults["udf"];

                // but forced project always wins
                if (security.user.forced_project != 0) projectid = security.user.forced_project;

                var new_ids = Bug.insert_bug(
                    short_desc,
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
                    from_addr,
                    cc,
                    "text/plain",
                    false, // internal only
                    null, // custom columns
                    false); // suppress notifications for now - wait till after the attachments

                if (mime_message != null)
                {
                    MyMime.add_attachments(mime_message, new_ids.bugid, new_ids.postid, security);

                    MyPop3.auto_reply(new_ids.bugid, from_addr, short_desc, projectid);
                }
                else if (attachment_as_base64 != null && attachment_as_base64.Length > 0)
                {
                    if (attachment_desc == null) attachment_desc = "";
                    if (attachment_content_type == null) attachment_content_type = "";
                    if (attachment_filename == null) attachment_filename = "";

                    var byte_array = Convert.FromBase64String(attachment_as_base64);
                    Stream stream = new MemoryStream(byte_array);

                    Bug.insert_post_attachment(
                        security,
                        new_ids.bugid,
                        stream,
                        byte_array.Length,
                        attachment_filename,
                        attachment_desc,
                        attachment_content_type,
                        -1, // parent
                        false, // internal_only
                        false); // don't send notification yet
                }

                // your customizations
                Bug.apply_post_insert_rules(new_ids.bugid);

                Bug.send_notifications(Bug.INSERT, new_ids.bugid, security);
                WhatsNew.add_news(new_ids.bugid, short_desc, "added", security);

                Response.AddHeader("BTNET", "OK:" + Convert.ToString(new_ids.bugid));
                Response.Write("OK:" + Convert.ToString(new_ids.bugid));
                Response.End();
            }
            else // update existing bug
            {
                var StatusResultingFromIncomingEmail = Util.get_setting("StatusResultingFromIncomingEmail", "0");

                sql = "";

                if (StatusResultingFromIncomingEmail != "0")
                {
                    sql = @"update bugs
				set bg_status = $st
				where bg_id = $bg
				";

                    sql = sql.Replace("$st", StatusResultingFromIncomingEmail);
                }

                sql += "select bg_short_desc from bugs where bg_id = $bg";

                sql = sql.Replace("$bg", Convert.ToString(bugid));
                var dr2 = DbUtil.get_datarow(sql);

                // Add a comment to existing bug.
                var postid = Bug.insert_comment(
                    bugid,
                    security.user.usid, // (int) dr["us_id"],
                    comment,
                    comment,
                    from_addr,
                    cc,
                    "text/plain",
                    false); // internal only

                if (mime_message != null)
                {
                    MyMime.add_attachments(mime_message, bugid, postid, security);
                }
                else if (attachment_as_base64 != null && attachment_as_base64.Length > 0)
                {
                    if (attachment_desc == null) attachment_desc = "";
                    if (attachment_content_type == null) attachment_content_type = "";
                    if (attachment_filename == null) attachment_filename = "";

                    var byte_array = Convert.FromBase64String(attachment_as_base64);
                    Stream stream = new MemoryStream(byte_array);

                    Bug.insert_post_attachment(
                        security,
                        bugid,
                        stream,
                        byte_array.Length,
                        attachment_filename,
                        attachment_desc,
                        attachment_content_type,
                        -1, // parent
                        false, // internal_only
                        false); // don't send notification yet
                }

                Bug.send_notifications(Bug.UPDATE, bugid, security);
                WhatsNew.add_news(bugid, (string) dr2["bg_short_desc"], "updated", security);

                Response.AddHeader("BTNET", "OK:" + Convert.ToString(bugid));
                Response.Write("OK:" + Convert.ToString(bugid));

                Response.End();
            }
        }
    }
}