/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Data;
    using System.Diagnostics;
    using System.IO;
    using System.Text.RegularExpressions;
    using System.Web;

    public class PrintBug
    {
        private static readonly Regex ReEmail = new Regex(
            @"([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\."
            + @")|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})",
            RegexOptions.IgnoreCase
            | RegexOptions.CultureInvariant
            | RegexOptions.IgnorePatternWhitespace
            | RegexOptions.Compiled);

        // convert URL's to hyperlinks
        private static readonly Regex ReHyperlinks = new Regex(
            //@"(?<Protocol>\w+):\/\/(?<Domain>[\w.]+\/?)\S*",
            @"https?://[-A-Za-z0-9+&@#/%?=~_()|!:,.;]*[-A-Za-z0-9+&@#/%=~_()|]",
            RegexOptions.IgnoreCase
            | RegexOptions.CultureInvariant
            | RegexOptions.IgnorePatternWhitespace
            | RegexOptions.Compiled);

        public static void print_bug(HttpResponse response, DataRow dr, Security security,
            bool includeStyle,
            bool imagesInline,
            bool historyInline,
            bool internalPosts)
        {
            var bugid = Convert.ToInt32(dr["id"]);
            var stringBugid = Convert.ToString(bugid);

            if (includeStyle) // when sending emails
            {
                response.Write("\n<style>\n");

                // If this file exists, use it.

                var mapPath = (string)HttpRuntime.Cache["MapPath"];

                var cssForEmailFile = mapPath + "\\Content\\custom\\btnet_css_for_email.css";

                try
                {
                    if (File.Exists(cssForEmailFile))
                    {
                        response.WriteFile(cssForEmailFile);
                        response.Write("\n");
                    }
                    else
                    {
                        cssForEmailFile = mapPath + "Content\\btnet_base.css";
                        response.WriteFile(cssForEmailFile);
                        response.Write("\n");
                        cssForEmailFile = mapPath + "\\Content\\custom\\" + "btnet_custom.css";
                        if (File.Exists(cssForEmailFile))
                        {
                            response.WriteFile(cssForEmailFile);
                            response.Write("\n");
                        }
                    }
                }
                catch (Exception e)
                {
                    Util.WriteToLog("Exception trying to read css file for email \""
                                      + cssForEmailFile
                                      + "\":"
                                      + e.Message);
                }

                // underline links in the emails to make them more obvious
                response.Write("\na {text-decoration: underline; }");
                response.Write("\na:visited {text-decoration: underline; }");
                response.Write("\na:hover {text-decoration: underline; }");
                response.Write("\n</style>\n");
            }

            response.Write("<body style='background:white'>");
            response.Write("<b>"
                           + Util.CapitalizeFirstLetter(Util.GetSetting("SingularBugLabel", "bug"))
                           + " ID:&nbsp;<a href="
                           + Util.GetSetting("AbsoluteUrlPrefix", "http://127.0.0.1/")
                           + "EditBug.aspx?id="
                           + stringBugid
                           + ">"
                           + stringBugid
                           + "</a>");

            if (Util.GetSetting("EnableMobile", "0") == "1")
                response.Write(
                    "&nbsp;&nbsp;&nbsp;&nbsp;Mobile link:&nbsp;<a href="
                    + Util.GetSetting("AbsoluteUrlPrefix", "http://127.0.0.1/")
                    + "MBug.aspx?id="
                    + stringBugid
                    + ">"
                    + Util.GetSetting("AbsoluteUrlPrefix", "http://127.0.0.1/")
                    + "MBug.aspx?id="
                    + stringBugid
                    + "</a>");

            response.Write("<br>");

            response.Write("Short desc:&nbsp;<a href="
                           + Util.GetSetting("AbsoluteUrlPrefix", "http://127.0.0.1/")
                           + "EditBug.aspx?id="
                           + stringBugid
                           + ">"
                           + HttpUtility.HtmlEncode((string)dr["short_desc"])
                           + "</a></b><p>");

            // start of the table with the bug fields
            response.Write("\n<table border=1 cellpadding=3 cellspacing=0>");
            response.Write("\n<tr><td>Last changed by<td>"
                           + FormatUserName((string)dr["last_updated_user"], (string)dr["last_updated_fullname"])
                           + "&nbsp;");
            response.Write("\n<tr><td>Reported By<td>"
                           + FormatUserName((string)dr["reporter"], (string)dr["reporter_fullname"])
                           + "&nbsp;");
            response.Write("\n<tr><td>Reported On<td>" + Util.FormatDbDateTime(dr["reported_date"]) + "&nbsp;");

            if (security.User.TagsFieldPermissionLevel > 0)
                response.Write("\n<tr><td>Tags<td>" + dr["bg_tags"] + "&nbsp;");

            if (security.User.ProjectFieldPermissionLevel > 0)
                response.Write("\n<tr><td>Project<td>" + dr["current_project"] + "&nbsp;");

            if (security.User.OrgFieldPermissionLevel > 0)
                response.Write("\n<tr><td>Organization<td>" + dr["og_name"] + "&nbsp;");

            if (security.User.CategoryFieldPermissionLevel > 0)
                response.Write("\n<tr><td>Category<td>" + dr["category_name"] + "&nbsp;");

            if (security.User.PriorityFieldPermissionLevel > 0)
                response.Write("\n<tr><td>Priority<td>" + dr["priority_name"] + "&nbsp;");

            if (security.User.AssignedToFieldPermissionLevel > 0)
                response.Write("\n<tr><td>Assigned<td>"
                               + FormatUserName((string)dr["assigned_to_username"],
                                   (string)dr["assigned_to_fullname"])
                               + "&nbsp;");

            if (security.User.StatusFieldPermissionLevel > 0)
                response.Write("\n<tr><td>Status<td>" + dr["status_name"] + "&nbsp;");

            if (security.User.UdfFieldPermissionLevel > 0)
                if (Util.GetSetting("ShowUserDefinedBugAttribute", "1") == "1")
                    response.Write("\n<tr><td>"
                                   + Util.GetSetting("UserDefinedBugAttributeName", "YOUR ATTRIBUTE")
                                   + "<td>"
                                   + dr["udf_name"] + "&nbsp;");

            // Get custom column info  (There's an inefficiency here - we just did this
            // same call in get_bug_datarow...)

            var dsCustomCols = Util.GetCustomColumns();

            // Show custom columns

            foreach (DataRow drcc in dsCustomCols.Tables[0].Rows)
            {
                var columnName = (string)drcc["name"];

                if (security.User.DictCustomFieldPermissionLevel[columnName] == Security.PermissionNone) continue;

                response.Write("\n<tr><td>");
                response.Write(columnName);
                response.Write("<td>");

                if ((string)drcc["datatype"] == "datetime")
                {
                    var dt = dr[(string)drcc["name"]];

                    response.Write(Util.FormatDbDateTime(dt));
                }
                else
                {
                    var s = "";

                    if ((string)drcc["dropdown type"] == "users")
                    {
                        var obj = dr[(string)drcc["name"]];
                        if (obj.GetType() != typeof(DBNull))
                        {
                            var userid = Convert.ToInt32(obj);
                            if (userid != 0)
                            {
                                var sqlGetUsername = "select us_username from users where us_id = $1";
                                s = (string)DbUtil.ExecuteScalar(sqlGetUsername.Replace("$1",
                                    Convert.ToString(userid)));
                            }
                        }
                    }
                    else
                    {
                        s = Convert.ToString(dr[(string)drcc["name"]]);
                    }

                    s = HttpUtility.HtmlEncode(s);
                    s = s.Replace("\n", "<br>");
                    s = s.Replace("  ", "&nbsp; ");
                    s = s.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");
                    response.Write(s);
                }

                response.Write("&nbsp;");
            }

            // create project custom dropdowns
            if ((int)dr["project"] != 0)
            {
                var sql = @"select
					isnull(pj_enable_custom_dropdown1,0) [pj_enable_custom_dropdown1],
					isnull(pj_enable_custom_dropdown2,0) [pj_enable_custom_dropdown2],
					isnull(pj_enable_custom_dropdown3,0) [pj_enable_custom_dropdown3],
					isnull(pj_custom_dropdown_label1,'') [pj_custom_dropdown_label1],
					isnull(pj_custom_dropdown_label2,'') [pj_custom_dropdown_label2],
					isnull(pj_custom_dropdown_label3,'') [pj_custom_dropdown_label3]
					from projects where pj_id = $pj";

                sql = sql.Replace("$pj", Convert.ToString((int)dr["project"]));

                var projectDr = DbUtil.GetDataRow(sql);

                if (projectDr != null)
                    for (var i = 1; i < 4; i++)
                        if ((int)projectDr["pj_enable_custom_dropdown" + Convert.ToString(i)] == 1)
                        {
                            response.Write("\n<tr><td>");
                            response.Write(projectDr["pj_custom_dropdown_label" + Convert.ToString(i)]);
                            response.Write("<td>");
                            response.Write(dr["bg_project_custom_dropdown_value" + Convert.ToString(i)]);
                            response.Write("&nbsp;");
                        }
            }

            response.Write("\n</table><p>"); // end of the table with the bug fields

            // Relationships
            if (Util.GetSetting("EnableRelationships", "0") == "1") WriteRelationships(response, bugid);

            // Tasks
            if (Util.GetSetting("EnableTasks", "0") == "1") WriteTasks(response, bugid);

            var dsPosts = GetBugPosts(bugid, security.User.ExternalUser, historyInline);
            WritePosts(
                dsPosts,
                response,
                bugid,
                0,
                false, /* don't write links */
                imagesInline,
                historyInline,
                internalPosts,
                security.User);

            response.Write("</body>");
        }

        protected static void WriteTasks(HttpResponse response, int bugid)
        {
            var dsTasks = Util.GetAllTasks(null, bugid);

            if (dsTasks.Tables[0].Rows.Count > 0)
            {
                response.Write("<b>Tasks</b><p>");

                SortableHtmlTable.CreateNonSortableFromDataSet(
                    response, dsTasks);
            }
        }

        protected static void WriteRelationships(HttpResponse response, int bugid)
        {
            var sql = @"select bg_id [id],
				bg_short_desc [desc],
				re_type [comment],
				case
					when re_direction = 0 then ''
					when re_direction = 2 then 'child of $bg'
					else 'parent of $bg' end [parent/child]
				from bug_relationships
				inner join bugs on re_bug2 = bg_id
				where re_bug1 = $bg
				order by 1";

            sql = sql.Replace("$bg", Convert.ToString(bugid));
            var dsRelationships = DbUtil.GetDataSet(sql);

            if (dsRelationships.Tables[0].Rows.Count > 0)
            {
                response.Write("<b>Relationships</b><p><table border=1 class=datat><tr>");
                response.Write("<td class=datah valign=bottom>id</td>");
                response.Write("<td class=datah valign=bottom>desc</td>");
                response.Write("<td class=datah valign=bottom>comment</td>");
                response.Write("<td class=datah valign=bottom>parent/child</td>");

                foreach (DataRow drRelationships in dsRelationships.Tables[0].Rows)
                {
                    response.Write("<tr>");

                    response.Write("<td class=datad valign=top align=right>");
                    response.Write(Convert.ToString((int)drRelationships["id"]));

                    response.Write("<td class=datad valign=top>");
                    response.Write(Convert.ToString(drRelationships["desc"]));

                    response.Write("<td class=datad valign=top>");
                    response.Write(Convert.ToString(drRelationships["comment"]));

                    response.Write("<td class=datad valign=top>");
                    response.Write(Convert.ToString(drRelationships["parent/child"]));
                }

                response.Write("</table><p>");
            }
        }

        public static int WritePosts(
            DataSet dsPosts,
            HttpResponse response,
            int bugid,
            int permissionLevel,
            bool writeLinks,
            bool imagesInline,
            bool historyInline,
            bool internalPosts,
            User user)
        {
            if (Util.GetSetting("ForceBordersInEmails", "0") == "1")
                response.Write("\n<table id='posts_table' border=1 cellpadding=0 cellspacing=3>");
            else
                response.Write("\n<table id='posts_table' border=0 cellpadding=0 cellspacing=3>");

            var postCnt = dsPosts.Tables[0].Rows.Count;

            int bpId;
            var prevBpId = -1;

            foreach (DataRow dr in dsPosts.Tables[0].Rows)
            {
                if (!internalPosts)
                    if ((int)dr["bp_hidden_from_external_users"] == 1)
                        continue;

                bpId = (int)dr["bp_id"];

                if ((string)dr["bp_type"] == "update")
                {
                    var comment = (string)dr["bp_comment"];

                    if (user.TagsFieldPermissionLevel == Security.PermissionNone
                        && comment.StartsWith("changed tags from"))
                        continue;

                    if (user.ProjectFieldPermissionLevel == Security.PermissionNone
                        && comment.StartsWith("changed project from"))
                        continue;

                    if (user.OrgFieldPermissionLevel == Security.PermissionNone
                        && comment.StartsWith("changed organization from"))
                        continue;

                    if (user.CategoryFieldPermissionLevel == Security.PermissionNone
                        && comment.StartsWith("changed category from"))
                        continue;

                    if (user.PriorityFieldPermissionLevel == Security.PermissionNone
                        && comment.StartsWith("changed priority from"))
                        continue;

                    if (user.AssignedToFieldPermissionLevel == Security.PermissionNone
                        && comment.StartsWith("changed assigned_to from"))
                        continue;

                    if (user.StatusFieldPermissionLevel == Security.PermissionNone
                        && comment.StartsWith("changed status from"))
                        continue;

                    if (user.UdfFieldPermissionLevel == Security.PermissionNone
                        && comment.StartsWith("changed " +
                                              Util.GetSetting("UserDefinedBugAttributeName", "YOUR ATTRIBUTE") +
                                              " from"))
                        continue;

                    var bSkip = false;
                    foreach (var key in user.DictCustomFieldPermissionLevel.Keys)
                    {
                        var fieldPermissionLevel = user.DictCustomFieldPermissionLevel[key];
                        if (fieldPermissionLevel == Security.PermissionNone)
                            if (comment.StartsWith("changed " + key + " from"))
                                bSkip = true;
                    }

                    if (bSkip) continue;
                }

                if (bpId == prevBpId)
                {
                    // show another attachment
                    WriteEmailAttachment(response, bugid, dr, writeLinks, imagesInline);
                }
                else
                {
                    // show the comment and maybe an attachment
                    if (prevBpId != -1) response.Write("\n</table>"); // end the previous table

                    WritePost(response, bugid, permissionLevel, dr, bpId, writeLinks, imagesInline,
                        user.IsAdmin,
                        user.CanEditAndDeletePosts,
                        user.ExternalUser);

                    if (Convert.ToString(dr["ba_file"]) != "") // intentially "ba"
                        WriteEmailAttachment(response, bugid, dr, writeLinks, imagesInline);
                    prevBpId = bpId;
                }
            }

            if (prevBpId != -1) response.Write("\n</table>"); // end the previous table

            response.Write("\n</table>");

            return postCnt;
        }

        private static void WritePost(
            HttpResponse response,
            int bugid,
            int permissionLevel,
            DataRow dr,
            int postId,
            bool writeLinks,
            bool imagesInline,
            bool thisIsAdmin,
            bool thisCanEditAndDeletePosts,
            bool thisExternalUser)
        {
            var type = (string)dr["bp_type"];

            var stringPostId = Convert.ToString(postId);
            var stringBugId = Convert.ToString(bugid);

            if ((int)dr["seconds_ago"] < 2 && writeLinks)
                // for the animation effect
                response.Write("\n\n<tr><td class=cmt name=new_post>\n<table width=100%>\n<tr><td align=left>");
            else
                response.Write("\n\n<tr><td class=cmt>\n<table width=100%>\n<tr><td align=left>");

            /*
				Format one of the following:

				changed by
				email sent to
				email received from
				file attached by
				comment posted by

			*/

            if (type == "update")
            {
                if (writeLinks) response.Write("<img src=Content/images/database.png align=top>&nbsp;");

                // posted by
                response.Write("<span class=pst>changed by ");
                response.Write(FormatEmailUserName(
                    writeLinks,
                    bugid,
                    permissionLevel,
                    (string)dr["us_email"],
                    (string)dr["us_username"],
                    (string)dr["us_fullname"]));
            }
            else if (type == "sent")
            {
                if (writeLinks) response.Write("<img src=Content/images/email_edit.png align=top>&nbsp;");

                response.Write("<span class=pst>email <a name=" + Convert.ToString(postId) + "></a>" +
                               Convert.ToString(postId) + " sent to ");

                if (writeLinks)
                    response.Write(FormatEmailTo(
                        bugid,
                        HttpUtility.HtmlEncode((string)dr["bp_email_to"])));
                else
                    response.Write(HttpUtility.HtmlEncode((string)dr["bp_email_to"]));

                if ((string)dr["bp_email_cc"] != "")
                {
                    response.Write(", cc: ");

                    if (writeLinks)
                        response.Write(FormatEmailTo(
                            bugid,
                            HttpUtility.HtmlEncode((string)dr["bp_email_cc"])));
                    else
                        response.Write(HttpUtility.HtmlEncode((string)dr["bp_email_cc"]));

                    response.Write(", ");
                }

                response.Write(" by ");

                response.Write(FormatEmailUserName(
                    writeLinks,
                    bugid,
                    permissionLevel,
                    (string)dr["us_email"],
                    (string)dr["us_username"],
                    (string)dr["us_fullname"]));
            }
            else if (type == "received")
            {
                if (writeLinks) response.Write("<img src=Content/images/email_open.png align=top>&nbsp;");
                response.Write("<span class=pst>email <a name=" + Convert.ToString(postId) + "></a>" +
                               Convert.ToString(postId) + " received from ");
                if (writeLinks)
                    response.Write(FormatEmailFrom(
                        postId,
                        (string)dr["bp_email_from"]));
                else
                    response.Write((string)dr["bp_email_from"]);
            }
            else if (type == "file")
            {
                if ((int)dr["bp_hidden_from_external_users"] == 1)
                    response.Write("<div class=private>Internal Only!</div>");
                response.Write("<span class=pst>file <a name=" + Convert.ToString(postId) + "></a>" +
                               Convert.ToString(postId) + " attached by ");
                response.Write(FormatEmailUserName(
                    writeLinks,
                    bugid,
                    permissionLevel,
                    (string)dr["us_email"],
                    (string)dr["us_username"],
                    (string)dr["us_fullname"]));
            }
            else if (type == "comment")
            {
                if ((int)dr["bp_hidden_from_external_users"] == 1)
                    response.Write("<div class=private>Internal Only!</div>");

                if (writeLinks) response.Write("<img src=Content/images/comment.png align=top>&nbsp;");

                response.Write("<span class=pst>comment <a name=" + Convert.ToString(postId) + "></a>" +
                               Convert.ToString(postId) + " posted by ");
                response.Write(FormatEmailUserName(
                    writeLinks,
                    bugid,
                    permissionLevel,
                    (string)dr["us_email"],
                    (string)dr["us_username"],
                    (string)dr["us_fullname"]));
            }
            else
            {
                Debug.Assert(false);
            }

            // Format the date
            response.Write(" on ");
            response.Write(Util.FormatDbDateTime(dr["bp_date"]));
            response.Write(", ");
            response.Write(Util.HowLongAgo((int)dr["seconds_ago"]));
            response.Write("</span>");

            // Write the links

            if (writeLinks)
            {
                response.Write("<td align=right>&nbsp;");

                if (permissionLevel != Security.PermissionReadonly)
                    if (type == "comment" || type == "sent" || type == "received")
                    {
                        response.Write("&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;'");
                        response.Write(" href=SendEmail.aspx?quote=1&bp_id=" + stringPostId + "&reply=forward");
                        response.Write(">forward</a>");
                    }

                // format links for responding to email
                if (type == "received")
                {
                    if (thisIsAdmin
                        || thisCanEditAndDeletePosts
                        && permissionLevel == Security.PermissionAll)
                    {
                        // This doesn't just work.  Need to make changes in edit/delete pages.
                        //	Response.Write ("&nbsp;&nbsp;&nbsp;<a style='font-size: 8pt;'");
                        //	Response.Write (" href=EditComment.aspx?id="
                        //		+ string_post_id + "&bug_id=" + string_bug_id);
                        //	Response.Write (">edit</a>");

                        // This delete leaves debris around, but it's better than nothing
                        response.Write("&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;'");
                        response.Write(" href=" + VirtualPathUtility.ToAbsolute("~/Comments/Delete.aspx") + @"?id="
                                       + stringPostId + "&bug_id=" + stringBugId);
                        response.Write(">delete</a>");
                    }

                    if (permissionLevel != Security.PermissionReadonly)
                    {
                        response.Write("&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;'");
                        response.Write(" href=SendEmail.aspx?quote=1&bp_id=" + stringPostId);
                        response.Write(">reply</a>");

                        response.Write("&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;'");
                        response.Write(" href=SendEmail.aspx?quote=1&bp_id=" + stringPostId + "&reply=all");
                        response.Write(">reply all</a>");
                    }
                }
                else if (type == "file")
                {
                    if (thisIsAdmin
                        || thisCanEditAndDeletePosts
                        && permissionLevel == Security.PermissionAll)
                    {
                        response.Write("&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;'");
                        response.Write(" href=" + VirtualPathUtility.ToAbsolute("~/Attachments/Edit.aspx") + @"?id="
                                       + stringPostId + "&bug_id=" + stringBugId);
                        response.Write(">edit</a>");

                        response.Write("&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;'");
                        response.Write(" href=" + VirtualPathUtility.ToAbsolute("~/Attachments/Delete.aspx") + @"?id="
                                       + stringPostId + "&bug_id=" + stringBugId);
                        response.Write(">delete</a>");
                    }
                }
                else if (type == "comment")
                {
                    if (thisIsAdmin
                        || thisCanEditAndDeletePosts
                        && permissionLevel == Security.PermissionAll)
                    {
                        response.Write("&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;'");
                        response.Write(" href=" + VirtualPathUtility.ToAbsolute("~/Comments/Edit.aspx") + @"?id="
                                       + stringPostId + "&bug_id=" + stringBugId);
                        response.Write(">edit</a>");

                        response.Write("&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;'");
                        response.Write(" href=" + VirtualPathUtility.ToAbsolute("~/Comments/Delete.aspx") + @"?id="
                                       + stringPostId + "&bug_id=" + stringBugId);
                        response.Write(">delete</a>");
                    }
                }

                // custom bug link
                if (Util.GetSetting("CustomPostLinkLabel", "") != "")
                {
                    var customPostLink = "&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;' href="
                                           + Util.GetSetting("CustomPostLinkUrl", "")
                                           + "?postid="
                                           + stringPostId
                                           + ">"
                                           + Util.GetSetting("CustomPostLinkLabel", "")
                                           + "</a>";

                    response.Write(customPostLink);
                }
            }

            response.Write("\n</table>\n<table border=0>\n<tr><td>");
            // the text itself
            var comment = (string)dr["bp_comment"];
            var commentType = (string)dr["bp_content_type"];

            if (writeLinks)
                comment = FormatComment(postId, comment, commentType);
            else
                comment = FormatComment(0, comment, commentType);

            if (type == "file")
            {
                if (comment.Length > 0)
                {
                    response.Write(comment);
                    response.Write("<p>");
                }

                response.Write("<span class=pst>");
                if (writeLinks) response.Write("<img src=Content/images/attach.gif>");
                response.Write("attachment:&nbsp;</span><span class=cmt_text>");
                response.Write(dr["bp_file"]);
                response.Write("</span>");

                if (writeLinks)
                {
                    if ((string)dr["bp_content_type"] != "text/html" ||
                        Util.GetSetting("ShowPotentiallyDangerousHtml", "0") == "1")
                    {
                        response.Write("&nbsp;&nbsp;&nbsp;<a target=_blank style='font-size: 8pt;'");
                        response.Write(" href=" + VirtualPathUtility.ToAbsolute("~/Attachments/View.aspx") + @"?download=0&id="
                                       + stringPostId + "&bug_id=" + stringBugId);
                        response.Write(">view</a>");
                    }

                    response.Write("&nbsp;&nbsp;&nbsp;<a target=_blank style='font-size: 8pt;'");
                    response.Write(" href=" + VirtualPathUtility.ToAbsolute("~/Attachments/View.aspx") + @"?download=1&id="
                                   + stringPostId + "&bug_id=" + stringBugId);
                    response.Write(">save</a>");
                }

                response.Write("<p><span class=pst>size: ");
                response.Write(dr["bp_size"]);
                response.Write("&nbsp;&nbsp;&nbsp;content-type: ");
                response.Write(dr["bp_content_type"]);
                response.Write("</span>");
            }
            else
            {
                response.Write(comment);
            }

            // maybe show inline images
            if (type == "file")
                if (imagesInline)
                {
                    var file = Convert.ToString(dr["bp_file"]);
                    WriteFileInline(response, file, stringPostId, stringBugId, (string)dr["bp_content_type"]);
                }
        }

        private static void WriteEmailAttachment(HttpResponse response, int bugid, DataRow dr, bool writeLinks,
            bool imagesInline)
        {
            var stringPostId = Convert.ToString(dr["ba_id"]); // intentially "ba"
            var stringBugId = Convert.ToString(bugid);

            response.Write("\n<p><span class=pst>");
            if (writeLinks) response.Write("<img src=Content/images/attach.gif>");
            response.Write("attachment:&nbsp;</span>");
            response.Write(dr["ba_file"]); // intentially "ba"
            response.Write("&nbsp;&nbsp;&nbsp;&nbsp;");

            if (writeLinks)
            {
                if ((string)dr["bp_content_type"] != "text/html" ||
                    Util.GetSetting("ShowPotentiallyDangerousHtml", "0") == "1")
                {
                    response.Write("<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Attachments/View.aspx") + @"?download=0&id=");
                    response.Write(stringPostId);
                    response.Write("&bug_id=");
                    response.Write(stringBugId);
                    response.Write(">view</a>");
                }

                response.Write("&nbsp;&nbsp;&nbsp;<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Attachments/View.aspx") + @"?download=1&id=");
                response.Write(stringPostId);
                response.Write("&bug_id=");
                response.Write(stringBugId);
                response.Write(">save</a>");
            }

            if (imagesInline)
            {
                var file = Convert.ToString(dr["ba_file"]); // intentially "ba"
                WriteFileInline(response, file, stringPostId, stringBugId, (string)dr["ba_content_type"]);
            }

            response.Write("<p><span class=pst>size: ");
            response.Write(dr["ba_size"]);
            response.Write("&nbsp;&nbsp;&nbsp;content-type: ");
            response.Write(dr["ba_content_type"]);
            response.Write("</span>");
        }

        private static void WriteFileInline(
            HttpResponse response,
            string filename,
            string stringPostId,
            string stringBugId,
            string contentType)
        {
            if (contentType == "image/gif"
                || contentType == "image/jpg"
                || contentType == "image/jpeg"
                || contentType == "image/pjpeg"
                || contentType == "image/png"
                || contentType == "image/x-png"
                || contentType == "image/bmp"
                || contentType == "image/tiff")
                response.Write("<p>"
                               + "<a href=javascript:resize_image('im" + stringPostId + "',1.5)>" + "[+]</a>&nbsp;"
                               + "<a href=javascript:resize_image('im" + stringPostId + "',.6)>" + "[-]</a>"
                               + "<br><img id=im" + stringPostId
                               + " src=" + VirtualPathUtility.ToAbsolute("~/Attachments/View.aspx") + @"?download=0&id="
                               + stringPostId + "&bug_id=" + stringBugId
                               + ">");
            else if (contentType == "text/plain"
                     || contentType == "text/xml"
                     || contentType == "text/css"
                     || contentType == "text/js"
                     || contentType == "text/html" && Util.GetSetting("ShowPotentiallyDangerousHtml", "0") == "1")
                response.Write("<p>"
                               + "<a href=javascript:resize_iframe('if" + stringPostId + "',200)>" + "[+]</a>&nbsp;"
                               + "<a href=javascript:resize_iframe('if" + stringPostId + "',-200)>" + "[-]</a>"
                               + "<br><iframe id=if"
                               + stringPostId
                               + " width=780 height=200 src=" + VirtualPathUtility.ToAbsolute("~/Attachments/View.aspx") + @"?download=0&id="
                               + stringPostId + "&bug_id=" + stringBugId
                               + "></iframe>");
        }

        public static string FormatEmailUserName(
            bool writeLinks,
            int bugid,
            int permissionLevel,
            string email,
            string username,
            string fullname)
        {
            if (email != null && email != "" && writeLinks && permissionLevel != Security.PermissionReadonly)
                return "<a href="
                       + Util.GetSetting("AbsoluteUrlPrefix", "http://127.0.0.1/")
                       + "SendEmail.aspx?bg_id="
                       + Convert.ToString(bugid)
                       + "&to="
                       + email
                       + ">"
                       + FormatUserName(username, fullname)
                       + "</a>";
            return FormatUserName(username, fullname);
        }

        private static string FormatEmailTo(int bugid, string email)
        {
            return "<a href="
                   + Util.GetSetting("AbsoluteUrlPrefix", "http://127.0.0.1/")
                   + "SendEmail.aspx?bg_id=" + Convert.ToString(bugid)
                   + "&to=" + HttpUtility.UrlEncode(HttpUtility.HtmlDecode(email)) + ">"
                   + email
                   + "</a>";
        }

        private static string FormatEmailFrom(int commentId, string from)
        {
            var displayPart = "";
            var emailPart = "";
            var pos = from.IndexOf("<"); // "

            if (pos > 0)
            {
                displayPart = from.Substring(0, pos);
                emailPart = from.Substring(pos + 1, from.Length - pos - 2);
            }
            else
            {
                emailPart = from;
            }

            return displayPart
                   + " <a href="
                   + Util.GetSetting("AbsoluteUrlPrefix", "http://127.0.0.1/")
                   + "SendEmail.aspx?bp_id="
                   + Convert.ToString(commentId)
                   + ">"
                   + emailPart
                   + "</a>";
        }

        private static string FormatComment(int postId, string s1, string t1)
        {
            string s2;
            string linkMarker;

            if (t1 != "text/html")
            {
                s2 = HttpUtility.HtmlEncode(s1);

                if (postId != 0)
                {
                    // convert urls to links
                    s2 = ReHyperlinks.Replace(
                        s2,
                        ConvertToHyperLink);

                    // This code doesn't perform well if s2 is one big string, no spaces, line breaks

                    // convert email addresses to send_email links
                    s2 = ReEmail.Replace(
                        s2,
                        delegate (Match m)
                        {
                            return
                                "<a href=SendEmail.aspx?bp_id="
                                + Convert.ToString(postId)
                                + "&to="
                                + m
                                + ">"
                                + m
                                + "</a>";
                        }
                    );
                }

                s2 = s2.Replace("\n", "<br>");
                s2 = s2.Replace("  ", " &nbsp;");
                s2 = s2.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");
            }
            else
            {
                s2 = s1;
            }

            // convert references to other bugs to links
            linkMarker = Util.GetSetting("BugLinkMarker", "bugid#");
            var reLinkMarker = new Regex(linkMarker + "([0-9]+)");
            s2 = reLinkMarker.Replace(
                s2,
                ConvertBugLink);

            return "<span class=cmt_text>" + s2 + "</span>";
        }

        private static string ConvertToEmail(Match m)
        {
            // Get the matched string.
            return string.Format("<a href='mailto:{0}'>{0}</a>", m);
        }

        private static string ConvertBugLink(Match m)
        {
            return "<a href="
                   + Util.GetSetting("AbsoluteUrlPrefix", "http://127.0.0.1/")
                   + "EditBug.aspx?id="
                   + m.Groups[1]
                   + ">"
                   + m
                   + "</a>";
        }

        private static string ConvertToHyperLink(Match m)
        {
            return string.Format("<a target=_blank href='{0}'>{0}</a>", m);
        }

        private static string FormatUserName(string username, string fullname)
        {
            if (Util.GetSetting("UseFullNames", "0") == "0")
                return username;
            return fullname;
        }

        public static DataSet GetBugPosts(int bugid, bool externalUser, bool historyInline)
        {
            var sql = @"
/* GetBugPosts */
select
a.bp_bug,
a.bp_comment,
isnull(us_username,'') [us_username],
case rtrim(us_firstname)
	when null then isnull(us_lastname, '')
	when '' then isnull(us_lastname, '')
	else isnull(us_lastname + ', ' + us_firstname,'')
	end [us_fullname],
isnull(us_email,'') [us_email],
a.bp_date,
datediff(s,a.bp_date,getdate()) [seconds_ago],
a.bp_id,
a.bp_type,
isnull(a.bp_email_from,'') bp_email_from,
isnull(a.bp_email_to,'') bp_email_to,
isnull(a.bp_email_cc,'') bp_email_cc,
isnull(a.bp_file,'') bp_file,
isnull(a.bp_size,0) bp_size,
isnull(a.bp_content_type,'') bp_content_type,
a.bp_hidden_from_external_users,
isnull(ba.bp_file,'') ba_file,  -- intentionally ba
isnull(ba.bp_id,'') ba_id, -- intentionally ba
isnull(ba.bp_size,'') ba_size,  -- intentionally ba
isnull(ba.bp_content_type,'') ba_content_type -- intentionally ba
from bug_posts a
left outer join users on us_id = a.bp_user
left outer join bug_posts ba on ba.bp_parent = a.bp_id and ba.bp_bug = a.bp_bug
where a.bp_bug = $id
and a.bp_parent is null";

            if (!historyInline) sql += "\n and a.bp_type <> 'update'";

            if (externalUser) sql += "\n and a.bp_hidden_from_external_users = 0";

            sql += "\n order by a.bp_id ";
            sql += Util.GetSetting("CommentSortOrder", "desc");
            sql += ", ba.bp_parent, ba.bp_id";

            sql = sql.Replace("$id", Convert.ToString(bugid));

            return DbUtil.GetDataSet(sql);
        }
    } // end PrintBug

    public class WritePostResult
    {
        public int PostCnt;
        public string PostString;
        public string RelatedBugs;
    }
} // end namespace