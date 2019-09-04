/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Organizations
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Web.UI;
    using Core;

    public partial class Edit : Page
    {
        public Dictionary<string, int> DictCustomFieldPermissionLevel = new Dictionary<string, int>();
        public DataSet DsCustom;
        public int Id;

        public string RadioTemplate = @"
<tr>
    <td>""$name$"" field permission
    <td colspan=2>
        <table id='$name$_field' border='0'>
        <tr>
        <td>
            <span ID='$name$0'><input id='$name$_field_0' type='radio' name='$name$' value='0' $checked0$/><label for='$name$_field_0'>none</label></span>
        </td>

        <td>
            <span ID='$name$1'><input id='$name$_field_1' type='radio' name='$name$' value='1' $checked1$/><label for='$name$_field_1'>view only</label></span>
        </td>
        <td>
            <span ID='$name$2'><input id='$name$_field_2' type='radio' name='$name$' value='2' $checked2$ /><label for='$name$_field_2'>edit</label></span>
        </td>
        </tr>
        </table>
<tr>";

        public string Sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.MustBeAdmin);

            MainMenu.Security = security;
            MainMenu.SelectedItem = "admin";

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - edit organization";

            this.msg.InnerText = "";

            var var = Request.QueryString["id"];
            if (var == null)
                this.Id = 0;
            else
                this.Id = Convert.ToInt32(var);

            this.DsCustom = Util.GetCustomColumns();

            if (!IsPostBack)
            {
                // add or edit?
                if (this.Id == 0)
                {
                    this.sub.Value = "Create";
                    this.og_active.Checked = true;
                    //other_orgs_permission_level.SelectedIndex = 2;
                    this.can_search.Checked = true;
                    this.can_be_assigned_to.Checked = true;
                    this.other_orgs.SelectedValue = "2";

                    this.project_field.SelectedValue = "2";
                    this.org_field.SelectedValue = "2";
                    this.category_field.SelectedValue = "2";
                    this.tags_field.SelectedValue = "2";
                    this.priority_field.SelectedValue = "2";
                    this.status_field.SelectedValue = "2";
                    this.assigned_to_field.SelectedValue = "2";
                    this.udf_field.SelectedValue = "2";

                    foreach (DataRow drCustom in this.DsCustom.Tables[0].Rows)
                    {
                        var bgName = (string) drCustom["name"];
                        this.DictCustomFieldPermissionLevel[bgName] = 2;
                    }
                }
                else
                {
                    this.sub.Value = "Update";

                    // Get this entry's data from the db and fill in the form

                    this.Sql = @"select *,isnull(og_domain,'') og_domain2 from orgs where og_id = $og_id";
                    this.Sql = this.Sql.Replace("$og_id", Convert.ToString(this.Id));
                    var dr = DbUtil.GetDataRow(this.Sql);

                    // Fill in this form
                    this.og_name.Value = (string) dr["og_name"];
                    this.og_domain.Value = (string) dr["og_domain2"];
                    this.og_active.Checked = Convert.ToBoolean((int) dr["og_active"]);
                    this.non_admins_can_use.Checked = Convert.ToBoolean((int) dr["og_non_admins_can_use"]);
                    this.external_user.Checked = Convert.ToBoolean((int) dr["og_external_user"]);
                    this.can_edit_sql.Checked = Convert.ToBoolean((int) dr["og_can_edit_sql"]);
                    this.can_delete_bug.Checked = Convert.ToBoolean((int) dr["og_can_delete_bug"]);
                    this.can_edit_and_delete_posts.Checked =
                        Convert.ToBoolean((int) dr["og_can_edit_and_delete_posts"]);
                    this.can_merge_bugs.Checked = Convert.ToBoolean((int) dr["og_can_merge_bugs"]);
                    this.can_mass_edit_bugs.Checked = Convert.ToBoolean((int) dr["og_can_mass_edit_bugs"]);
                    this.can_use_reports.Checked = Convert.ToBoolean((int) dr["og_can_use_reports"]);
                    this.can_edit_reports.Checked = Convert.ToBoolean((int) dr["og_can_edit_reports"]);
                    this.can_be_assigned_to.Checked = Convert.ToBoolean((int) dr["og_can_be_assigned_to"]);
                    this.can_view_tasks.Checked = Convert.ToBoolean((int) dr["og_can_view_tasks"]);
                    this.can_edit_tasks.Checked = Convert.ToBoolean((int) dr["og_can_edit_tasks"]);
                    this.can_search.Checked = Convert.ToBoolean((int) dr["og_can_search"]);
                    this.can_only_see_own_reported.Checked =
                        Convert.ToBoolean((int) dr["og_can_only_see_own_reported"]);
                    this.can_assign_to_internal_users.Checked =
                        Convert.ToBoolean((int) dr["og_can_assign_to_internal_users"]);

                    this.other_orgs.SelectedValue = Convert.ToString((int) dr["og_other_orgs_permission_level"]);

                    this.project_field.SelectedValue = Convert.ToString((int) dr["og_project_field_permission_level"]);
                    this.org_field.SelectedValue = Convert.ToString((int) dr["og_org_field_permission_level"]);
                    this.category_field.SelectedValue =
                        Convert.ToString((int) dr["og_category_field_permission_level"]);
                    this.tags_field.SelectedValue = Convert.ToString((int) dr["og_tags_field_permission_level"]);
                    this.priority_field.SelectedValue =
                        Convert.ToString((int) dr["og_priority_field_permission_level"]);
                    this.status_field.SelectedValue = Convert.ToString((int) dr["og_status_field_permission_level"]);
                    this.assigned_to_field.SelectedValue =
                        Convert.ToString((int) dr["og_assigned_to_field_permission_level"]);
                    this.udf_field.SelectedValue = Convert.ToString((int) dr["og_udf_field_permission_level"]);

                    foreach (DataRow drCustom in this.DsCustom.Tables[0].Rows)
                    {
                        var bgName = (string) drCustom["name"];
                        var obj = dr["og_" + bgName + "_field_permission_level"];
                        int permission;
                        if (Convert.IsDBNull(obj))
                            permission = Security.PermissionAll;
                        else
                            permission = (int) obj;
                        this.DictCustomFieldPermissionLevel[bgName] = permission;
                    }
                }
            }
            else
            {
                foreach (DataRow drCustom in this.DsCustom.Tables[0].Rows)
                {
                    var bgName = (string) drCustom["name"];
                    this.DictCustomFieldPermissionLevel[bgName] = Convert.ToInt32(Request[bgName]);
                }

                on_update();
            }
        }

        public bool validate()
        {
            var good = true;
            if (this.og_name.Value == "")
            {
                good = false;
                this.name_err.InnerText = "Name is required.";
            }
            else
            {
                this.name_err.InnerText = "";
            }

            return good;
        }

        public void on_update()
        {
            var good = validate();

            if (good)
            {
                if (this.Id == 0) // insert new
                {
                    this.Sql = @"
insert into orgs
    (og_name,
    og_domain,
    og_active,
    og_non_admins_can_use,
    og_external_user,
    og_can_edit_sql,
    og_can_delete_bug,
    og_can_edit_and_delete_posts,
    og_can_merge_bugs,
    og_can_mass_edit_bugs,
    og_can_use_reports,
    og_can_edit_reports,
    og_can_be_assigned_to,
    og_can_view_tasks,
    og_can_edit_tasks,
    og_can_search,
    og_can_only_see_own_reported,
    og_can_assign_to_internal_users,
    og_other_orgs_permission_level,
    og_project_field_permission_level,
    og_org_field_permission_level,
    og_category_field_permission_level,
    og_tags_field_permission_level,
    og_priority_field_permission_level,
    og_status_field_permission_level,
    og_assigned_to_field_permission_level,
    og_udf_field_permission_level
    $custom1$
    )
    values (
    N'$name', 
    N'$domain',
    $active,
    $non_admins_can_use,
    $external_user,
    $can_edit_sql,
    $can_delete_bug,
    $can_edit_and_delete_posts,
    $can_merge_bugs,
    $can_mass_edit_bugs,
    $can_use_reports,
    $can_edit_reports,
    $can_be_assigned_to,
    $can_view_tasks,
    $can_edit_tasks,
    $can_search,
    $can_only_see_own_reported,
    $can_assign_to_internal_users,
    $other_orgs,
    $flp_project,
    $flp_org,
    $flp_category,
    $flp_tags,
    $flp_priority,
    $flp_status,
    $flp_assigned_to,
    $flp_udf
    $custom2$
)";
                }
                else // edit existing
                {
                    this.Sql = @"
update orgs set
    og_name = N'$name',
    og_domain = N'$domain',
    og_active = $active,
    og_non_admins_can_use = $non_admins_can_use,
    og_external_user = $external_user,
    og_can_edit_sql = $can_edit_sql,
    og_can_delete_bug = $can_delete_bug,
    og_can_edit_and_delete_posts = $can_edit_and_delete_posts,
    og_can_merge_bugs = $can_merge_bugs,
    og_can_mass_edit_bugs = $can_mass_edit_bugs,
    og_can_use_reports = $can_use_reports,
    og_can_edit_reports = $can_edit_reports,
    og_can_be_assigned_to = $can_be_assigned_to,
    og_can_view_tasks = $can_view_tasks,
    og_can_edit_tasks = $can_edit_tasks,
    og_can_search = $can_search,
    og_can_only_see_own_reported = $can_only_see_own_reported,
    og_can_assign_to_internal_users = $can_assign_to_internal_users,
    og_other_orgs_permission_level = $other_orgs,
    og_project_field_permission_level = $flp_project,
    og_org_field_permission_level = $flp_org,
    og_category_field_permission_level = $flp_category,
    og_tags_field_permission_level = $flp_tags,
    og_priority_field_permission_level = $flp_priority,
    og_status_field_permission_level = $flp_status,
    og_assigned_to_field_permission_level = $flp_assigned_to,
    og_udf_field_permission_level = $flp_udf
    $custom3$
    where og_id = $og_id";

                    this.Sql = this.Sql.Replace("$og_id", Convert.ToString(this.Id));
                }

                this.Sql = this.Sql.Replace("$name", this.og_name.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$domain", this.og_domain.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$active", Util.BoolToString(this.og_active.Checked));
                this.Sql = this.Sql.Replace("$non_admins_can_use",
                    Util.BoolToString(this.non_admins_can_use.Checked));
                this.Sql = this.Sql.Replace("$external_user", Util.BoolToString(this.external_user.Checked));
                this.Sql = this.Sql.Replace("$can_edit_sql", Util.BoolToString(this.can_edit_sql.Checked));
                this.Sql = this.Sql.Replace("$can_delete_bug", Util.BoolToString(this.can_delete_bug.Checked));
                this.Sql = this.Sql.Replace("$can_edit_and_delete_posts",
                    Util.BoolToString(this.can_edit_and_delete_posts.Checked));
                this.Sql = this.Sql.Replace("$can_merge_bugs", Util.BoolToString(this.can_merge_bugs.Checked));
                this.Sql = this.Sql.Replace("$can_mass_edit_bugs",
                    Util.BoolToString(this.can_mass_edit_bugs.Checked));
                this.Sql = this.Sql.Replace("$can_use_reports", Util.BoolToString(this.can_use_reports.Checked));
                this.Sql = this.Sql.Replace("$can_edit_reports", Util.BoolToString(this.can_edit_reports.Checked));
                this.Sql = this.Sql.Replace("$can_be_assigned_to",
                    Util.BoolToString(this.can_be_assigned_to.Checked));
                this.Sql = this.Sql.Replace("$can_view_tasks", Util.BoolToString(this.can_view_tasks.Checked));
                this.Sql = this.Sql.Replace("$can_edit_tasks", Util.BoolToString(this.can_edit_tasks.Checked));
                this.Sql = this.Sql.Replace("$can_search", Util.BoolToString(this.can_search.Checked));
                this.Sql = this.Sql.Replace("$can_only_see_own_reported",
                    Util.BoolToString(this.can_only_see_own_reported.Checked));
                this.Sql = this.Sql.Replace("$can_assign_to_internal_users",
                    Util.BoolToString(this.can_assign_to_internal_users.Checked));
                this.Sql = this.Sql.Replace("$other_orgs", this.other_orgs.SelectedValue);
                this.Sql = this.Sql.Replace("$flp_project", this.project_field.SelectedValue);
                this.Sql = this.Sql.Replace("$flp_org", this.org_field.SelectedValue);
                this.Sql = this.Sql.Replace("$flp_category", this.category_field.SelectedValue);
                this.Sql = this.Sql.Replace("$flp_tags", this.tags_field.SelectedValue);
                this.Sql = this.Sql.Replace("$flp_priority", this.priority_field.SelectedValue);
                this.Sql = this.Sql.Replace("$flp_status", this.status_field.SelectedValue);
                this.Sql = this.Sql.Replace("$flp_assigned_to", this.assigned_to_field.SelectedValue);
                this.Sql = this.Sql.Replace("$flp_udf", this.udf_field.SelectedValue);

                if (this.Id == 0) // insert new
                {
                    var custom1 = "";
                    var custom2 = "";
                    foreach (DataRow drCustom in this.DsCustom.Tables[0].Rows)
                    {
                        var bgName = (string) drCustom["name"];
                        var ogColName = "og_"
                                          + bgName
                                          + "_field_permission_level";

                        custom1 += ",[" + ogColName + "]";
                        custom2 += "," + Util.SanitizeInteger(Request[bgName]);
                    }

                    this.Sql = this.Sql.Replace("$custom1$", custom1);
                    this.Sql = this.Sql.Replace("$custom2$", custom2);
                }
                else
                {
                    var custom3 = "";
                    foreach (DataRow drCustom in this.DsCustom.Tables[0].Rows)
                    {
                        var bgName = (string) drCustom["name"];
                        var ogColName = "og_"
                                          + bgName
                                          + "_field_permission_level";

                        custom3 += ",[" + ogColName + "]=" + Util.SanitizeInteger(Request[bgName]);
                    }

                    this.Sql = this.Sql.Replace("$custom3$", custom3);
                }

                DbUtil.ExecuteNonQuery(this.Sql);
                Server.Transfer("~/Administration/Organizations/List.aspx");
            }
            else
            {
                if (this.Id == 0) // insert new
                    this.msg.InnerText = "Organization was not created.";
                else // edit existing
                    this.msg.InnerText = "Organization was not updated.";
            }
        }
    }
}