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

    public partial class edit_self : Page
    {
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

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "edit your settings";

            this.msg.InnerText = "";

            this.id = this.security.user.usid;

            if (!IsPostBack)
            {
                this.sql = @"declare @org int
			select @org = us_org from users where us_id = $us

			select qu_id, qu_desc
			from queries
			where (isnull(qu_user,0) = 0 and isnull(qu_org,0) = 0)
			or isnull(qu_user,0) = $us
			or isnull(qu_org,0) = @org
			order by qu_desc";

                this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));

                this.query.DataSource = DbUtil.get_dataview(this.sql);
                this.query.DataTextField = "qu_desc";
                this.query.DataValueField = "qu_id";
                this.query.DataBind();

                this.sql = @"select pj_id, pj_name, isnull(pu_auto_subscribe,0) [pu_auto_subscribe]
			from projects
			left outer join project_user_xref on pj_id = pu_project and $us = pu_user
			where isnull(pu_permission_level,$dpl) <> 0
			order by pj_name";

                this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));
                this.sql = this.sql.Replace("$dpl", Util.get_setting("DefaultPermissionLevel", "2"));

                var projects_dv = DbUtil.get_dataview(this.sql);

                this.project_auto_subscribe.DataSource = projects_dv;
                this.project_auto_subscribe.DataTextField = "pj_name";
                this.project_auto_subscribe.DataValueField = "pj_id";
                this.project_auto_subscribe.DataBind();

                // Get this entry's data from the db and fill in the form
                // MAW -- 2006/01/27 -- Converted to use new notification columns

                this.sql = @"select
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

                this.sql = this.sql.Replace("$id", Convert.ToString(this.id));

                var dr = DbUtil.get_datarow(this.sql);

                // Fill in this form
                this.firstname.Value = (string) dr["firstname"];
                this.lastname.Value = (string) dr["lastname"];
                this.bugs_per_page.Value = Convert.ToString(dr["us_bugs_per_page"]);

                if (Util.get_setting("DisableFCKEditor", "0") == "1")
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
                foreach (DataRowView drv in projects_dv)
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
                if (!Util.check_password_strength(this.pw.Value))
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

            if (!Util.is_int(this.bugs_per_page.Value))
            {
                good = false;
                this.bugs_per_page_err.InnerText =
                    Util.get_setting("PluralBugLabel", "Bugs") + " Per Page must be a number.";
            }
            else
            {
                this.bugs_per_page_err.InnerText = "";
            }

            this.email_err.InnerHtml = "";
            if (this.email.Value != "")
                if (!Util.validate_email(this.email.Value))
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
                this.sql = @"update users set
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

                this.sql = this.sql.Replace("$fn", this.firstname.Value.Replace("'", "''"));
                this.sql = this.sql.Replace("$ln", this.lastname.Value.Replace("'", "''"));
                this.sql = this.sql.Replace("$bp", this.bugs_per_page.Value.Replace("'", "''"));
                this.sql = this.sql.Replace("$fk", Util.bool_to_string(this.use_fckeditor.Checked));
                this.sql = this.sql.Replace("$pp", Util.bool_to_string(this.enable_popups.Checked));
                this.sql = this.sql.Replace("$em", this.email.Value.Replace("'", "''"));
                this.sql = this.sql.Replace("$en", Util.bool_to_string(this.enable_notifications.Checked));
                this.sql = this.sql.Replace("$ss", Util.bool_to_string(this.send_to_self.Checked));
                this.sql = this.sql.Replace("$rn", this.reported_notifications.SelectedItem.Value);
                this.sql = this.sql.Replace("$an", this.assigned_notifications.SelectedItem.Value);
                this.sql = this.sql.Replace("$sn", this.subscribed_notifications.SelectedItem.Value);
                this.sql = this.sql.Replace("$as", Util.bool_to_string(this.auto_subscribe.Checked));
                this.sql = this.sql.Replace("$ao", Util.bool_to_string(this.auto_subscribe_own.Checked));
                this.sql = this.sql.Replace("$ar", Util.bool_to_string(this.auto_subscribe_reported.Checked));
                this.sql = this.sql.Replace("$dq", this.query.SelectedItem.Value);
                this.sql = this.sql.Replace("$sg", this.signature.InnerText.Replace("'", "''"));
                this.sql = this.sql.Replace("$id", Convert.ToString(this.id));

                // update user
                DbUtil.execute_nonquery(this.sql);

                // update the password
                if (this.pw.Value != "") Util.update_user_password(this.id, this.pw.Value);

                // Now update project_user_xref

                // First turn everything off, then turn selected ones on.
                this.sql = @"update project_user_xref
				set pu_auto_subscribe = 0 where pu_user = $id";
                this.sql = this.sql.Replace("$id", Convert.ToString(this.id));
                DbUtil.execute_nonquery(this.sql);

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
                    this.sql = @"update project_user_xref
				set pu_auto_subscribe = 1 where pu_user = $id and pu_project in ($projects)

			insert into project_user_xref (pu_project, pu_user, pu_auto_subscribe)
				select pj_id, $id, 1
				from projects
				where pj_id in ($projects)
				and pj_id not in (select pu_project from project_user_xref where pu_user = $id)";

                    this.sql = this.sql.Replace("$id", Convert.ToString(this.id));
                    this.sql = this.sql.Replace("$projects", projects);
                    DbUtil.execute_nonquery(this.sql);
                }

                // apply subscriptions retroactively
                if (this.retroactive.Checked)
                {
                    this.sql = @"delete from bug_subscriptions where bs_user = $id;";
                    if (this.auto_subscribe.Checked)
                    {
                        this.sql += @"insert into bug_subscriptions (bs_bug, bs_user)
					select bg_id, $id from bugs;";
                    }
                    else
                    {
                        if (this.auto_subscribe_reported.Checked)
                            this.sql += @"insert into bug_subscriptions (bs_bug, bs_user)
						select bg_id, $id from bugs where bg_reported_user = $id
						and bg_id not in (select bs_bug from bug_subscriptions where bs_user = $id);";

                        if (this.auto_subscribe_own.Checked)
                            this.sql += @"insert into bug_subscriptions (bs_bug, bs_user)
						select bg_id, $id from bugs where bg_assigned_to_user = $id
						and bg_id not in (select bs_bug from bug_subscriptions where bs_user = $id);";

                        if (projects != "")
                            this.sql += @"insert into bug_subscriptions (bs_bug, bs_user)
						select bg_id, $id from bugs where bg_project in ($projects)
						and bg_id not in (select bs_bug from bug_subscriptions where bs_user = $id);";
                    }

                    this.sql = this.sql.Replace("$id", Convert.ToString(this.id));
                    this.sql = this.sql.Replace("$projects", projects);
                    DbUtil.execute_nonquery(this.sql);
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