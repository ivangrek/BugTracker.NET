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
    using BugTracker.Web.Core.Controls;
    using Core;

    public partial class EditSelf : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public int Id;
        public string Sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            MainMenu.SelectedItem = MainMenuSections.Settings;

            Page.Title = $"{ApplicationSettings.AppTitle} - edit your settings";

            this.msg.InnerText = "";

            this.Id = Security.User.Usid;

            if (!IsPostBack)
            {
                this.Sql = @"declare @org int
            select @org = us_org from users where us_id = $us

            select qu_id, qu_desc
            from queries
            where (isnull(qu_user,0) = 0 and isnull(qu_org,0) = 0)
            or isnull(qu_user,0) = $us
            or isnull(qu_org,0) = @org
            order by qu_desc";

                this.Sql = this.Sql.Replace("$us", Convert.ToString(Security.User.Usid));

                this.query.DataSource = DbUtil.GetDataView(this.Sql);
                this.query.DataTextField = "qu_desc";
                this.query.DataValueField = "qu_id";
                this.query.DataBind();

                this.Sql = @"select pj_id, pj_name, isnull(pu_auto_subscribe,0) [pu_auto_subscribe]
            from projects
            left outer join project_user_xref on pj_id = pu_project and $us = pu_user
            where isnull(pu_permission_level,$dpl) <> 0
            order by pj_name";

                this.Sql = this.Sql.Replace("$us", Convert.ToString(Security.User.Usid));
                this.Sql = this.Sql.Replace("$dpl", ApplicationSettings.DefaultPermissionLevel.ToString());

                var projectsDv = DbUtil.GetDataView(this.Sql);

                this.project_auto_subscribe.DataSource = projectsDv;
                this.project_auto_subscribe.DataTextField = "pj_name";
                this.project_auto_subscribe.DataValueField = "pj_id";
                this.project_auto_subscribe.DataBind();

                // Get this entry's data from the db and fill in the form
                // MAW -- 2006/01/27 -- Converted to use new notification columns

                this.Sql = @"select
            us_username [username],
            isnull(us_firstname,'') [firstname],
            isnull(us_lastname,'') [lastname],
            isnull(us_bugs_per_page,10) [us_bugs_per_page],
            us_use_fckeditor,
            us_enable_bug_list_popups,
            isnull(us_email,'') [email],
            us_enable_notifications,
            us_send_notifications_to_self,
            us_reported_notifications,
            us_assigned_notifications,
            us_subscribed_notifications,
            us_auto_subscribe,
            us_auto_subscribe_own_bugs,
            us_auto_subscribe_reported_bugs,
            us_default_query,
            isnull(us_signature,'') [signature]
            from users
            where us_id = $id";

                this.Sql = this.Sql.Replace("$id", Convert.ToString(this.Id));

                var dr = DbUtil.GetDataRow(this.Sql);

                // Fill in this form
                this.firstname.Value = (string) dr["firstname"];
                this.lastname.Value = (string) dr["lastname"];
                this.bugs_per_page.Value = Convert.ToString(dr["us_bugs_per_page"]);

                if (ApplicationSettings.DisableFCKEditor)
                {
                    this.use_fckeditor.Visible = false;
                    this.use_fckeditor_label.Visible = false;
                }

                this.use_fckeditor.Checked = Convert.ToBoolean((int) dr["us_use_fckeditor"]);
                this.enable_popups.Checked = Convert.ToBoolean((int) dr["us_enable_bug_list_popups"]);
                this.email.Value = (string) dr["email"];
                this.enable_notifications.Checked = Convert.ToBoolean((int) dr["us_enable_notifications"]);
                this.reported_notifications.Items[(int) dr["us_reported_notifications"]].Selected = true;
                this.assigned_notifications.Items[(int) dr["us_assigned_notifications"]].Selected = true;
                this.subscribed_notifications.Items[(int) dr["us_subscribed_notifications"]].Selected = true;
                this.send_to_self.Checked = Convert.ToBoolean((int) dr["us_send_notifications_to_self"]);
                this.auto_subscribe.Checked = Convert.ToBoolean((int) dr["us_auto_subscribe"]);
                this.auto_subscribe_own.Checked = Convert.ToBoolean((int) dr["us_auto_subscribe_own_bugs"]);
                this.auto_subscribe_reported.Checked = Convert.ToBoolean((int) dr["us_auto_subscribe_reported_bugs"]);
                this.signature.InnerText = (string) dr["signature"];

                foreach (ListItem li in this.query.Items)
                    if (Convert.ToInt32(li.Value) == (int) dr["us_default_query"])
                    {
                        li.Selected = true;
                        break;
                    }

                // select projects
                foreach (DataRowView drv in projectsDv)
                foreach (ListItem li in this.project_auto_subscribe.Items)
                    if (Convert.ToInt32(li.Value) == (int) drv["pj_id"])
                    {
                        if ((int) drv["pu_auto_subscribe"] == 1)
                            li.Selected = true;
                        else
                            li.Selected = false;
                    }
            }
            else
            {
                on_update();
            }
        }

        public bool validate()
        {
            var good = true;

            this.pw_err.InnerText = "";

            if (this.pw.Value != "")
                if (!Util.CheckPasswordStrength(this.pw.Value))
                {
                    good = false;
                    this.pw_err.InnerHtml = "Password is not difficult enough to guess.";
                    this.pw_err.InnerHtml += "<br>Avoid common words.";
                    this.pw_err.InnerHtml +=
                        "<br>Try using a mixture of lowercase, uppercase, digits, and special characters.";
                }

            if (this.confirm_pw.Value != this.pw.Value)
            {
                good = false;
                this.confirm_pw_err.InnerText = "Confirm Password must match Password.";
            }
            else
            {
                this.confirm_pw_err.InnerText = "";
            }

            if (!Util.IsInt(this.bugs_per_page.Value))
            {
                good = false;
                this.bugs_per_page_err.InnerText =
                    ApplicationSettings.PluralBugLabel + " Per Page must be a number.";
            }
            else
            {
                this.bugs_per_page_err.InnerText = "";
            }

            this.email_err.InnerHtml = "";
            if (this.email.Value != "")
                if (!Util.ValidateEmail(this.email.Value))
                {
                    good = false;
                    this.email_err.InnerHtml = "Format of email address is invalid.";
                }

            return good;
        }

        public void on_update()
        {
            var good = validate();

            if (good)
            {
                this.Sql = @"update users set
            us_firstname = N'$fn',
            us_lastname = N'$ln',
            us_bugs_per_page = N'$bp',
            us_use_fckeditor = $fk,
            us_enable_bug_list_popups = $pp,
            us_email = N'$em',
            us_enable_notifications = $en,
            us_send_notifications_to_self = $ss,
            us_reported_notifications = $rn,
            us_assigned_notifications = $an,
            us_subscribed_notifications = $sn,
            us_auto_subscribe = $as,
            us_auto_subscribe_own_bugs = $ao,
            us_auto_subscribe_reported_bugs = $ar,
            us_default_query = $dq,
            us_signature = N'$sg'
            where us_id = $id";

                this.Sql = this.Sql.Replace("$fn", this.firstname.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$ln", this.lastname.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$bp", this.bugs_per_page.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$fk", Util.BoolToString(this.use_fckeditor.Checked));
                this.Sql = this.Sql.Replace("$pp", Util.BoolToString(this.enable_popups.Checked));
                this.Sql = this.Sql.Replace("$em", this.email.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$en", Util.BoolToString(this.enable_notifications.Checked));
                this.Sql = this.Sql.Replace("$ss", Util.BoolToString(this.send_to_self.Checked));
                this.Sql = this.Sql.Replace("$rn", this.reported_notifications.SelectedItem.Value);
                this.Sql = this.Sql.Replace("$an", this.assigned_notifications.SelectedItem.Value);
                this.Sql = this.Sql.Replace("$sn", this.subscribed_notifications.SelectedItem.Value);
                this.Sql = this.Sql.Replace("$as", Util.BoolToString(this.auto_subscribe.Checked));
                this.Sql = this.Sql.Replace("$ao", Util.BoolToString(this.auto_subscribe_own.Checked));
                this.Sql = this.Sql.Replace("$ar", Util.BoolToString(this.auto_subscribe_reported.Checked));
                this.Sql = this.Sql.Replace("$dq", this.query.SelectedItem.Value);
                this.Sql = this.Sql.Replace("$sg", this.signature.InnerText.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$id", Convert.ToString(this.Id));

                // update user
                DbUtil.ExecuteNonQuery(this.Sql);

                // update the password
                if (this.pw.Value != "") Util.UpdateUserPassword(this.Id, this.pw.Value);

                // Now update project_user_xref

                // First turn everything off, then turn selected ones on.
                this.Sql = @"update project_user_xref
                set pu_auto_subscribe = 0 where pu_user = $id";
                this.Sql = this.Sql.Replace("$id", Convert.ToString(this.Id));
                DbUtil.ExecuteNonQuery(this.Sql);

                // Second see what to turn back on
                var projects = "";
                foreach (ListItem li in this.project_auto_subscribe.Items)
                    if (li.Selected)
                    {
                        if (projects != "") projects += ",";
                        projects += Convert.ToInt32(li.Value);
                    }

                // If we need to turn anything back on
                if (projects != "")
                {
                    this.Sql = @"update project_user_xref
                set pu_auto_subscribe = 1 where pu_user = $id and pu_project in ($projects)

            insert into project_user_xref (pu_project, pu_user, pu_auto_subscribe)
                select pj_id, $id, 1
                from projects
                where pj_id in ($projects)
                and pj_id not in (select pu_project from project_user_xref where pu_user = $id)";

                    this.Sql = this.Sql.Replace("$id", Convert.ToString(this.Id));
                    this.Sql = this.Sql.Replace("$projects", projects);
                    DbUtil.ExecuteNonQuery(this.Sql);
                }

                // apply subscriptions retroactively
                if (this.retroactive.Checked)
                {
                    this.Sql = @"delete from bug_subscriptions where bs_user = $id;";
                    if (this.auto_subscribe.Checked)
                    {
                        this.Sql += @"insert into bug_subscriptions (bs_bug, bs_user)
                    select bg_id, $id from bugs;";
                    }
                    else
                    {
                        if (this.auto_subscribe_reported.Checked)
                            this.Sql += @"insert into bug_subscriptions (bs_bug, bs_user)
                        select bg_id, $id from bugs where bg_reported_user = $id
                        and bg_id not in (select bs_bug from bug_subscriptions where bs_user = $id);";

                        if (this.auto_subscribe_own.Checked)
                            this.Sql += @"insert into bug_subscriptions (bs_bug, bs_user)
                        select bg_id, $id from bugs where bg_assigned_to_user = $id
                        and bg_id not in (select bs_bug from bug_subscriptions where bs_user = $id);";

                        if (projects != "")
                            this.Sql += @"insert into bug_subscriptions (bs_bug, bs_user)
                        select bg_id, $id from bugs where bg_project in ($projects)
                        and bg_id not in (select bs_bug from bug_subscriptions where bs_user = $id);";
                    }

                    this.Sql = this.Sql.Replace("$id", Convert.ToString(this.Id));
                    this.Sql = this.Sql.Replace("$projects", projects);
                    DbUtil.ExecuteNonQuery(this.Sql);
                }

                this.msg.InnerText = "Your settings have been updated.";
            }
            else
            {
                this.msg.InnerText = "Your settings have not been updated.";
            }
        }
    }
}