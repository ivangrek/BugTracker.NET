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

    public partial class delete_user : Page
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
            this.security.check_security(HttpContext.Current, Security.MUST_BE_ADMIN_OR_PROJECT_ADMIN);

            var id = Util.sanitize_integer(Request["id"]);

            if (!this.security.user.is_admin)
            {
                this.sql = @"select us_created_user, us_admin from users where us_id = $us";
                this.sql = this.sql.Replace("$us", id);
                var dr = DbUtil.get_datarow(this.sql);

                if (this.security.user.usid != (int) dr["us_created_user"])
                {
                    Response.Write("You not allowed to delete this user, because you didn't create it.");
                    Response.End();
                }
                else if ((int) dr["us_admin"] == 1)
                {
                    Response.Write("You not allowed to delete this user, because it is an admin.");
                    Response.End();
                }
            }

            if (IsPostBack)
            {
                // do delete here
                this.sql = @"
delete from emailed_links where el_username in (select us_username from users where us_id = $us)
delete users where us_id = $us
delete project_user_xref where pu_user = $us
delete bug_subscriptions where bs_user = $us
delete bug_user where bu_user = $us
delete queries where qu_user = $us
delete queued_notifications where qn_user = $us
delete dashboard_items where ds_user = $us";

                this.sql = this.sql.Replace("$us", Util.sanitize_integer(this.row_id.Value));
                DbUtil.execute_nonquery(this.sql);
                Server.Transfer("users.aspx");
            }
            else
            {
                Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                            + "delete user";

                this.sql = @"declare @cnt int
select @cnt = count(1) from bugs where bg_reported_user = $us or bg_assigned_to_user = $us
if @cnt = 0
begin
	select @cnt = count(1) from bug_posts where bp_user = $us
end
select us_username, @cnt [cnt] from users where us_id = $us";

                this.sql = this.sql.Replace("$us", id);

                var dr = DbUtil.get_datarow(this.sql);

                if ((int) dr["cnt"] > 0)
                {
                    Response.Write("You can't delete user \""
                                   + Convert.ToString(dr["us_username"])
                                   + "\" because some bugs or bug posts still reference it.");
                    Response.End();
                }
                else
                {
                    this.confirm_href.InnerText = "confirm delete of \""
                                                  + Convert.ToString(dr["us_username"])
                                                  + "\"";

                    this.row_id.Value = id;
                }
            }
        }
    }
}