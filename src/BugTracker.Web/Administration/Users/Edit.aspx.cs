/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Users
{
    using System;
    using System.Collections;
    using System.Data;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using BugTracker.Web.Core.Controls;
    using Core;

    public partial class Edit : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public bool Copy;
        public int Id;
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

            Page.Title = $"{ApplicationSettings.AppTitle} - edit user";

            if (!Security.User.IsAdmin)
            {
                // Check if the current user is an admin for any project
                this.Sql = @"select pu_project
            from project_user_xref
            where pu_user = $us
            and pu_admin = 1";
                this.Sql = this.Sql.Replace("$us", Convert.ToString(Security.User.Usid));
                var dsProjects = DbUtil.GetDataSet(this.Sql);

                if (dsProjects.Tables[0].Rows.Count == 0)
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

            if (Request["copy"] != null && Request["copy"] == "y") this.Copy = true;

            this.msg.InnerText = "";

            var var = Request.QueryString["id"];
            if (var == null)
            {
                this.Id = 0;
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
                this.Id = Convert.ToInt32(var);
            }

            if (!IsPostBack)
            {
                if (!Security.User.IsAdmin)
                {
                    // logged in user is a project level admin

                    // get values for permissions grid
                    // Table 0
                    this.Sql = @"
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

                    this.Sql = this.Sql.Replace("$this_usid", Convert.ToString(Security.User.Usid));
                }
                else // user is a real admin
                {
                    // Table 0

                    // populate permissions grid
                    this.Sql = @"
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

                this.Sql += @"/* populate query dropdown */
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

                if (Security.User.IsAdmin)
                {
                    this.Sql += @"/* populate org dropdown 1 */
                select og_id, og_name
                from orgs
                order by og_name;";
                }
                else
                {
                    if (Security.User.OtherOrgsPermissionLevel == SecurityPermissionLevel.PermissionAll)
                        this.Sql += @"/* populate org dropdown 2 */
                    select og_id, og_name
                    from orgs
                    where og_non_admins_can_use = 1
                    order by og_name;";
                    else
                        this.Sql += @"/* populate org dropdown 3 */
                    select 1; -- dummy";
                }

                // Table 3
                if (this.Id != 0)
                    // get existing user values

                    this.Sql += @"
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

                this.Sql = this.Sql.Replace("$us", Convert.ToString(this.Id));
                this.Sql = this.Sql.Replace("$dpl", ApplicationSettings.DefaultPermissionLevel.ToString());

                var ds = DbUtil.GetDataSet(this.Sql);

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
                if (Security.User.IsAdmin
                    || Security.User.OtherOrgsPermissionLevel == SecurityPermissionLevel.PermissionAll)
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
                        new ListItem(Security.User.OrgName, Convert.ToString(Security.User.Org)));
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
                if (this.Id == 0)
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
                    if (!Security.User.IsAdmin)
                    {
                        if (Security.User.Usid != (int)dr["us_created_user"])
                        {
                            Response.Write("You not allowed to edit this user, because you didn't create it.");
                            Response.End();
                        }
                        else if ((int)dr["us_admin"] == 1)
                        {
                            Response.Write("You not allowed to edit this user, because it is an admin.");
                            Response.End();
                        }
                    }

                    // select values in dropdowns

                    // select forced project
                    var currentForcedProject = (int)dr["us_forced_project"];
                    foreach (ListItem li in this.forced_project.Items)
                        if (Convert.ToInt32(li.Value) == currentForcedProject)
                        {
                            li.Selected = true;
                            break;
                        }

                    // Fill in this form
                    if (this.Copy)
                    {
                        this.username.Value = "Enter username here";
                        this.firstname.Value = "";
                        this.lastname.Value = "";
                        this.email.Value = "";
                        this.signature.InnerText = "";
                    }
                    else
                    {
                        this.username.Value = (string)dr["us_username"];
                        this.firstname.Value = (string)dr["us_firstname"];
                        this.lastname.Value = (string)dr["us_lastname"];
                        this.email.Value = (string)dr["us_email"];
                        this.signature.InnerText = (string)dr["us_signature"];
                    }

                    this.bugs_per_page.Value = Convert.ToString(dr["us_bugs_per_page"]);
                    this.use_fckeditor.Checked = Convert.ToBoolean((int)dr["us_use_fckeditor"]);
                    this.enable_popups.Checked = Convert.ToBoolean((int)dr["us_enable_bug_list_popups"]);
                    this.active.Checked = Convert.ToBoolean((int)dr["us_active"]);
                    this.admin.Checked = Convert.ToBoolean((int)dr["us_admin"]);
                    this.enable_notifications.Checked = Convert.ToBoolean((int)dr["us_enable_notifications"]);
                    this.send_to_self.Checked = Convert.ToBoolean((int)dr["us_send_notifications_to_self"]);
                    this.reported_notifications.Items[(int)dr["us_reported_notifications"]].Selected = true;
                    this.assigned_notifications.Items[(int)dr["us_assigned_notifications"]].Selected = true;
                    this.subscribed_notifications.Items[(int)dr["us_subscribed_notifications"]].Selected = true;
                    this.auto_subscribe.Checked = Convert.ToBoolean((int)dr["us_auto_subscribe"]);
                    this.auto_subscribe_own.Checked = Convert.ToBoolean((int)dr["us_auto_subscribe_own_bugs"]);
                    this.auto_subscribe_reported.Checked =
                        Convert.ToBoolean((int)dr["us_auto_subscribe_reported_bugs"]);

                    // org
                    foreach (ListItem li in this.org.Items)
                        if (Convert.ToInt32(li.Value) == (int)dr["us_org"])
                        {
                            li.Selected = true;
                            break;
                        }

                    // query
                    foreach (ListItem li in this.query.Items)
                        if (Convert.ToInt32(li.Value) == (int)dr["us_default_query"])
                        {
                            li.Selected = true;
                            break;
                        }

                    // select projects
                    foreach (DataRow dr2 in ds.Tables[0].Rows)
                        foreach (ListItem li in this.project_auto_subscribe.Items)
                            if (Convert.ToInt32(li.Value) == (int)dr2["pj_id"])
                            {
                                if ((int)dr2["pu_auto_subscribe"] == 1)
                                    li.Selected = true;
                                else
                                    li.Selected = false;
                            }

                    foreach (DataRow dr3 in ds.Tables[0].Rows)
                        foreach (ListItem li in this.project_admin.Items)
                            if (Convert.ToInt32(li.Value) == (int)dr3["pj_id"])
                            {
                                if ((int)dr3["pu_admin"] == 1)
                                    li.Selected = true;
                                else
                                    li.Selected = false;
                            }
                } // add or edit
            } // if !postback
            else
            {
                on_update(Security);
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
            if (this.Id == 0 || this.Copy)
                if (this.pw.Value == "")
                {
                    good = false;
                    this.pw_err.InnerText = "Password is required.";
                }

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

            if (this.org.SelectedItem.Text == "[select org]")
            {
                good = false;
                this.org_err.InnerText = "You must select a org";
            }
            else
            {
                this.org_err.InnerText = "";
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

        public string replace_vars_in_sql_statement(string sql, ISecurity security)
        {
            sql = sql.Replace("$un", this.username.Value.Replace("'", "''"));
            sql = sql.Replace("$fn", this.firstname.Value.Replace("'", "''"));
            sql = sql.Replace("$ln", this.lastname.Value.Replace("'", "''"));
            sql = sql.Replace("$bp", this.bugs_per_page.Value.Replace("'", "''"));
            sql = sql.Replace("$fk", Util.BoolToString(this.use_fckeditor.Checked));
            sql = sql.Replace("$pp", Util.BoolToString(this.enable_popups.Checked));
            sql = sql.Replace("$em", this.email.Value.Replace("'", "''"));
            sql = sql.Replace("$ac", Util.BoolToString(this.active.Checked));
            sql = sql.Replace("$en", Util.BoolToString(this.enable_notifications.Checked));
            sql = sql.Replace("$ss", Util.BoolToString(this.send_to_self.Checked));
            sql = sql.Replace("$rn", this.reported_notifications.SelectedItem.Value);
            sql = sql.Replace("$an", this.assigned_notifications.SelectedItem.Value);
            sql = sql.Replace("$sn", this.subscribed_notifications.SelectedItem.Value);
            sql = sql.Replace("$as", Util.BoolToString(this.auto_subscribe.Checked));
            sql = sql.Replace("$ao", Util.BoolToString(this.auto_subscribe_own.Checked));
            sql = sql.Replace("$ar", Util.BoolToString(this.auto_subscribe_reported.Checked));
            sql = sql.Replace("$dq", this.query.SelectedItem.Value);
            sql = sql.Replace("$org", this.org.SelectedItem.Value);
            sql = sql.Replace("$sg", this.signature.InnerText.Replace("'", "''"));
            sql = sql.Replace("$fp", this.forced_project.SelectedItem.Value);
            sql = sql.Replace("$id", Convert.ToString(this.Id));

            // only admins can create admins.
            if (Security.User.IsAdmin)
                sql = sql.Replace("$ad", Util.BoolToString(this.admin.Checked));
            else
                sql = sql.Replace("$ad", "0");

            return sql;
        }

        public void on_update(ISecurity security)
        {
            var good = validate();

            if (good)
            {
                if (this.Id == 0 || this.Copy) // insert new
                {
                    // See if the user already exists?
                    this.Sql = "select count(1) from users where us_username = N'$1'";
                    this.Sql = this.Sql.Replace("$1", this.username.Value.Replace("'", "''"));
                    var userCount = (int)DbUtil.ExecuteScalar(this.Sql);

                    if (userCount == 0)
                    {
                        // MAW -- 2006/01/27 -- Converted to use new notification columns
                        this.Sql = @"
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

                        this.Sql = replace_vars_in_sql_statement(this.Sql, security);
                        this.Sql = this.Sql.Replace("$createdby", Convert.ToString(Security.User.Usid));

                        // only admins can create admins.
                        if (Security.User.IsAdmin)
                            this.Sql = this.Sql.Replace("$ad", Util.BoolToString(this.admin.Checked));
                        else
                            this.Sql = this.Sql.Replace("$ad", "0");

                        // fill the password field with some junk, just temporarily.
                        this.Sql = this.Sql.Replace("$pw", Convert.ToString(new Random().Next()));

                        // insert the user
                        this.Id = Convert.ToInt32(DbUtil.ExecuteScalar(this.Sql));

                        // now encrypt the password and update the db
                        Util.UpdateUserPassword(this.Id, this.pw.Value);

                        update_project_user_xref(security);

                        Response.Redirect("~/Administration/Users/List.aspx");
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
                    this.Sql = @"select count(1)
                from users where us_username = N'$1' and us_id <> $2";
                    this.Sql = this.Sql.Replace("$1", this.username.Value.Replace("'", "''"));
                    this.Sql = this.Sql.Replace("$2", Convert.ToString(this.Id));
                    var userCount = (int)DbUtil.ExecuteScalar(this.Sql);

                    if (userCount == 0)
                    {
                        this.Sql = @"
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

                        this.Sql = replace_vars_in_sql_statement(this.Sql, security);

                        DbUtil.ExecuteNonQuery(this.Sql);

                        // update the password
                        if (this.pw.Value != "") Util.UpdateUserPassword(this.Id, this.pw.Value);

                        update_project_user_xref(security);

                        Response.Redirect("~/Administration/Users/List.aspx");
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
                if (this.Id == 0) // insert new
                    this.msg.InnerText = "User was not created.";
                else // edit existing
                    this.msg.InnerText = "User was not updated.";
            }
        }

        public void update_project_user_xref(ISecurity security)
        {
            var hashProjects = new Hashtable();

            foreach (ListItem li in this.project_auto_subscribe.Items)
            {
                var p = new Project();
                p.Id = Convert.ToInt32(li.Value);
                hashProjects[p.Id] = p;

                if (li.Selected)
                {
                    p.AutoSubscribe = 1;
                    p.MaybeInsert = true;
                }
                else
                {
                    p.AutoSubscribe = 0;
                }
            }

            foreach (ListItem li in this.project_admin.Items)
            {
                var p = (Project)hashProjects[Convert.ToInt32(li.Value)];
                if (li.Selected)
                {
                    p.Admin = 1;
                    p.MaybeInsert = true;
                }
                else
                {
                    p.Admin = 0;
                }
            }

            RadioButton rb;
            int permissionLevel;
            var defaultPermissionLevel = ApplicationSettings.DefaultPermissionLevel;

            foreach (DataGridItem dgi in this.MyDataGrid.Items)
            {
                rb = (RadioButton)dgi.FindControl("none");
                if (rb.Checked)
                {
                    permissionLevel = 0;
                }
                else
                {
                    rb = (RadioButton)dgi.FindControl("readonly");
                    if (rb.Checked)
                    {
                        permissionLevel = 1;
                    }
                    else
                    {
                        rb = (RadioButton)dgi.FindControl("reporter");
                        if (rb.Checked)
                            permissionLevel = 3;
                        else
                            permissionLevel = 2;
                    }
                }

                var pjId = Convert.ToInt32(dgi.Cells[1].Text);

                var p = (Project)hashProjects[pjId];
                p.PermissionLevel = permissionLevel;

                if (permissionLevel != defaultPermissionLevel) p.MaybeInsert = true;
            }

            var projects = "";
            foreach (Project p in hashProjects.Values)
                if (p.MaybeInsert)
                {
                    if (projects != "") projects += ",";

                    projects += Convert.ToString(p.Id);
                }

            this.Sql = "";

            // Insert new recs - we will update them later
            // Downstream logic is now simpler in that it just deals with existing recs
            if (projects != "")
            {
                this.Sql += @"
            insert into project_user_xref (pu_project, pu_user, pu_auto_subscribe)
            select pj_id, $us, 0
            from projects
            where pj_id in ($projects)
            and pj_id not in (select pu_project from project_user_xref where pu_user = $us);";
                this.Sql = this.Sql.Replace("$projects", projects);
            }

            // First turn everything off, then turn selected ones on.
            this.Sql += @"
        update project_user_xref
        set pu_auto_subscribe = 0,
        pu_admin = 0,
        pu_permission_level = $dpl
        where pu_user = $us;";

            projects = "";
            foreach (Project p in hashProjects.Values)
                if (p.AutoSubscribe == 1)
                {
                    if (projects != "") projects += ",";

                    projects += Convert.ToString(p.Id);
                }

            var autoSubscribeProjects = projects; // save for later

            if (projects != "")
            {
                this.Sql += @"
            update project_user_xref
            set pu_auto_subscribe = 1
            where pu_user = $us
            and pu_project in ($projects);";
                this.Sql = this.Sql.Replace("$projects", projects);
            }

            if (Security.User.IsAdmin)
            {
                projects = "";
                foreach (Project p in hashProjects.Values)
                    if (p.Admin == 1)
                    {
                        if (projects != "") projects += ",";

                        projects += Convert.ToString(p.Id);
                    }

                if (projects != "")
                {
                    this.Sql += @"
                update project_user_xref
                set pu_admin = 1
                where pu_user = $us
                and pu_project in ($projects);";

                    this.Sql = this.Sql.Replace("$projects", projects);
                }
            }

            // update permission levels to 0
            projects = "";
            foreach (Project p in hashProjects.Values)
                if (p.PermissionLevel == 0)
                {
                    if (projects != "") projects += ",";

                    projects += Convert.ToString(p.Id);
                }

            if (projects != "")
            {
                this.Sql += @"
            update project_user_xref
            set pu_permission_level = 0
            where pu_user = $us
            and pu_project in ($projects);";

                this.Sql = this.Sql.Replace("$projects", projects);
            }

            // update permission levels to 1
            projects = "";
            foreach (Project p in hashProjects.Values)
                if (p.PermissionLevel == 1)
                {
                    if (projects != "") projects += ",";

                    projects += Convert.ToString(p.Id);
                }

            if (projects != "")
            {
                this.Sql += @"
            update project_user_xref
            set pu_permission_level = 1
            where pu_user = $us
            and pu_project in ($projects);";

                this.Sql = this.Sql.Replace("$projects", projects);
            }

            // update permission levels to 2
            projects = "";
            foreach (Project p in hashProjects.Values)
                if (p.PermissionLevel == 2)
                {
                    if (projects != "") projects += ",";

                    projects += Convert.ToString(p.Id);
                }

            if (projects != "")
            {
                this.Sql += @"
            update project_user_xref
            set pu_permission_level = 2
            where pu_user = $us
            and pu_project in ($projects);";

                this.Sql = this.Sql.Replace("$projects", projects);
            }

            // update permission levels to 3
            projects = "";
            foreach (Project p in hashProjects.Values)
                if (p.PermissionLevel == 3)
                {
                    if (projects != "") projects += ",";

                    projects += Convert.ToString(p.Id);
                }

            if (projects != "")
            {
                this.Sql += @"
            update project_user_xref
            set pu_permission_level = 3
            where pu_user = $us
            and pu_project in ($projects);";

                this.Sql = this.Sql.Replace("$projects", projects);
            }

            // apply subscriptions retroactively
            if (this.retroactive.Checked)
            {
                this.Sql = @"
            delete from bug_subscriptions where bs_user = $us;";

                if (this.auto_subscribe.Checked)
                {
                    this.Sql += @"
            insert into bug_subscriptions (bs_bug, bs_user)
                select bg_id, $us from bugs;";
                }
                else
                {
                    if (this.auto_subscribe_reported.Checked)
                        this.Sql += @"
                    insert into bug_subscriptions (bs_bug, bs_user)
                    select bg_id, $us from bugs where bg_reported_user = $us
                    and bg_id not in (select bs_bug from bug_subscriptions where bs_user = $us);";

                    if (this.auto_subscribe_own.Checked)
                        this.Sql += @"
                    insert into bug_subscriptions (bs_bug, bs_user)
                    select bg_id, $us from bugs where bg_assigned_to_user = $us
                    and bg_id not in (select bs_bug from bug_subscriptions where bs_user = $us);";

                    if (autoSubscribeProjects != "")
                    {
                        this.Sql += @"
                    insert into bug_subscriptions (bs_bug, bs_user)
                    select bg_id, $us from bugs where bg_project in ($projects)
                    and bg_id not in (select bs_bug from bug_subscriptions where bs_user = $us);";
                        this.Sql = this.Sql.Replace("$projects", autoSubscribeProjects);
                    }
                }
            }

            this.Sql = this.Sql.Replace("$us", Convert.ToString(this.Id));
            this.Sql = this.Sql.Replace("$dpl", ApplicationSettings.DefaultPermissionLevel.ToString());
            DbUtil.ExecuteNonQuery(this.Sql);
        }

        private class Project
        {
            public int Admin;
            public int AutoSubscribe;
            public int Id;
            public bool MaybeInsert;
            public int PermissionLevel;
        }
    }
}