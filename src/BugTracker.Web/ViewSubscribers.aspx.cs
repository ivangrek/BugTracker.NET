/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using Core;

    public partial class ViewSubscribers : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public int Bugid;
        public DataSet Ds;
        public string Sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            Page.Title = $"{ApplicationSettings.AppTitle} - view subscribers";

            this.Bugid = Convert.ToInt32(Util.SanitizeInteger(Request["id"]));

            var permissionLevel = Bug.GetBugPermissionLevel(this.Bugid, Security);
            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var action = Request["actn"];

            if (action == null) action = "";

            if (action != "")
            {
                if (permissionLevel == SecurityPermissionLevel.PermissionReadonly)
                {
                    Response.Write("You are not allowed to edit this item");
                    Response.End();
                }

                if (!string.IsNullOrEmpty(Request["userid"]))
                {
                    var newSubscriberUserid = Convert.ToInt32(Request["userid"]);

                    this.Sql = @"delete from bug_subscriptions where bs_bug = $bg and bs_user = $us;
            insert into bug_subscriptions (bs_bug, bs_user) values($bg, $us)";
                    ;
                    this.Sql = this.Sql.Replace("$bg", Convert.ToString(this.Bugid));
                    this.Sql = this.Sql.Replace("$us", Convert.ToString(newSubscriberUserid));
                    DbUtil.ExecuteNonQuery(this.Sql);

                    // send a notification to this user only
                    Bug.SendNotifications(Bug.Update, this.Bugid, Security, newSubscriberUserid);
                }
            }

            // clean up bug subscriptions that no longer fit the security restrictions

            Bug.AutoSubscribe(this.Bugid);

            // show who is subscribed

            if (Security.User.IsAdmin)
            {
                this.Sql = @"
select
'<a href=DeleteSubscriber.aspx?ses=$ses&bg_id=$bg&us_id=' + convert(varchar,us_id) + '>unsubscribe</a>'	[$no_sort_unsubscriber],
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

                this.Sql = this.Sql.Replace("$ses", Convert.ToString(Session["session_cookie"]));
            }
            else
            {
                this.Sql = @"
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

            this.Sql = this.Sql.Replace("$bg", Convert.ToString(this.Bugid));
            this.Ds = DbUtil.GetDataSet(this.Sql);

            // Get list of users who could be subscribed to this bug.

            this.Sql = @"
declare @project int;
declare @org int;
select @project = bg_project, @org = bg_org from bugs where bg_id = $bg;";

            // Only users explicitly allowed will be listed
            if (ApplicationSettings.DefaultPermissionLevel == 0)
                this.Sql +=
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
                this.Sql +=
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

            if (!ApplicationSettings.UseFullNames)
            {
                // false condition
                this.Sql = this.Sql.Replace("$fullnames", "0 = 1");
            }
            else
                // true condition
                this.Sql = this.Sql.Replace("$fullnames", "1 = 1");

            this.Sql = this.Sql.Replace("$bg", Convert.ToString(this.Bugid));

            //DataSet ds_users =
            this.userid.DataSource = DbUtil.GetDataView(this.Sql);
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