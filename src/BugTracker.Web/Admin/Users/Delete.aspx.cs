/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Users
{
    using System;
    using System.Web.UI;
    using BugTracker.Web.Core.Controls;
    using Core;

    public partial class Delete : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        protected string Sql {get; set; }

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.MustBeAdminOrProjectAdmin);

            MainMenu.SelectedItem = MainMenuSections.Administration;

            var id = Util.SanitizeInteger(Request["id"]);

            if (!Security.User.IsAdmin)
            {
                this.Sql = @"select us_created_user, us_admin from users where us_id = $us";
                this.Sql = this.Sql.Replace("$us", id);
                var dr = DbUtil.GetDataRow(this.Sql);

                if (Security.User.Usid != (int) dr["us_created_user"])
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
                this.Sql = @"
delete from emailed_links where el_username in (select us_username from users where us_id = $us)
delete users where us_id = $us
delete project_user_xref where pu_user = $us
delete bug_subscriptions where bs_user = $us
delete bug_user where bu_user = $us
delete queries where qu_user = $us
delete queued_notifications where qn_user = $us
delete dashboard_items where ds_user = $us";

                this.Sql = this.Sql.Replace("$us", Util.SanitizeInteger(this.row_id.Value));
                DbUtil.ExecuteNonQuery(this.Sql);
                Response.Redirect("~/Admin/Users/List.aspx");
            }
            else
            {
                Page.Title = $"{ApplicationSettings.AppTitle} - delete user";

                this.Sql = @"declare @cnt int
select @cnt = count(1) from bugs where bg_reported_user = $us or bg_assigned_to_user = $us
if @cnt = 0
begin
	select @cnt = count(1) from bug_posts where bp_user = $us
end
select us_username, @cnt [cnt] from users where us_id = $us";

                this.Sql = this.Sql.Replace("$us", id);

                var dr = DbUtil.GetDataRow(this.Sql);

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