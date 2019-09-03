/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Comments
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class Edit : Page
    {
        public int Bugid;
        public int Id;

        public Security Security;
        public string Sql;
        public bool UseFckeditor;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();

            this.Security.CheckSecurity(HttpContext.Current, Security.AnyUserOkExceptGuest);

            if (this.Security.User.IsAdmin || this.Security.User.CanEditAndDeletePosts)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "edit comment";

            this.msg.InnerText = "";

            this.Id = Convert.ToInt32(Request["id"]);

            if (!IsPostBack)
                this.Sql = @"select bp_comment, bp_type,
        isnull(bp_comment_search,bp_comment) bp_comment_search,
        isnull(bp_content_type,'') bp_content_type,
        bp_bug, bp_hidden_from_external_users
        from bug_posts where bp_id = $id";
            else
                this.Sql = @"select bp_bug, bp_type,
        isnull(bp_content_type,'') bp_content_type,
        bp_hidden_from_external_users
        from bug_posts where bp_id = $id";

            this.Sql = this.Sql.Replace("$id", Convert.ToString(this.Id));
            var dr = DbUtil.GetDataRow(this.Sql);

            this.Bugid = (int) dr["bp_bug"];

            var permissionLevel = Bug.GetBugPermissionLevel(this.Bugid, this.Security);
            if (permissionLevel == Security.PermissionNone
                || permissionLevel == Security.PermissionReadonly
                || (string) dr["bp_type"] != "comment")
            {
                Response.Write("You are not allowed to edit this item");
                Response.End();
            }

            var contentType = (string) dr["bp_content_type"];

            if (this.Security.User.UseFckeditor && contentType == "text/html" &&
                Util.GetSetting("DisableFCKEditor", "0") == "0")
                this.UseFckeditor = true;
            else
                this.UseFckeditor = false;

            if (this.Security.User.ExternalUser || Util.GetSetting("EnableInternalOnlyPosts", "0") == "0")
            {
                this.internal_only.Visible = false;
                this.internal_only_label.Visible = false;
            }

            if (!IsPostBack)
            {
                this.internal_only.Checked = Convert.ToBoolean((int) dr["bp_hidden_from_external_users"]);

                if (this.UseFckeditor)
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
                this.Sql = @"update bug_posts set
                    bp_comment = N'$cm',
                    bp_comment_search = N'$cs',
                    bp_content_type = N'$cn',
                    bp_hidden_from_external_users = $internal
                where bp_id = $id

                select bg_short_desc from bugs where bg_id = $bugid";

                if (this.UseFckeditor)
                {
                    var text = Util.StripDangerousTags(this.comment.Value);
                    this.Sql = this.Sql.Replace("$cm", text.Replace("'", "&#39;"));
                    this.Sql = this.Sql.Replace("$cs", Util.StripHtml(this.comment.Value).Replace("'", "''"));
                    this.Sql = this.Sql.Replace("$cn", "text/html");
                }
                else
                {
                    this.Sql = this.Sql.Replace("$cm", HttpUtility.HtmlDecode(this.comment.Value).Replace("'", "''"));
                    this.Sql = this.Sql.Replace("$cs", this.comment.Value.Replace("'", "''"));
                    this.Sql = this.Sql.Replace("$cn", "text/plain");
                }

                this.Sql = this.Sql.Replace("$id", Convert.ToString(this.Id));
                this.Sql = this.Sql.Replace("$bugid", Convert.ToString(this.Bugid));
                this.Sql = this.Sql.Replace("$internal", Util.BoolToString(this.internal_only.Checked));
                var dr = DbUtil.GetDataRow(this.Sql);

                // Don't send notifications for internal only comments.
                // We aren't putting them the email notifications because it that makes it
                // easier for them to accidently get forwarded to the "wrong" people...
                if (!this.internal_only.Checked)
                {
                    Bug.SendNotifications(Bug.Update, this.Bugid, this.Security);
                    Core.WhatsNew.AddNews(this.Bugid, (string) dr["bg_short_desc"], "updated", this.Security);
                }

                Response.Redirect($"~/Bugs/Edit.aspx?id={this.Bugid}");
            }
        }
    }
}