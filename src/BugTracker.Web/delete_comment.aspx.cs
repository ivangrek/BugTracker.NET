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

    public partial class delete_comment : Page
    {
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

            if (IsPostBack)
            {
                // do delete here

                this.sql = @"delete bug_posts where bp_id = $1";
                this.sql = this.sql.Replace("$1", Util.sanitize_integer(this.row_id.Value));
                DbUtil.execute_nonquery(this.sql);
                Response.Redirect("edit_bug.aspx?id=" + Util.sanitize_integer(this.redirect_bugid.Value));
            }
            else
            {
                var bug_id = Util.sanitize_integer(Request["bug_id"]);
                this.redirect_bugid.Value = bug_id;

                var permission_level = Bug.get_bug_permission_level(Convert.ToInt32(bug_id), this.security);
                if (permission_level != Security.PERMISSION_ALL)
                {
                    Response.Write("You are not allowed to edit this item");
                    Response.End();
                }

                Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                            + "delete comment";

                var id = Util.sanitize_integer(Request["id"]);

                this.back_href.HRef = "edit_bug.aspx?id=" + bug_id;

                this.sql = @"select bp_comment from bug_posts where bp_id = $1";
                this.sql = this.sql.Replace("$1", id);

                var dr = DbUtil.get_datarow(this.sql);

                // show the first few chars of the comment
                var s = Convert.ToString(dr["bp_comment"]);
                var len = 20;
                if (s.Length < len) len = s.Length;

                this.confirm_href.InnerText = "confirm delete of comment: "
                                              + s.Substring(0, len)
                                              + "...";

                this.row_id.Value = id;
            }
        }
    }
}