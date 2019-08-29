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

    public partial class edit_attachment : Page
    {
        public int bugid;
        public int id;
        public Security security;
        public string sql;

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
                                                                        + "edit attachment";

            this.msg.InnerText = "";

            var var = Request.QueryString["id"];
            this.id = Convert.ToInt32(var);

            var = Request.QueryString["bug_id"];
            this.bugid = Convert.ToInt32(var);

            var permission_level = Bug.get_bug_permission_level(this.bugid, this.security);
            if (permission_level != Security.PERMISSION_ALL)
            {
                Response.Write("You are not allowed to edit this item");
                Response.End();
            }

            if (this.security.user.external_user || Util.get_setting("EnableInternalOnlyPosts", "0") == "0")
            {
                this.internal_only.Visible = false;
                this.internal_only_label.Visible = false;
            }

            if (!IsPostBack)
            {
                // Get this entry's data from the db and fill in the form

                this.sql = @"select bp_comment, bp_file, bp_hidden_from_external_users from bug_posts where bp_id = $1";
                this.sql = this.sql.Replace("$1", Convert.ToString(this.id));
                var dr = DbUtil.get_datarow(this.sql);

                // Fill in this form
                this.desc.Value = (string) dr["bp_comment"];
                this.filename.InnerText = (string) dr["bp_file"];
                this.internal_only.Checked = Convert.ToBoolean((int) dr["bp_hidden_from_external_users"]);
            }
            else
            {
                on_update();
            }
        }

        public bool validate()
        {
            var good = true;

            return good;
        }

        public void on_update()
        {
            var good = validate();

            if (good)
            {
                this.sql = @"update bug_posts set
			bp_comment = N'$1',
			bp_hidden_from_external_users = $internal
			where bp_id = $3";

                this.sql = this.sql.Replace("$3", Convert.ToString(this.id));
                this.sql = this.sql.Replace("$1", this.desc.Value.Replace("'", "''"));
                this.sql = this.sql.Replace("$internal", Util.bool_to_string(this.internal_only.Checked));

                DbUtil.execute_nonquery(this.sql);

                if (!this.internal_only.Checked) Bug.send_notifications(Bug.UPDATE, this.bugid, this.security);

                Response.Redirect("edit_bug.aspx?id=" + Convert.ToString(this.bugid));
            }
            else
            {
                this.msg.InnerText = "Attachment was not updated.";
            }
        }
    }
}