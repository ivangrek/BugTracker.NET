/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Text;

    public class User
    {
        public static IApplicationSettings ApplicationSettings = new ApplicationSettings();

        public bool AddsNotAllowed;

        public int AssignedToFieldPermissionLevel = Security.PermissionAll;
        public int BugsPerPage = 10;
        public bool CanAssignToInternalUsers;
        public bool CanBeAssignedTo = true;
        public bool CanDeleteBug;

        public bool CanEditAndDeletePosts;
        public bool CanEditReports;
        public bool CanEditSql;
        public bool CanEditTasks = true;
        public bool CanMassEditBugs;
        public bool CanMergeBugs;
        public bool CanOnlySeeOwnReported;
        public bool CanSearch = true;

        public bool CanUseReports;

        public bool CanViewTasks = true;
        public int CategoryFieldPermissionLevel = Security.PermissionAll;

        public Dictionary<string, int> DictCustomFieldPermissionLevel = new Dictionary<string, int>();
        public string Email = "";
        public bool EnablePopups = true;

        public bool ExternalUser;
        public int ForcedProject;
        public string Fullname = "";
        public bool IsAdmin;
        public bool IsGuest;
        public bool IsProjectAdmin;
        public int Org;
        public int OrgFieldPermissionLevel = Security.PermissionAll;
        public string OrgName = "";

        public int OtherOrgsPermissionLevel = Security.PermissionAll;
        public int PriorityFieldPermissionLevel = Security.PermissionAll;
        public int ProjectFieldPermissionLevel = Security.PermissionAll;
        public int StatusFieldPermissionLevel = Security.PermissionAll;
        public int TagsFieldPermissionLevel = Security.PermissionAll;
        public int UdfFieldPermissionLevel = Security.PermissionAll;
        public bool UseFckeditor;
        public string Username = "";
        public int Usid;

        public void SetFromDb(DataRow dr)
        {
            this.Usid = Convert.ToInt32(dr["us_id"]);
            this.Username = (string) dr["us_username"];
            this.Email = (string) dr["us_email"];

            this.BugsPerPage = Convert.ToInt32(dr["us_bugs_per_page"]);
            if (ApplicationSettings.DisableFCKEditor)
                this.UseFckeditor = false;
            else
                this.UseFckeditor = Convert.ToBoolean(dr["us_use_fckeditor"]);
            this.EnablePopups = Convert.ToBoolean(dr["us_enable_bug_list_popups"]);

            this.ExternalUser = Convert.ToBoolean(dr["og_external_user"]);
            this.CanOnlySeeOwnReported = Convert.ToBoolean(dr["og_can_only_see_own_reported"]);
            this.CanEditSql = Convert.ToBoolean(dr["og_can_edit_sql"]);
            this.CanDeleteBug = Convert.ToBoolean(dr["og_can_delete_bug"]);
            this.CanEditAndDeletePosts = Convert.ToBoolean(dr["og_can_edit_and_delete_posts"]);
            this.CanMergeBugs = Convert.ToBoolean(dr["og_can_merge_bugs"]);
            this.CanMassEditBugs = Convert.ToBoolean(dr["og_can_mass_edit_bugs"]);
            this.CanUseReports = Convert.ToBoolean(dr["og_can_use_reports"]);
            this.CanEditReports = Convert.ToBoolean(dr["og_can_edit_reports"]);
            this.CanBeAssignedTo = Convert.ToBoolean(dr["og_can_be_assigned_to"]);
            this.CanViewTasks = Convert.ToBoolean(dr["og_can_view_tasks"]);
            this.CanEditTasks = Convert.ToBoolean(dr["og_can_edit_tasks"]);
            this.CanSearch = Convert.ToBoolean(dr["og_can_search"]);
            this.CanAssignToInternalUsers = Convert.ToBoolean(dr["og_can_assign_to_internal_users"]);
            this.OtherOrgsPermissionLevel = (int) dr["og_other_orgs_permission_level"];
            this.Org = (int) dr["og_id"];
            this.OrgName = (string) dr["og_name"];
            this.ForcedProject = (int) dr["us_forced_project"];

            this.CategoryFieldPermissionLevel = (int) dr["og_category_field_permission_level"];

            if (ApplicationSettings.EnableTags)
                this.TagsFieldPermissionLevel = (int) dr["og_tags_field_permission_level"];
            else
                this.TagsFieldPermissionLevel = Security.PermissionNone;
            this.PriorityFieldPermissionLevel = (int) dr["og_priority_field_permission_level"];
            this.AssignedToFieldPermissionLevel = (int) dr["og_assigned_to_field_permission_level"];
            this.StatusFieldPermissionLevel = (int) dr["og_status_field_permission_level"];
            this.ProjectFieldPermissionLevel = (int) dr["og_project_field_permission_level"];
            this.OrgFieldPermissionLevel = (int) dr["og_org_field_permission_level"];
            this.UdfFieldPermissionLevel = (int) dr["og_udf_field_permission_level"];

            // field permission for custom fields
            var dsCustom = Util.GetCustomColumns();
            foreach (DataRow drCustom in dsCustom.Tables[0].Rows)
            {
                var bgName = (string) drCustom["name"];
                var ogName = "og_"
                              + (string) drCustom["name"]
                              + "_field_permission_level";

                try
                {
                    var obj = dr[ogName];
                    if (Convert.IsDBNull(obj))
                        this.DictCustomFieldPermissionLevel[bgName] = Security.PermissionAll;
                    else
                        this.DictCustomFieldPermissionLevel[bgName] = (int) dr[ogName];
                }

                catch (Exception ex)
                {
                    Util.WriteToLog("exception looking for " + ogName + ":" + ex.Message);

                    // automatically add it if it's missing
                    DbUtil.ExecuteNonQuery("alter table orgs add ["
                                            + ogName
                                            + "] int null");
                    this.DictCustomFieldPermissionLevel[bgName] = Security.PermissionAll;
                }
            }

            if (((string) dr["us_firstname"]).Trim().Length == 0)
                this.Fullname = (string) dr["us_lastname"];
            else
                this.Fullname = (string) dr["us_lastname"] + ", " + (string) dr["us_firstname"];

            if ((int) dr["us_admin"] == 1)
            {
                this.IsAdmin = true;
            }
            else
            {
                if ((int) dr["project_admin"] > 0)
                {
                    this.IsProjectAdmin = true;
                }
                else
                {
                    if (this.Username.ToLower() == "guest") this.IsGuest = true;
                }
            }

            // if user is forced to a specific project, and doesn't have
            // at least reporter permission on that project, than user
            // can't add bugs
            if ((int) dr["us_forced_project"] != 0)
                if ((int) dr["pu_permission_level"] == Security.PermissionReadonly
                    || (int) dr["pu_permission_level"] == Security.PermissionNone)
                    this.AddsNotAllowed = true;
        }

        public static int CopyUser(
            string username,
            string email,
            string firstname,
            string lastname,
            string signature,
            int salt,
            string password,
            string templateUsername,
            bool useDomainAsOrgName)
        {
            // get all the org columns

            Util.WriteToLog("CopyUser creating " + username + " from template user " + templateUsername);
            var orgColumns = new StringBuilder();

            var sql = "";

            if (useDomainAsOrgName)
            {
                sql = @" /* get org cols */
select sc.name
from syscolumns sc
inner join sysobjects so on sc.id = so.id
where so.name = 'orgs'
and sc.name not in ('og_id', 'og_name', 'og_domain')";

                var ds = DbUtil.GetDataSet(sql);
                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    orgColumns.Append(",");
                    orgColumns.Append("[");
                    orgColumns.Append(Convert.ToString(dr["name"]));
                    orgColumns.Append("]");
                }
            }

            sql = @"
/* copy user */
declare @template_user_id int
declare @template_org_id int
select @template_user_id = us_id,
@template_org_id = us_org 
from users where us_username = N'$template_user'

declare @org_id int
set @org_id = -1

IF $use_domain_as_org_name = 1
BEGIN
    select @org_id = og_id from orgs where og_domain = N'$domain'
    IF @org_id = -1
    BEGIN
        insert into orgs
        (
            og_name,
            og_domain       
            $ORG_COLUMNS        
        )
        select 
        N'$domain',
        N'$domain'
        $ORG_COLUMNS
        from orgs where og_id = @template_org_id
        select @org_id = scope_identity()
    END
END

declare @new_user_id int
set @new_user_id = -1

IF NOT EXISTS (SELECT us_id FROM users WHERE us_username = '$username')
BEGIN

insert into users
	(us_username, us_email, us_firstname, us_lastname, us_signature, us_salt, us_password,
	us_default_query,
	us_enable_notifications,
	us_auto_subscribe,
	us_auto_subscribe_own_bugs,
	us_auto_subscribe_reported_bugs,
	us_send_notifications_to_self,
	us_active,
	us_bugs_per_page,
	us_forced_project,
	us_reported_notifications,
	us_assigned_notifications,
	us_subscribed_notifications,
	us_use_fckeditor,
	us_enable_bug_list_popups,
	us_org)

select
	N'$username', N'$email', N'$firstname', N'$lastname', N'$signature', $salt, N'$password',
	us_default_query,
	us_enable_notifications,
	us_auto_subscribe,
	us_auto_subscribe_own_bugs,
	us_auto_subscribe_reported_bugs,
	us_send_notifications_to_self,
	1, -- active
	us_bugs_per_page,
	us_forced_project,
	us_reported_notifications,
	us_assigned_notifications,
	us_subscribed_notifications,
	us_use_fckeditor,
	us_enable_bug_list_popups,
	case when @org_id = -1 then us_org else @org_id end
	from users where us_id = @template_user_id

select @new_user_id = scope_identity()

insert into project_user_xref
	(pu_project, pu_user, pu_auto_subscribe, pu_permission_level, pu_admin)

select pu_project, @new_user_id, pu_auto_subscribe, pu_permission_level, pu_admin
	from project_user_xref
	where pu_user = @template_user_id

select @new_user_id

END
";
            sql = sql.Replace("$username", username.Replace("'", "''"));
            sql = sql.Replace("$email", email.Replace("'", "''"));
            sql = sql.Replace("$firstname", firstname.Replace("'", "''"));
            sql = sql.Replace("$lastname", lastname.Replace("'", "''"));
            sql = sql.Replace("$signature", signature.Replace("'", "''"));
            sql = sql.Replace("$salt", Convert.ToString(salt));
            sql = sql.Replace("$password", password);
            sql = sql.Replace("$template_user", templateUsername.Replace("'", "''"));

            sql = sql.Replace("$use_domain_as_org_name", Convert.ToString(useDomainAsOrgName ? "1" : "0"));

            var emailParts = email.Split('@');
            if (emailParts.Length == 2)
                sql = sql.Replace("$domain", emailParts[1].Replace("'", "''"));
            else
                sql = sql.Replace("$domain", email.Replace("'", "''"));

            sql = sql.Replace("$ORG_COLUMNS", orgColumns.ToString());
            return Convert.ToInt32(DbUtil.ExecuteScalar(sql));
        }
    } // end class
}