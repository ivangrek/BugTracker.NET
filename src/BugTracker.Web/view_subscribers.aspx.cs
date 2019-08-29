/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using Core;

    public partial class view_subscribers : Page
    {
        public int bugid;
        public DataSet ds;
        public Security security;
        public string sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "view subscribers";

            this.bugid = Convert.ToInt32(Util.sanitize_integer(Request["id"]));

            var permission_level = Bug.get_bug_permission_level(this.bugid, this.security);
            if (permission_level == Security.PERMISSION_NONE)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var action = Request["actn"];

            if (action == null) action = "";

            if (action != "")
            {
                if (permission_level == Security.PERMISSION_READONLY)
                {
                    Response.Write("You are not allowed to edit this item");
                    Response.End();
                }

                if (!string.IsNullOrEmpty(Request["userid"]))
                {
                    var new_subscriber_userid = Convert.ToInt32(Request["userid"]);

                    this.sql = @"delete from bug_subscriptions where bs_bug = $bg and bs_user = $us;
			insert into bug_subscriptions (bs_bug, bs_user) values($bg, $us)";
                    ;
                    this.sql = this.sql.Replace("$bg", Convert.ToString(this.bugid));
                    this.sql = this.sql.Replace("$us", Convert.ToString(new_subscriber_userid));
                    DbUtil.execute_nonquery(this.sql);

                    // send a notification to this user only
                    Bug.send_notifications(Bug.UPDATE, this.bugid, this.security, new_subscriber_userid);
                }
            }

            // clean up bug subscriptions that no longer fit the security restrictions

            Bug.auto_subscribe(this.bugid);

            // show who is subscribed

            if (this.security.user.is_admin)
            {
                this.sql = @"
select
'<a href=delete_subscriber.aspx?ses=$ses&bg_id=$bg&us_id=' + convert(varchar,us_id) + '>unsubscribe</a>'	[$no_sort_unsubscriber],
us_username [user],
us_lastname + ', ' + us_firstname [name],
us_email [email],
case when us_reported_notifications < 4 or us_assigned_notifications < 4 or us_subscribed_notifications < 4 then 'Y' else 'N' end [user is<br>filtering<br>notifications]
from bug_subscriptions
inner join users on bs_user = us_id
where bs_bug = $bg
and us_enable_notifications = 1
and us_active = 1
order by 1";

                this.sql = this.sql.Replace("$ses", Convert.ToString(Session["session_cookie"]));
            }
            else
            {
                this.sql = @"
select
us_username [user],
us_lastname + ', ' + us_firstname [name],
us_email [email],
case when us_reported_notifications < 4 or us_assigned_notifications < 4 or us_subscribed_notifications < 4 then 'Y' else 'N' end [user is<br>filtering<br>notifications]
from bug_subscriptions
inner join users on bs_user = us_id
where bs_bug = $bg
and us_enable_notifications = 1
and us_active = 1
order by 1";
            }

            this.sql = this.sql.Replace("$bg", Convert.ToString(this.bugid));
            this.ds = DbUtil.get_dataset(this.sql);

            // Get list of users who could be subscribed to this bug.

            this.sql = @"
declare @project int;
declare @org int;
select @project = bg_project, @org = bg_org from bugs where bg_id = $bg;";

            // Only users explicitly allowed will be listed
            if (Util.get_setting("DefaultPermissionLevel", "2") == "0")
                this.sql +=
                    @"select us_id, case when $fullnames then us_lastname + ', ' + us_firstname else us_username end us_username
			from users
			where us_active = 1
			and us_enable_notifications = 1
			and us_id in
				(select pu_user from project_user_xref
				where pu_project = @project
				and pu_permission_level <> 0)
			and us_id not in (
				select us_id
				from bug_subscriptions
				inner join users on bs_user = us_id
				where bs_bug = $bg
				and us_enable_notifications = 1
				and us_active = 1)
			and us_id not in (
				select us_id from users
				inner join orgs on us_org = og_id
				where us_org <> @org
				and og_other_orgs_permission_level = 0)

			order by us_username; ";
            // Only users explictly DISallowed will be omitted
            else
                this.sql +=
                    @"select us_id, case when $fullnames then us_lastname + ', ' + us_firstname else us_username end us_username
			from users
			where us_active = 1
			and us_enable_notifications = 1
			and us_id not in
				(select pu_user from project_user_xref
				where pu_project = @project
				and pu_permission_level = 0)
			and us_id not in (
				select us_id
				from bug_subscriptions
				inner join users on bs_user = us_id
				where bs_bug = $bg
				and us_enable_notifications = 1
				and us_active = 1)
			and us_id not in (
				select us_id from users
				inner join orgs on us_org = og_id
				where us_org <> @org
				and og_other_orgs_permission_level = 0)
			order by us_username; ";

            if (Util.get_setting("UseFullNames", "0") == "0")
                // false condition
                this.sql = this.sql.Replace("$fullnames", "0 = 1");
            else
                // true condition
                this.sql = this.sql.Replace("$fullnames", "1 = 1");

            this.sql = this.sql.Replace("$bg", Convert.ToString(this.bugid));

            //DataSet ds_users =
            this.userid.DataSource = DbUtil.get_dataview(this.sql);
            this.userid.DataTextField = "us_username";
            this.userid.DataValueField = "us_id";
            this.userid.DataBind();

            if (this.userid.Items.Count == 0)
                this.userid.Items.Insert(0, new ListItem("[no users to select]", "0"));
            else
                this.userid.Items.Insert(0, new ListItem("[select to add]", "0"));
        }
    }
}