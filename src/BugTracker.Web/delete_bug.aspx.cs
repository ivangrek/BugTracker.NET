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

    public partial class delete_bug : Page
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

            if (this.security.user.is_admin || this.security.user.can_delete_bug)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            var id = Util.sanitize_integer(Request["id"]);

            var permission_level = Bug.get_bug_permission_level(Convert.ToInt32(id), this.security);
            if (permission_level != Security.PERMISSION_ALL)
            {
                Response.Write("You are not allowed to edit this item");
                Response.End();
            }

            if (IsPostBack)
            {
                Bug.delete_bug(Convert.ToInt32(this.row_id.Value));
                Server.Transfer("bugs.aspx");
            }
            else
            {
                Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                            + "delete " +
                                                                            Util.get_setting("SingularBugLabel",
                                                                                "bug");

                this.back_href.HRef = "edit_bug.aspx?id=" + id;

                this.sql = @"select bg_short_desc from bugs where bg_id = $1";
                this.sql = this.sql.Replace("$1", id);

                var dr = DbUtil.get_datarow(this.sql);

                this.confirm_href.InnerText = "confirm delete of "
                                              + Util.get_setting("SingularBugLabel", "bug")
                                              + ": "
                                              + Convert.ToString(dr["bg_short_desc"]);

                this.row_id.Value = id;
            }
        }
    }
}