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
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;

    public static class PrintBug
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

        public static string PrintBugNew(DataRow dr, ISecurity security,
            bool includeStyle,
            bool imagesInline,
            bool historyInline,
            bool internalPosts)
        {
            var stringBuilder = new StringBuilder();
            IApplicationSettings applicationSettings = new ApplicationSettings();

            var bugid = Convert.ToInt32(dr["id"]);
            var stringBugid = Convert.ToString(bugid);

            if (includeStyle) // when sending emails
            {
                stringBuilder.Append("\n<style>\n");

                // If this file exists, use it.

                var mapPath = (string)HttpRuntime.Cache["MapPath"];

                var cssForEmailFile = mapPath + "\\Content\\custom\\btnet_css_for_email.css";

                try
                {
                    if (File.Exists(cssForEmailFile))
                    {
                        stringBuilder.Append(cssForEmailFile);
                        stringBuilder.Append("\n");
                    }
                    else
                    {
                        cssForEmailFile = mapPath + "Content\\btnet_base.css";
                        stringBuilder.Append(cssForEmailFile);
                        stringBuilder.Append("\n");
                        cssForEmailFile = mapPath + "\\Content\\custom\\" + "btnet_custom.css";

                        if (File.Exists(cssForEmailFile))
                        {
                            stringBuilder.Append(cssForEmailFile);
                            stringBuilder.Append("\n");
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
                stringBuilder.Append("\na {text-decoration: underline; }");
                stringBuilder.Append("\na:visited {text-decoration: underline; }");
                stringBuilder.Append("\na:hover {text-decoration: underline; }");
                stringBuilder.Append("\n</style>\n");
            }

            stringBuilder.Append("<body style='background:white'>");
            stringBuilder.Append("<b>"
                           + Util.CapitalizeFirstLetter(applicationSettings.SingularBugLabel)
                           + " ID:&nbsp;<a href="
                           + applicationSettings.AbsoluteUrlPrefix
                           + VirtualPathUtility.ToAbsolute("~/Bugs/Edit.aspx?id=")
                           + stringBugid
                           + ">"
                           + stringBugid
                           + "</a>");

            if (applicationSettings.EnableMobile)
                stringBuilder.Append(
                    "&nbsp;&nbsp;&nbsp;&nbsp;Mobile link:&nbsp;<a href="
                    + applicationSettings.AbsoluteUrlPrefix
                    + VirtualPathUtility.ToAbsolute("~/Bugs/MobileEdit.aspx?id=")
                    + stringBugid
                    + ">"
                    + applicationSettings.AbsoluteUrlPrefix
                    + VirtualPathUtility.ToAbsolute("~/Bugs/MobileEdit.aspx?id=")
                    + stringBugid
                    + "</a>");

            stringBuilder.Append("<br>");

            stringBuilder.Append("Short desc:&nbsp;<a href="
                           + applicationSettings.AbsoluteUrlPrefix
                           + VirtualPathUtility.ToAbsolute("~/Bugs/Edit.aspx?id=")
                           + stringBugid
                           + ">"
                           + HttpUtility.HtmlEncode((string)dr["short_desc"])
                           + "</a></b><p>");

            // start of the table with the bug fields
            stringBuilder.Append("\n<table border=1 cellpadding=3 cellspacing=0>");
            stringBuilder.Append("\n<tr><td>Last changed by<td>"
                           + FormatUserName((string)dr["last_updated_user"], (string)dr["last_updated_fullname"])
                           + "&nbsp;");
            stringBuilder.Append("\n<tr><td>Reported By<td>"
                           + FormatUserName((string)dr["reporter"], (string)dr["reporter_fullname"])
                           + "&nbsp;");
            stringBuilder.Append("\n<tr><td>Reported On<td>" + Util.FormatDbDateTime(dr["reported_date"]) + "&nbsp;");

            if (security.User.TagsFieldPermissionLevel > 0)
            {
                stringBuilder.Append("\n<tr><td>Tags<td>" + dr["bg_tags"] + "&nbsp;");
            }

            if (security.User.ProjectFieldPermissionLevel > 0)
            {
                stringBuilder.Append("\n<tr><td>Project<td>" + dr["current_project"] + "&nbsp;");
            }

            if (security.User.OrgFieldPermissionLevel > 0)
            {
                stringBuilder.Append("\n<tr><td>Organization<td>" + dr["og_name"] + "&nbsp;");
            }

            if (security.User.CategoryFieldPermissionLevel > 0)
            {
                stringBuilder.Append("\n<tr><td>Category<td>" + dr["category_name"] + "&nbsp;");
            }

            if (security.User.PriorityFieldPermissionLevel > 0)
            {
                stringBuilder.Append("\n<tr><td>Priority<td>" + dr["priority_name"] + "&nbsp;");
            }

            if (security.User.AssignedToFieldPermissionLevel > 0)
            {
                stringBuilder.Append("\n<tr><td>Assigned<td>"
                               + FormatUserName((string)dr["assigned_to_username"],
                                   (string)dr["assigned_to_fullname"])
                               + "&nbsp;");
            }

            if (security.User.StatusFieldPermissionLevel > 0)
            {
                stringBuilder.Append("\n<tr><td>Status<td>" + dr["status_name"] + "&nbsp;");
            }

            if (security.User.UdfFieldPermissionLevel > 0)
                if (applicationSettings.ShowUserDefinedBugAttribute)
                    stringBuilder.Append("\n<tr><td>"
                                   + applicationSettings.UserDefinedBugAttributeName
                                   + "<td>"
                                   + dr["udf_name"] + "&nbsp;");

            // Get custom column info  (There's an inefficiency here - we just did this
            // same call in get_bug_datarow...)

            var dsCustomCols = Util.GetCustomColumns();

            // Show custom columns

            foreach (DataRow drcc in dsCustomCols.Tables[0].Rows)
            {
                var columnName = (string)drcc["name"];

                if (security.User.DictCustomFieldPermissionLevel[columnName] == SecurityPermissionLevel.PermissionNone) continue;

                stringBuilder.Append("\n<tr><td>");
                stringBuilder.Append(columnName);
                stringBuilder.Append("<td>");

                if ((string)drcc["datatype"] == "datetime")
                {
                    var dt = dr[(string)drcc["name"]];

                    stringBuilder.Append(Util.FormatDbDateTime(dt));
                }
                else
                {
                    var s = string.Empty;

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
                    stringBuilder.Append(s);
                }

                stringBuilder.Append("&nbsp;");
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
                            stringBuilder.Append("\n<tr><td>");
                            stringBuilder.Append(projectDr["pj_custom_dropdown_label" + Convert.ToString(i)]);
                            stringBuilder.Append("<td>");
                            stringBuilder.Append(dr["bg_project_custom_dropdown_value" + Convert.ToString(i)]);
                            stringBuilder.Append("&nbsp;");
                        }
            }

            stringBuilder.Append("\n</table><p>"); // end of the table with the bug fields

            // Relationships
            if (applicationSettings.EnableRelationships)
            {
                var html2 = WriteRelationships(bugid);

                stringBuilder.Append(html2);
            }

            // Tasks
            if (applicationSettings.EnableTasks)
            {
                var html2 =  WriteTasks(bugid);

                stringBuilder.Append(html2);
            }

            var dsPosts = GetBugPosts(bugid, security.User.ExternalUser, historyInline);

            var (_, html) = WritePosts(
                dsPosts,
                bugid,
                0,
                false, /* don't write links */
                imagesInline,
                historyInline,
                internalPosts,
                security.User);

            stringBuilder.Append(html);
            stringBuilder.Append("</body>");

            return stringBuilder.ToString();
        }

        public static (int, string) WritePosts(
            DataSet dsPosts,
            int bugid,
            SecurityPermissionLevel permissionLevel,
            bool writeLinks,
            bool imagesInline,
            bool historyInline,
            bool internalPosts,
            User user)
        {
            var stringBuilder = new StringBuilder();
            IApplicationSettings applicationSettings = new ApplicationSettings();

            if (applicationSettings.ForceBordersInEmails)
            {
                stringBuilder.Append("\n<table id='posts_table' border=1 cellpadding=0 cellspacing=3>");
            }
            else
            {
                stringBuilder.Append("\n<table id='posts_table' border=0 cellpadding=0 cellspacing=3>");
            }

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

                    if (user.TagsFieldPermissionLevel == SecurityPermissionLevel.PermissionNone
                        && comment.StartsWith("changed tags from"))
                        continue;

                    if (user.ProjectFieldPermissionLevel == SecurityPermissionLevel.PermissionNone
                        && comment.StartsWith("changed project from"))
                        continue;

                    if (user.OrgFieldPermissionLevel == SecurityPermissionLevel.PermissionNone
                        && comment.StartsWith("changed organization from"))
                        continue;

                    if (user.CategoryFieldPermissionLevel == SecurityPermissionLevel.PermissionNone
                        && comment.StartsWith("changed category from"))
                        continue;

                    if (user.PriorityFieldPermissionLevel == SecurityPermissionLevel.PermissionNone
                        && comment.StartsWith("changed priority from"))
                        continue;

                    if (user.AssignedToFieldPermissionLevel == SecurityPermissionLevel.PermissionNone
                        && comment.StartsWith("changed assigned_to from"))
                        continue;

                    if (user.StatusFieldPermissionLevel == SecurityPermissionLevel.PermissionNone
                        && comment.StartsWith("changed status from"))
                        continue;

                    if (user.UdfFieldPermissionLevel == SecurityPermissionLevel.PermissionNone
                        && comment.StartsWith("changed " +
                                              applicationSettings.UserDefinedBugAttributeName +
                                              " from"))
                        continue;

                    var bSkip = false;
                    foreach (var key in user.DictCustomFieldPermissionLevel.Keys)
                    {
                        var fieldPermissionLevel = user.DictCustomFieldPermissionLevel[key];
                        if (fieldPermissionLevel == SecurityPermissionLevel.PermissionNone)
                            if (comment.StartsWith("changed " + key + " from"))
                                bSkip = true;
                    }

                    if (bSkip) continue;
                }

                if (bpId == prevBpId)
                {
                    // show another attachment
                    var html = WriteEmailAttachment(bugid, dr, writeLinks, imagesInline);

                    stringBuilder.Append(html);
                }
                else
                {
                    // show the comment and maybe an attachment
                    if (prevBpId != -1) stringBuilder.Append("\n</table>"); // end the previous table

                    var html = WritePost(bugid, permissionLevel, dr, bpId, writeLinks, imagesInline,
                        user.IsAdmin,
                        user.CanEditAndDeletePosts,
                        user.ExternalUser);

                    stringBuilder.Append(html);

                    if (!string.IsNullOrEmpty(Convert.ToString(dr["ba_file"]))) // intentially "ba"
                    {
                        html = WriteEmailAttachment(bugid, dr, writeLinks, imagesInline);

                        stringBuilder.Append(html);
                    }

                    prevBpId = bpId;
                }
            }

            if (prevBpId != -1)
            {
                stringBuilder.Append("\n</table>"); // end the previous table
            }

            stringBuilder.Append("\n</table>");

            return (postCnt, stringBuilder.ToString());
        }

        private static string WriteTasks(int bugid)
        {
            var stringBuilder = new StringBuilder();
            var dsTasks = Util.GetAllTasks(null, bugid);

            if (dsTasks.Tables[0].Rows.Count > 0)
            {
                stringBuilder.Append("<b>Tasks</b><p>");

                // TODO SortableHtmlTable.CreateNonSortableFromDataSet(response, dsTasks);
            }

            return stringBuilder.ToString();
        }

        private static string WriteRelationships(int bugid)
        {
            var stringBuilder = new StringBuilder();
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
                stringBuilder.Append("<b>Relationships</b><p><table border=1 class=datat><tr>");
                stringBuilder.Append("<td class=datah valign=bottom>id</td>");
                stringBuilder.Append("<td class=datah valign=bottom>desc</td>");
                stringBuilder.Append("<td class=datah valign=bottom>comment</td>");
                stringBuilder.Append("<td class=datah valign=bottom>parent/child</td>");

                foreach (DataRow drRelationships in dsRelationships.Tables[0].Rows)
                {
                    stringBuilder.Append("<tr>");

                    stringBuilder.Append("<td class=datad valign=top align=right>");
                    stringBuilder.Append(Convert.ToString((int)drRelationships["id"]));

                    stringBuilder.Append("<td class=datad valign=top>");
                    stringBuilder.Append(Convert.ToString(drRelationships["desc"]));

                    stringBuilder.Append("<td class=datad valign=top>");
                    stringBuilder.Append(Convert.ToString(drRelationships["comment"]));

                    stringBuilder.Append("<td class=datad valign=top>");
                    stringBuilder.Append(Convert.ToString(drRelationships["parent/child"]));
                }

                stringBuilder.Append("</table><p>");
            }

            return stringBuilder.ToString();
        }

        private static string WritePost(
            int bugid,
            SecurityPermissionLevel permissionLevel,
            DataRow dr,
            int postId,
            bool writeLinks,
            bool imagesInline,
            bool thisIsAdmin,
            bool thisCanEditAndDeletePosts,
            bool thisExternalUser)
        {
            var stringBuilder = new StringBuilder();
            var type = (string)dr["bp_type"];

            var stringPostId = Convert.ToString(postId);
            var stringBugId = Convert.ToString(bugid);

            if ((int)dr["seconds_ago"] < 2 && writeLinks)
                // for the animation effect
                stringBuilder.Append("\n\n<tr><td class=cmt name=new_post>\n<table width=100%>\n<tr><td align=left>");
            else
                stringBuilder.Append("\n\n<tr><td class=cmt>\n<table width=100%>\n<tr><td align=left>");

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
                if (writeLinks) stringBuilder.Append("<img src='/Content/images/database.png' align=top>&nbsp;");

                // posted by
                stringBuilder.Append("<span class=pst>changed by ");
                stringBuilder.Append(FormatEmailUserName(
                    writeLinks,
                    bugid,
                    permissionLevel,
                    (string)dr["us_email"],
                    (string)dr["us_username"],
                    (string)dr["us_fullname"]));
            }
            else if (type == "sent")
            {
                if (writeLinks) stringBuilder.Append("<img src='/Content/images/email_edit.png' align=top>&nbsp;");

                stringBuilder.Append("<span class=pst>email <a name=" + Convert.ToString(postId) + "></a>" +
                               Convert.ToString(postId) + " sent to ");

                if (writeLinks)
                    stringBuilder.Append(FormatEmailTo(
                        bugid,
                        HttpUtility.HtmlEncode((string)dr["bp_email_to"])));
                else
                    stringBuilder.Append(HttpUtility.HtmlEncode((string)dr["bp_email_to"]));

                if (!string.IsNullOrEmpty((string)dr["bp_email_cc"]))
                {
                    stringBuilder.Append(", cc: ");

                    if (writeLinks)
                        stringBuilder.Append(FormatEmailTo(
                            bugid,
                            HttpUtility.HtmlEncode((string)dr["bp_email_cc"])));
                    else
                        stringBuilder.Append(HttpUtility.HtmlEncode((string)dr["bp_email_cc"]));

                    stringBuilder.Append(", ");
                }

                stringBuilder.Append(" by ");

                stringBuilder.Append(FormatEmailUserName(
                    writeLinks,
                    bugid,
                    permissionLevel,
                    (string)dr["us_email"],
                    (string)dr["us_username"],
                    (string)dr["us_fullname"]));
            }
            else if (type == "received")
            {
                if (writeLinks) stringBuilder.Append("<img src='/Content/images/email_open.png' align=top>&nbsp;");
                stringBuilder.Append("<span class=pst>email <a name=" + Convert.ToString(postId) + "></a>" +
                               Convert.ToString(postId) + " received from ");
                if (writeLinks)
                    stringBuilder.Append(FormatEmailFrom(
                        postId,
                        (string)dr["bp_email_from"]));
                else
                    stringBuilder.Append((string)dr["bp_email_from"]);
            }
            else if (type == "file")
            {
                if ((int)dr["bp_hidden_from_external_users"] == 1)
                    stringBuilder.Append("<div class=private>Internal Only!</div>");
                stringBuilder.Append("<span class=pst>file <a name=" + Convert.ToString(postId) + "></a>" +
                               Convert.ToString(postId) + " attached by ");
                stringBuilder.Append(FormatEmailUserName(
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
                    stringBuilder.Append("<div class=private>Internal Only!</div>");

                if (writeLinks) stringBuilder.Append("<img src=" + VirtualPathUtility.ToAbsolute("~/Content/images/comment.png") + " align=top>&nbsp;");

                stringBuilder.Append("<span class=pst>comment <a name=" + Convert.ToString(postId) + "></a>" +
                               Convert.ToString(postId) + " posted by ");
                stringBuilder.Append(FormatEmailUserName(
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
            stringBuilder.Append(" on ");
            stringBuilder.Append(Util.FormatDbDateTime(dr["bp_date"]));
            stringBuilder.Append(", ");
            stringBuilder.Append(Util.HowLongAgo((int)dr["seconds_ago"]));
            stringBuilder.Append("</span>");

            // Write the links
            IApplicationSettings applicationSettings = new ApplicationSettings();

            if (writeLinks)
            {
                stringBuilder.Append("<td align=right>&nbsp;");

                if (permissionLevel != SecurityPermissionLevel.PermissionReadonly)
                    if (type == "comment" || type == "sent" || type == "received")
                    {
                        stringBuilder.Append("&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;'");
                        stringBuilder.Append(" href=" + VirtualPathUtility.ToAbsolute("~/SendEmail.aspx") + @"?quote=1&bp_id=" + stringPostId + "&reply=forward");
                        stringBuilder.Append(">forward</a>");
                    }

                // format links for responding to email
                if (type == "received")
                {
                    if (thisIsAdmin
                        || thisCanEditAndDeletePosts
                        && permissionLevel == SecurityPermissionLevel.PermissionAll)
                    {
                        // This doesn't just work.  Need to make changes in edit/delete pages.
                        //	Response.Write ("&nbsp;&nbsp;&nbsp;<a style='font-size: 8pt;'");
                        //	Response.Write (" href=EditComment.aspx?id="
                        //		+ string_post_id + "&bug_id=" + string_bug_id);
                        //	Response.Write (">edit</a>");

                        // This delete leaves debris around, but it's better than nothing
                        stringBuilder.Append("&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;'");
                        stringBuilder.Append(" href='" + VirtualPathUtility.ToAbsolute($"~/Comment/Delete?id={stringPostId}&bugId={stringBugId}"));
                        stringBuilder.Append("'>delete</a>");
                    }

                    if (permissionLevel != SecurityPermissionLevel.PermissionReadonly)
                    {
                        stringBuilder.Append("&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;'");
                        stringBuilder.Append(" href=" + VirtualPathUtility.ToAbsolute("~/SendEmail.aspx") + @"?quote=1&bp_id=" + stringPostId);
                        stringBuilder.Append(">reply</a>");

                        stringBuilder.Append("&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;'");
                        stringBuilder.Append(" href=" + VirtualPathUtility.ToAbsolute("~/SendEmail.aspx") + @"?quote=1&bp_id=" + stringPostId + "&reply=all");
                        stringBuilder.Append(">reply all</a>");
                    }
                }
                else if (type == "file")
                {
                    if (thisIsAdmin
                        || thisCanEditAndDeletePosts
                        && permissionLevel == SecurityPermissionLevel.PermissionAll)
                    {
                        stringBuilder.Append("&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;'");
                        stringBuilder.Append(" href='" + VirtualPathUtility.ToAbsolute($"~/Attachment/Update?id={stringPostId}&bugId={stringBugId}"));
                        stringBuilder.Append("'>edit</a>");

                        stringBuilder.Append("&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;'");
                        stringBuilder.Append(" href='" + VirtualPathUtility.ToAbsolute($"~/Attachment/Delete?id={stringPostId}&bugId={stringBugId}"));
                        stringBuilder.Append("'>delete</a>");
                    }
                }
                else if (type == "comment")
                {
                    if (thisIsAdmin
                        || thisCanEditAndDeletePosts
                        && permissionLevel == SecurityPermissionLevel.PermissionAll)
                    {
                        stringBuilder.Append("&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;'");
                        stringBuilder.Append(" href='" + VirtualPathUtility.ToAbsolute($"~/Comment/Update?id={stringPostId}&bugId={stringBugId}"));
                        stringBuilder.Append("'>edit</a>");

                        stringBuilder.Append("&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;'");
                        stringBuilder.Append(" href='" + VirtualPathUtility.ToAbsolute($"~/Comment/Delete?id={stringPostId}&bugId={stringBugId}"));
                        stringBuilder.Append("'>delete</a>");
                    }
                }

                // custom bug link
                if (!string.IsNullOrEmpty(applicationSettings.CustomPostLinkLabel))
                {
                    var customPostLink = "&nbsp;&nbsp;&nbsp;<a class=warn style='font-size: 8pt;' href="
                                           + applicationSettings.CustomPostLinkUrl
                                           + "?postid="
                                           + stringPostId
                                           + ">"
                                           + applicationSettings.CustomPostLinkLabel
                                           + "</a>";

                    stringBuilder.Append(customPostLink);
                }
            }

            stringBuilder.Append("\n</table>\n<table border=0>\n<tr><td>");
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
                    stringBuilder.Append(comment);
                    stringBuilder.Append("<p>");
                }

                stringBuilder.Append("<span class=pst>");
                if (writeLinks) stringBuilder.Append("<img src='" + VirtualPathUtility.ToAbsolute("~/Content/images/attach.gif") + "'>");
                stringBuilder.Append("attachment:&nbsp;</span><span class=cmt_text>");
                stringBuilder.Append(dr["bp_file"]);
                stringBuilder.Append("</span>");

                if (writeLinks)
                {
                    if ((string)dr["bp_content_type"] != "text/html" ||
                        applicationSettings.ShowPotentiallyDangerousHtml)
                    {
                        stringBuilder.Append("&nbsp;&nbsp;&nbsp;<a target=_blank style='font-size: 8pt;'");
                        stringBuilder.Append(" href='" + VirtualPathUtility.ToAbsolute($"~/Attachment/Show?download=false&id={stringPostId}&bugId={stringBugId}"));
                        stringBuilder.Append("'>view</a>");
                    }

                    stringBuilder.Append("&nbsp;&nbsp;&nbsp;<a target=_blank style='font-size: 8pt;'");
                    stringBuilder.Append(" href=" + VirtualPathUtility.ToAbsolute($"~/Attachment/Show?download=true&id={stringPostId}&bugId={stringBugId}"));
                    stringBuilder.Append(">save</a>");
                }

                stringBuilder.Append("<p><span class=pst>size: ");
                stringBuilder.Append(dr["bp_size"]);
                stringBuilder.Append("&nbsp;&nbsp;&nbsp;content-type: ");
                stringBuilder.Append(dr["bp_content_type"]);
                stringBuilder.Append("</span>");
            }
            else
            {
                stringBuilder.Append(comment);
            }

            // maybe show inline images
            if (type == "file")
                if (imagesInline)
                {
                    var file = Convert.ToString(dr["bp_file"]);
                    var html = WriteFileInline(file, stringPostId, stringBugId, (string)dr["bp_content_type"]);

                    stringBuilder.Append(html);
                }

            return stringBuilder.ToString();
        }

        private static string WriteEmailAttachment(int bugid, DataRow dr, bool writeLinks,
            bool imagesInline)
        {
            var stringBuilder = new StringBuilder();
            var stringPostId = Convert.ToString(dr["ba_id"]); // intentially "ba"
            var stringBugId = Convert.ToString(bugid);

            stringBuilder.Append("\n<p><span class=pst>");
            if (writeLinks) stringBuilder.Append("<img src='" + VirtualPathUtility.ToAbsolute("~/Content/images/attach.gif") + "'>");
            stringBuilder.Append("attachment:&nbsp;</span>");
            stringBuilder.Append(dr["ba_file"]); // intentially "ba"
            stringBuilder.Append("&nbsp;&nbsp;&nbsp;&nbsp;");

            if (writeLinks)
            {
                IApplicationSettings applicationSettings = new ApplicationSettings();

                if ((string)dr["bp_content_type"] != "text/html" ||
                    applicationSettings.ShowPotentiallyDangerousHtml)
                {
                    stringBuilder.Append("<a target=_blank href='" + VirtualPathUtility.ToAbsolute("~/Attachment/Show?download=false&id="));
                    stringBuilder.Append(stringPostId);
                    stringBuilder.Append("&bugId=");
                    stringBuilder.Append(stringBugId);
                    stringBuilder.Append("'>view</a>");
                }

                stringBuilder.Append("&nbsp;&nbsp;&nbsp;<a target=_blank href='" + VirtualPathUtility.ToAbsolute("~/Attachment/Show?download=true&id="));
                stringBuilder.Append(stringPostId);
                stringBuilder.Append("&bugId=");
                stringBuilder.Append(stringBugId);
                stringBuilder.Append("'>save</a>");
            }

            if (imagesInline)
            {
                var file = Convert.ToString(dr["ba_file"]); // intentially "ba"
                var html = WriteFileInline(file, stringPostId, stringBugId, (string)dr["ba_content_type"]);

                stringBuilder.Append(html);
            }

            stringBuilder.Append("<p><span class=pst>size: ");
            stringBuilder.Append(dr["ba_size"]);
            stringBuilder.Append("&nbsp;&nbsp;&nbsp;content-type: ");
            stringBuilder.Append(dr["ba_content_type"]);
            stringBuilder.Append("</span>");

            return stringBuilder.ToString();
        }

        private static string WriteFileInline(
            string filename,
            string stringPostId,
            string stringBugId,
            string contentType)
        {
            var stringBuilder = new StringBuilder();
            IApplicationSettings applicationSettings = new ApplicationSettings();

            if (contentType == "image/gif"
                || contentType == "image/jpg"
                || contentType == "image/jpeg"
                || contentType == "image/pjpeg"
                || contentType == "image/png"
                || contentType == "image/x-png"
                || contentType == "image/bmp"
                || contentType == "image/tiff")
                stringBuilder.Append("<p>"
                               + "<a href=javascript:resize_image('im" + stringPostId + "',1.5)>" + "[+]</a>&nbsp;"
                               + "<a href=javascript:resize_image('im" + stringPostId + "',.6)>" + "[-]</a>"
                               + "<br><img id=im" + stringPostId
                               + " src='" + VirtualPathUtility.ToAbsolute($"~/Attachment/Show?download=false&id={stringPostId}&bugId={stringBugId}")
                               + "'>");
            else if (contentType == "text/plain"
                     || contentType == "text/xml"
                     || contentType == "text/css"
                     || contentType == "text/js"
                     || contentType == "text/html" && applicationSettings.ShowPotentiallyDangerousHtml)
                stringBuilder.Append("<p>"
                               + "<a href=javascript:resize_iframe('if" + stringPostId + "',200)>" + "[+]</a>&nbsp;"
                               + "<a href=javascript:resize_iframe('if" + stringPostId + "',-200)>" + "[-]</a>"
                               + "<br><iframe id=if"
                               + stringPostId
                               + " width=780 height=200 src='" + VirtualPathUtility.ToAbsolute($"~/Attachment/Show?download=false&id={stringPostId}&bugId={stringBugId}")
                               + "'></iframe>");

            return stringBuilder.ToString();
        }

        public static string FormatEmailUserName(
            bool writeLinks,
            int bugid,
            SecurityPermissionLevel permissionLevel,
            string email,
            string username,
            string fullname)
        {
            IApplicationSettings applicationSettings = new ApplicationSettings();

            if (email != null && !string.IsNullOrEmpty(email) && writeLinks && permissionLevel != SecurityPermissionLevel.PermissionReadonly)
                return "<a href="
                       + applicationSettings.AbsoluteUrlPrefix
                       + VirtualPathUtility.ToAbsolute("~/SendEmail.aspx") + @"?bg_id="
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
            IApplicationSettings applicationSettings = new ApplicationSettings();

            return "<a href="
                   + applicationSettings.AbsoluteUrlPrefix
                   + VirtualPathUtility.ToAbsolute("~/SendEmail.aspx") + @"?bg_id=" + Convert.ToString(bugid)
                   + "&to=" + HttpUtility.UrlEncode(HttpUtility.HtmlDecode(email)) + ">"
                   + email
                   + "</a>";
        }

        private static string FormatEmailFrom(int commentId, string from)
        {
            var displayPart = string.Empty;
            var emailPart = string.Empty;
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

            IApplicationSettings applicationSettings = new ApplicationSettings();

            return displayPart
                   + " <a href="
                   + applicationSettings.AbsoluteUrlPrefix
                   + VirtualPathUtility.ToAbsolute("~/SendEmail.aspx") + @"?bp_id="
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
                                "<a href=" + VirtualPathUtility.ToAbsolute("~/SendEmail.aspx") + @"?bp_id="
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

            IApplicationSettings applicationSettings = new ApplicationSettings();

            // convert references to other bugs to links
            linkMarker = applicationSettings.BugLinkMarker;
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
            IApplicationSettings applicationSettings = new ApplicationSettings();

            return "<a href="
                   + applicationSettings.AbsoluteUrlPrefix
                   + VirtualPathUtility.ToAbsolute("~/Bugs/Edit.aspx?id=")
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
            IApplicationSettings applicationSettings = new ApplicationSettings();

            if (!applicationSettings.UseFullNames)
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

            IApplicationSettings applicationSettings = new ApplicationSettings();

            sql += "\n order by a.bp_id ";
            sql += applicationSettings.CommentSortOrder;
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