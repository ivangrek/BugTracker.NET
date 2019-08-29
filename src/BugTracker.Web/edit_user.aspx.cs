/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Collections;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using Core;

    public partial class edit_user : Page
    {
        public bool copy;
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
            this.security.check_security(HttpContext.Current, Security.MUST_BE_ADMIN_OR_PROJECT_ADMIN);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "edit user";

            if (!this.security.user.is_admin)
            {
                // Check if the current user is an admin for any project
                this.sql = @"select pu_project
			from project_user_xref
			where pu_user = $us
			and pu_admin = 1";
                this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));
                var ds_projects = DbUtil.get_dataset(this.sql);

                if (ds_projects.Tables[0].Rows.Count == 0)
                {
                    Response.Write("You not allowed to add users.");
                    Response.End();
                }

                this.admin.Visible = false;
                this.admin_label.Visible = false;
                this.project_admin_label.Visible = false;
                this.project_admin.Visible = false;
                this.project_admin_help.Visible = false;
            }

            if (Request["copy"] != null && Request["copy"] == "y") this.copy = true;

            this.msg.InnerText = "";

            var var = Request.QueryString["id"];
            if (var == null)
            {
                this.id = 0;
                // MAW -- 2006/01/27 -- Set default settings when adding a new user
                this.auto_subscribe_own.Checked = true;
                this.auto_subscribe_reported.Checked = true;
                this.enable_popups.Checked = true;
                this.reported_notifications.Items[4].Selected = true;
                this.assigned_notifications.Items[4].Selected = true;
                this.subscribed_notifications.Items[4].Selected = true;
            }
            else
            {
                this.id = Convert.ToInt32(var);
            }

            if (!IsPostBack)
            {
                if (!this.security.user.is_admin)
                {
                    // logged in user is a project level admin

                    // get values for permissions grid
                    // Table 0
                    this.sql = @"
				select pj_id, pj_name,
				isnull(a.pu_permission_level,$dpl) [pu_permission_level],
				isnull(a.pu_auto_subscribe,0) [pu_auto_subscribe],
				isnull(a.pu_admin,0) [pu_admin]
				from projects
				inner join project_user_xref project_admin on pj_id = project_admin.pu_project
				and project_admin.pu_user = $this_usid
				and project_admin.pu_admin = 1
				left outer join project_user_xref a on pj_id = a.pu_project
				and a.pu_user = $us
				order by pj_name;";

                    this.sql = this.sql.Replace("$this_usid", Convert.ToString(this.security.user.usid));
                }
                else // user is a real admin
                {
                    // Table 0

                    // populate permissions grid
                    this.sql = @"
				select pj_id, pj_name,
				isnull(pu_permission_level,$dpl) [pu_permission_level],
				isnull(pu_auto_subscribe,0) [pu_auto_subscribe],
				isnull(pu_admin,0) [pu_admin]
				from projects
				left outer join project_user_xref on pj_id = pu_project
				and pu_user = $us
				order by pj_name;";
                }

                // Table 1

                this.sql += @"/* populate query dropdown */
		    declare @org int
		    set @org = null
		    select @org = us_org from users where us_id = $us

			select qu_id, qu_desc
			from queries
			where (isnull(qu_user,0) = 0 and isnull(qu_org,0) = 0)
			or isnull(qu_user,0) = $us
			or isnull(qu_org,0) = isnull(@org,-1)
			order by qu_desc;";

                // Table 2

                if (this.security.user.is_admin)
                {
                    this.sql += @"/* populate org dropdown 1 */
				select og_id, og_name
				from orgs
				order by og_name;";
                }
                else
                {
                    if (this.security.user.other_orgs_permission_level == Security.PERMISSION_ALL)
                        this.sql += @"/* populate org dropdown 2 */
					select og_id, og_name
					from orgs
					where og_non_admins_can_use = 1
					order by og_name;";
                    else
                        this.sql += @"/* populate org dropdown 3 */
					select 1; -- dummy";
                }

                // Table 3
                if (this.id != 0)
                    // get existing user values

                    this.sql += @"
			select
				us_username,
				isnull(us_firstname,'') [us_firstname],
				isnull(us_lastname,'') [us_lastname],
				isnull(us_bugs_per_page,10) [us_bugs_per_page],
				us_use_fckeditor,
				us_enable_bug_list_popups,
				isnull(us_email,'') [us_email],
				us_active,
				us_admin,
				us_enable_notifications,
				us_send_notifications_to_self,
                us_reported_notifications,
                us_assigned_notifications,
                us_subscribed_notifications,
				us_auto_subscribe,
				us_auto_subscribe_own_bugs,
				us_auto_subscribe_reported_bugs,
				us_default_query,
				us_org,
				isnull(us_signature,'') [us_signature],
				isnull(us_forced_project,0) [us_forced_project],
				us_created_user
				from users
				where us_id = $us";

                this.sql = this.sql.Replace("$us", Convert.ToString(this.id));
                this.sql = this.sql.Replace("$dpl", Util.get_setting("DefaultPermissionLevel", "2"));

                var ds = DbUtil.get_dataset(this.sql);

                // query dropdown
                this.query.DataSource = ds.Tables[1].DefaultView;
                this.query.DataTextField = "qu_desc";
                this.query.DataValueField = "qu_id";
                this.query.DataBind();

                // forced project dropdown
                this.forced_project.DataSource = ds.Tables[0].DefaultView;
                this.forced_project.DataTextField = "pj_name";
                this.forced_project.DataValueField = "pj_id";
                this.forced_project.DataBind();
                this.forced_project.Items.Insert(0, new ListItem("[no forced project]", "0"));

                // org dropdown
                if (this.security.user.is_admin
                    || this.security.user.other_orgs_permission_level == Security.PERMISSION_ALL)
                {
                    this.org.DataSource = ds.Tables[2].DefaultView;
                    this.org.DataTextField = "og_name";
                    this.org.DataValueField = "og_id";
                    this.org.DataBind();
                    this.org.Items.Insert(0, new ListItem("[select org]", "0"));
                }
                else
                {
                    this.org.Items.Insert(0,
                        new ListItem(this.security.user.org_name, Convert.ToString(this.security.user.org)));
                }

                // populate permissions grid
                this.MyDataGrid.DataSource = ds.Tables[0].DefaultView;
                this.MyDataGrid.DataBind();

                // subscribe by project dropdown
                this.project_auto_subscribe.DataSource = ds.Tables[0].DefaultView;
                this.project_auto_subscribe.DataTextField = "pj_name";
                this.project_auto_subscribe.DataValueField = "pj_id";
                this.project_auto_subscribe.DataBind();

                // project admin dropdown
                this.project_admin.DataSource = ds.Tables[0].DefaultView;
                this.project_admin.DataTextField = "pj_name";
                this.project_admin.DataValueField = "pj_id";
                this.project_admin.DataBind();

                // add or edit?
                if (this.id == 0)
                {
                    this.sub.Value = "Create";
                    this.bugs_per_page.Value = "10";
                    this.active.Checked = true;
                    this.enable_notifications.Checked = true;
                }
                else
                {
                    this.sub.Value = "Update";

                    // get the values for this existing user
                    var dr = ds.Tables[3].Rows[0];

                    // check if project admin is allowed to edit this user
                    if (!this.security.user.is_admin)
                    {
                        if (this.security.user.usid != (int) dr["us_created_user"])
                        {
                            Response.Write("You not allowed to edit this user, because you didn't create it.");
                            Response.End();
                        }
                        else if ((int) dr["us_admin"] == 1)
                        {
                            Response.Write("You not allowed to edit this user, because it is an admin.");
                            Response.End();
                        }
                    }

                    // select values in dropdowns

                    // select forced project
                    var current_forced_project = (int) dr["us_forced_project"];
                    foreach (ListItem li in this.forced_project.Items)
                        if (Convert.ToInt32(li.Value) == current_forced_project)
                        {
                            li.Selected = true;
                            break;
                        }

                    // Fill in this form
                    if (this.copy)
                    {
                        this.username.Value = "Enter username here";
                        this.firstname.Value = "";
                        this.lastname.Value = "";
                        this.email.Value = "";
                        this.signature.InnerText = "";
                    }
                    else
                    {
                        this.username.Value = (string) dr["us_username"];
                        this.firstname.Value = (string) dr["us_firstname"];
                        this.lastname.Value = (string) dr["us_lastname"];
                        this.email.Value = (string) dr["us_email"];
                        this.signature.InnerText = (string) dr["us_signature"];
                    }

                    this.bugs_per_page.Value = Convert.ToString(dr["us_bugs_per_page"]);
                    this.use_fckeditor.Checked = Convert.ToBoolean((int) dr["us_use_fckeditor"]);
                    this.enable_popups.Checked = Convert.ToBoolean((int) dr["us_enable_bug_list_popups"]);
                    this.active.Checked = Convert.ToBoolean((int) dr["us_active"]);
                    this.admin.Checked = Convert.ToBoolean((int) dr["us_admin"]);
                    this.enable_notifications.Checked = Convert.ToBoolean((int) dr["us_enable_notifications"]);
                    this.send_to_self.Checked = Convert.ToBoolean((int) dr["us_send_notifications_to_self"]);
                    this.reported_notifications.Items[(int) dr["us_reported_notifications"]].Selected = true;
                    this.assigned_notifications.Items[(int) dr["us_assigned_notifications"]].Selected = true;
                    this.subscribed_notifications.Items[(int) dr["us_subscribed_notifications"]].Selected = true;
                    this.auto_subscribe.Checked = Convert.ToBoolean((int) dr["us_auto_subscribe"]);
                    this.auto_subscribe_own.Checked = Convert.ToBoolean((int) dr["us_auto_subscribe_own_bugs"]);
                    this.auto_subscribe_reported.Checked =
                        Convert.ToBoolean((int) dr["us_auto_subscribe_reported_bugs"]);

                    // org
                    foreach (ListItem li in this.org.Items)
                        if (Convert.ToInt32(li.Value) == (int) dr["us_org"])
                        {
                            li.Selected = true;
                            break;
                        }

                    // query
                    foreach (ListItem li in this.query.Items)
                        if (Convert.ToInt32(li.Value) == (int) dr["us_default_query"])
                        {
                            li.Selected = true;
                            break;
                        }

                    // select projects
                    foreach (DataRow dr2 in ds.Tables[0].Rows)
                    foreach (ListItem li in this.project_auto_subscribe.Items)
                        if (Convert.ToInt32(li.Value) == (int) dr2["pj_id"])
                        {
                            if ((int) dr2["pu_auto_subscribe"] == 1)
                                li.Selected = true;
                            else
                                li.Selected = false;
                        }

                    foreach (DataRow dr3 in ds.Tables[0].Rows)
                    foreach (ListItem li in this.project_admin.Items)
                        if (Convert.ToInt32(li.Value) == (int) dr3["pj_id"])
                        {
                            if ((int) dr3["pu_admin"] == 1)
                                li.Selected = true;
                            else
                                li.Selected = false;
                        }
                } // add or edit
            } // if !postback
            else
            {
                on_update();
            }
        }

        public bool validate()
        {
            var good = true;
            if (this.username.Value == "")
            {
                good = false;
                this.username_err.InnerText = "Username is required.";
            }
            else
            {
                this.username_err.InnerText = "";
            }

            this.pw_err.InnerText = "";
            if (this.id == 0 || this.copy)
                if (this.pw.Value == "")
                {
                    good = false;
                    this.pw_err.InnerText = "Password is required.";
                }

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

            if (this.org.SelectedItem.Text == "[select org]")
            {
                good = false;
                this.org_err.InnerText = "You must select a org";
            }
            else
            {
                this.org_err.InnerText = "";
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

        public string replace_vars_in_sql_statement(string sql)
        {
            sql = sql.Replace("$un", this.username.Value.Replace("'", "''"));
            sql = sql.Replace("$fn", this.firstname.Value.Replace("'", "''"));
            sql = sql.Replace("$ln", this.lastname.Value.Replace("'", "''"));
            sql = sql.Replace("$bp", this.bugs_per_page.Value.Replace("'", "''"));
            sql = sql.Replace("$fk", Util.bool_to_string(this.use_fckeditor.Checked));
            sql = sql.Replace("$pp", Util.bool_to_string(this.enable_popups.Checked));
            sql = sql.Replace("$em", this.email.Value.Replace("'", "''"));
            sql = sql.Replace("$ac", Util.bool_to_string(this.active.Checked));
            sql = sql.Replace("$en", Util.bool_to_string(this.enable_notifications.Checked));
            sql = sql.Replace("$ss", Util.bool_to_string(this.send_to_self.Checked));
            sql = sql.Replace("$rn", this.reported_notifications.SelectedItem.Value);
            sql = sql.Replace("$an", this.assigned_notifications.SelectedItem.Value);
            sql = sql.Replace("$sn", this.subscribed_notifications.SelectedItem.Value);
            sql = sql.Replace("$as", Util.bool_to_string(this.auto_subscribe.Checked));
            sql = sql.Replace("$ao", Util.bool_to_string(this.auto_subscribe_own.Checked));
            sql = sql.Replace("$ar", Util.bool_to_string(this.auto_subscribe_reported.Checked));
            sql = sql.Replace("$dq", this.query.SelectedItem.Value);
            sql = sql.Replace("$org", this.org.SelectedItem.Value);
            sql = sql.Replace("$sg", this.signature.InnerText.Replace("'", "''"));
            sql = sql.Replace("$fp", this.forced_project.SelectedItem.Value);
            sql = sql.Replace("$id", Convert.ToString(this.id));

            // only admins can create admins.
            if (this.security.user.is_admin)
                sql = sql.Replace("$ad", Util.bool_to_string(this.admin.Checked));
            else
                sql = sql.Replace("$ad", "0");

            return sql;
        }

        public void on_update()
        {
            var good = validate();

            if (good)
            {
                if (this.id == 0 || this.copy) // insert new
                {
                    // See if the user already exists?
                    this.sql = "select count(1) from users where us_username = N'$1'";
                    this.sql = this.sql.Replace("$1", this.username.Value.Replace("'", "''"));
                    var user_count = (int) DbUtil.execute_scalar(this.sql);

                    if (user_count == 0)
                    {
                        // MAW -- 2006/01/27 -- Converted to use new notification columns
                        this.sql = @"
insert into users
(us_username, us_password,
us_firstname, us_lastname,
us_bugs_per_page,
us_use_fckeditor,
us_enable_bug_list_popups,
us_email,
us_active, us_admin,
us_enable_notifications,
us_send_notifications_to_self,
us_reported_notifications,
us_assigned_notifications,
us_subscribed_notifications,
us_auto_subscribe,
us_auto_subscribe_own_bugs,
us_auto_subscribe_reported_bugs,
us_default_query,
us_org,
us_signature,
us_forced_project,
us_created_user)

values (
N'$un', N'$pw', N'$fn', N'$ln',
$bp, $fk, $pp, N'$em',
$ac, $ad, $en,  $ss,
$rn, $an, $sn, $as,
$ao, $ar, $dq, $org, N'$sg',
$fp,
$createdby
);

select scope_identity()";

                        this.sql = replace_vars_in_sql_statement(this.sql);
                        this.sql = this.sql.Replace("$createdby", Convert.ToString(this.security.user.usid));

                        // only admins can create admins.
                        if (this.security.user.is_admin)
                            this.sql = this.sql.Replace("$ad", Util.bool_to_string(this.admin.Checked));
                        else
                            this.sql = this.sql.Replace("$ad", "0");

                        // fill the password field with some junk, just temporarily.
                        this.sql = this.sql.Replace("$pw", Convert.ToString(new Random().Next()));

                        // insert the user
                        this.id = Convert.ToInt32(DbUtil.execute_scalar(this.sql));

                        // now encrypt the password and update the db
                        Util.update_user_password(this.id, this.pw.Value);

                        update_project_user_xref();

                        Server.Transfer("users.aspx");
                    }
                    else
                    {
                        this.username_err.InnerText = "User already exists.   Choose another username.";
                        this.msg.InnerText = "User was not created.";
                    }
                }
                else // edit existing
                {
                    // See if the user already exists?
                    this.sql = @"select count(1)
				from users where us_username = N'$1' and us_id <> $2";
                    this.sql = this.sql.Replace("$1", this.username.Value.Replace("'", "''"));
                    this.sql = this.sql.Replace("$2", Convert.ToString(this.id));
                    var user_count = (int) DbUtil.execute_scalar(this.sql);

                    if (user_count == 0)
                    {
                        this.sql = @"
update users set
us_username = N'$un',
us_firstname = N'$fn',
us_lastname = N'$ln',
us_bugs_per_page = N'$bp',
us_use_fckeditor = $fk,
us_enable_bug_list_popups = $pp,
us_email = N'$em',
us_active = $ac,
us_admin = $ad,
us_enable_notifications = $en,
us_send_notifications_to_self = $ss,
us_reported_notifications = $rn,
us_assigned_notifications = $an,
us_subscribed_notifications = $sn,
us_auto_subscribe = $as,
us_auto_subscribe_own_bugs = $ao,
us_auto_subscribe_reported_bugs = $ar,
us_default_query = $dq,
us_org = $org,
us_signature = N'$sg',
us_forced_project = $fp
where us_id = $id";

                        this.sql = replace_vars_in_sql_statement(this.sql);

                        DbUtil.execute_nonquery(this.sql);

                        // update the password
                        if (this.pw.Value != "") Util.update_user_password(this.id, this.pw.Value);

                        update_project_user_xref();

                        Server.Transfer("users.aspx");
                    }
                    else
                    {
                        this.username_err.InnerText = "Username already exists.   Choose another username.";
                        this.msg.InnerText = "User was not updated.";
                    }
                }
            }
            else
            {
                if (this.id == 0) // insert new
                    this.msg.InnerText = "User was not created.";
                else // edit existing
                    this.msg.InnerText = "User was not updated.";
            }
        }

        public void update_project_user_xref()
        {
            var hash_projects = new Hashtable();

            foreach (ListItem li in this.project_auto_subscribe.Items)
            {
                var p = new Project();
                p.id = Convert.ToInt32(li.Value);
                hash_projects[p.id] = p;

                if (li.Selected)
                {
                    p.auto_subscribe = 1;
                    p.maybe_insert = true;
                }
                else
                {
                    p.auto_subscribe = 0;
                }
            }

            foreach (ListItem li in this.project_admin.Items)
            {
                var p = (Project) hash_projects[Convert.ToInt32(li.Value)];
                if (li.Selected)
                {
                    p.admin = 1;
                    p.maybe_insert = true;
                }
                else
                {
                    p.admin = 0;
                }
            }

            RadioButton rb;
            int permission_level;
            var default_permission_level = Convert.ToInt32(Util.get_setting("DefaultPermissionLevel", "2"));

            foreach (DataGridItem dgi in this.MyDataGrid.Items)
            {
                rb = (RadioButton) dgi.FindControl("none");
                if (rb.Checked)
                {
                    permission_level = 0;
                }
                else
                {
                    rb = (RadioButton) dgi.FindControl("readonly");
                    if (rb.Checked)
                    {
                        permission_level = 1;
                    }
                    else
                    {
                        rb = (RadioButton) dgi.FindControl("reporter");
                        if (rb.Checked)
                            permission_level = 3;
                        else
                            permission_level = 2;
                    }
                }

                var pj_id = Convert.ToInt32(dgi.Cells[1].Text);

                var p = (Project) hash_projects[pj_id];
                p.permission_level = permission_level;

                if (permission_level != default_permission_level) p.maybe_insert = true;
            }

            var projects = "";
            foreach (Project p in hash_projects.Values)
                if (p.maybe_insert)
                {
                    if (projects != "") projects += ",";

                    projects += Convert.ToString(p.id);
                }

            this.sql = "";

            // Insert new recs - we will update them later
            // Downstream logic is now simpler in that it just deals with existing recs
            if (projects != "")
            {
                this.sql += @"
			insert into project_user_xref (pu_project, pu_user, pu_auto_subscribe)
			select pj_id, $us, 0
			from projects
			where pj_id in ($projects)
			and pj_id not in (select pu_project from project_user_xref where pu_user = $us);";
                this.sql = this.sql.Replace("$projects", projects);
            }

            // First turn everything off, then turn selected ones on.
            this.sql += @"
		update project_user_xref
		set pu_auto_subscribe = 0,
		pu_admin = 0,
		pu_permission_level = $dpl
		where pu_user = $us;";

            projects = "";
            foreach (Project p in hash_projects.Values)
                if (p.auto_subscribe == 1)
                {
                    if (projects != "") projects += ",";

                    projects += Convert.ToString(p.id);
                }

            var auto_subscribe_projects = projects; // save for later

            if (projects != "")
            {
                this.sql += @"
			update project_user_xref
			set pu_auto_subscribe = 1
			where pu_user = $us
			and pu_project in ($projects);";
                this.sql = this.sql.Replace("$projects", projects);
            }

            if (this.security.user.is_admin)
            {
                projects = "";
                foreach (Project p in hash_projects.Values)
                    if (p.admin == 1)
                    {
                        if (projects != "") projects += ",";

                        projects += Convert.ToString(p.id);
                    }

                if (projects != "")
                {
                    this.sql += @"
				update project_user_xref
				set pu_admin = 1
				where pu_user = $us
				and pu_project in ($projects);";

                    this.sql = this.sql.Replace("$projects", projects);
                }
            }

            // update permission levels to 0
            projects = "";
            foreach (Project p in hash_projects.Values)
                if (p.permission_level == 0)
                {
                    if (projects != "") projects += ",";

                    projects += Convert.ToString(p.id);
                }

            if (projects != "")
            {
                this.sql += @"
			update project_user_xref
			set pu_permission_level = 0
			where pu_user = $us
			and pu_project in ($projects);";

                this.sql = this.sql.Replace("$projects", projects);
            }

            // update permission levels to 1
            projects = "";
            foreach (Project p in hash_projects.Values)
                if (p.permission_level == 1)
                {
                    if (projects != "") projects += ",";

                    projects += Convert.ToString(p.id);
                }

            if (projects != "")
            {
                this.sql += @"
			update project_user_xref
			set pu_permission_level = 1
			where pu_user = $us
			and pu_project in ($projects);";

                this.sql = this.sql.Replace("$projects", projects);
            }

            // update permission levels to 2
            projects = "";
            foreach (Project p in hash_projects.Values)
                if (p.permission_level == 2)
                {
                    if (projects != "") projects += ",";

                    projects += Convert.ToString(p.id);
                }

            if (projects != "")
            {
                this.sql += @"
			update project_user_xref
			set pu_permission_level = 2
			where pu_user = $us
			and pu_project in ($projects);";

                this.sql = this.sql.Replace("$projects", projects);
            }

            // update permission levels to 3
            projects = "";
            foreach (Project p in hash_projects.Values)
                if (p.permission_level == 3)
                {
                    if (projects != "") projects += ",";

                    projects += Convert.ToString(p.id);
                }

            if (projects != "")
            {
                this.sql += @"
			update project_user_xref
			set pu_permission_level = 3
			where pu_user = $us
			and pu_project in ($projects);";

                this.sql = this.sql.Replace("$projects", projects);
            }

            // apply subscriptions retroactively
            if (this.retroactive.Checked)
            {
                this.sql = @"
			delete from bug_subscriptions where bs_user = $us;";

                if (this.auto_subscribe.Checked)
                {
                    this.sql += @"
			insert into bug_subscriptions (bs_bug, bs_user)
				select bg_id, $us from bugs;";
                }
                else
                {
                    if (this.auto_subscribe_reported.Checked)
                        this.sql += @"
					insert into bug_subscriptions (bs_bug, bs_user)
					select bg_id, $us from bugs where bg_reported_user = $us
					and bg_id not in (select bs_bug from bug_subscriptions where bs_user = $us);";

                    if (this.auto_subscribe_own.Checked)
                        this.sql += @"
					insert into bug_subscriptions (bs_bug, bs_user)
					select bg_id, $us from bugs where bg_assigned_to_user = $us
					and bg_id not in (select bs_bug from bug_subscriptions where bs_user = $us);";

                    if (auto_subscribe_projects != "")
                    {
                        this.sql += @"
					insert into bug_subscriptions (bs_bug, bs_user)
					select bg_id, $us from bugs where bg_project in ($projects)
					and bg_id not in (select bs_bug from bug_subscriptions where bs_user = $us);";
                        this.sql = this.sql.Replace("$projects", auto_subscribe_projects);
                    }
                }
            }

            this.sql = this.sql.Replace("$us", Convert.ToString(this.id));
            this.sql = this.sql.Replace("$dpl", Convert.ToString(default_permission_level));
            DbUtil.execute_nonquery(this.sql);
        }

        private class Project
        {
            public int admin;
            public int auto_subscribe;
            public int id;
            public bool maybe_insert;
            public int permission_level;
        }
    }
}