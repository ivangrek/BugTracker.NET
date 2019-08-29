/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class edit_comment : Page
    {
        public int bugid;
        public int id;

        public Security security;
        public string sql;
        public bool use_fckeditor;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();

            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK_EXCEPT_GUEST);

            if (this.security.user.is_admin || this.security.user.can_edit_and_delete_posts)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "edit comment";

            this.msg.InnerText = "";

            this.id = Convert.ToInt32(Request["id"]);

            if (!IsPostBack)
                this.sql = @"select bp_comment, bp_type,
        isnull(bp_comment_search,bp_comment) bp_comment_search,
        isnull(bp_content_type,'') bp_content_type,
        bp_bug, bp_hidden_from_external_users
        from bug_posts where bp_id = $id";
            else
                this.sql = @"select bp_bug, bp_type,
        isnull(bp_content_type,'') bp_content_type,
        bp_hidden_from_external_users
        from bug_posts where bp_id = $id";

            this.sql = this.sql.Replace("$id", Convert.ToString(this.id));
            var dr = DbUtil.get_datarow(this.sql);

            this.bugid = (int) dr["bp_bug"];

            var permission_level = Bug.get_bug_permission_level(this.bugid, this.security);
            if (permission_level == Security.PERMISSION_NONE
                || permission_level == Security.PERMISSION_READONLY
                || (string) dr["bp_type"] != "comment")
            {
                Response.Write("You are not allowed to edit this item");
                Response.End();
            }

            var content_type = (string) dr["bp_content_type"];

            if (this.security.user.use_fckeditor && content_type == "text/html" &&
                Util.get_setting("DisableFCKEditor", "0") == "0")
                this.use_fckeditor = true;
            else
                this.use_fckeditor = false;

            if (this.security.user.external_user || Util.get_setting("EnableInternalOnlyPosts", "0") == "0")
            {
                this.internal_only.Visible = false;
                this.internal_only_label.Visible = false;
            }

            if (!IsPostBack)
            {
                this.internal_only.Checked = Convert.ToBoolean((int) dr["bp_hidden_from_external_users"]);

                if (this.use_fckeditor)
                    this.comment.Value = (string) dr["bp_comment"];
                else
                    this.comment.Value = (string) dr["bp_comment_search"];
            }
            else
            {
                on_update();
            }
        }

        public bool validate()
        {
            var good = true;

            if (this.comment.Value.Length == 0)
            {
                this.msg.InnerText = "Comment cannot be blank.";
                return false;
            }

            return good;
        }

        public void on_update()
        {
            var good = validate();

            if (good)
            {
                this.sql = @"update bug_posts set
                    bp_comment = N'$cm',
                    bp_comment_search = N'$cs',
                    bp_content_type = N'$cn',
                    bp_hidden_from_external_users = $internal
                where bp_id = $id

                select bg_short_desc from bugs where bg_id = $bugid";

                if (this.use_fckeditor)
                {
                    var text = Util.strip_dangerous_tags(this.comment.Value);
                    this.sql = this.sql.Replace("$cm", text.Replace("'", "&#39;"));
                    this.sql = this.sql.Replace("$cs", Util.strip_html(this.comment.Value).Replace("'", "''"));
                    this.sql = this.sql.Replace("$cn", "text/html");
                }
                else
                {
                    this.sql = this.sql.Replace("$cm", HttpUtility.HtmlDecode(this.comment.Value).Replace("'", "''"));
                    this.sql = this.sql.Replace("$cs", this.comment.Value.Replace("'", "''"));
                    this.sql = this.sql.Replace("$cn", "text/plain");
                }

                this.sql = this.sql.Replace("$id", Convert.ToString(this.id));
                this.sql = this.sql.Replace("$bugid", Convert.ToString(this.bugid));
                this.sql = this.sql.Replace("$internal", Util.bool_to_string(this.internal_only.Checked));
                var dr = DbUtil.get_datarow(this.sql);

                // Don't send notifications for internal only comments.
                // We aren't putting them the email notifications because it that makes it
                // easier for them to accidently get forwarded to the "wrong" people...
                if (!this.internal_only.Checked)
                {
                    Bug.send_notifications(Bug.UPDATE, this.bugid, this.security);
                    WhatsNew.add_news(this.bugid, (string) dr["bg_short_desc"], "updated", this.security);
                }

                Response.Redirect("edit_bug.aspx?id=" + Convert.ToString(this.bugid));
            }
        }
    }
}