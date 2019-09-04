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
    using System.Web.UI.HtmlControls;
    using System.Web.UI.WebControls;
    using Core;

    public partial class EditTask : Page
    {
        public int Bugid;

        public string Sql;
        public int TskId;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.AnyUserOkExceptGuest);

            this.msg.InnerText = "";

            var stringBugid = Util.SanitizeInteger(Request["bugid"]);
            this.Bugid = Convert.ToInt32(stringBugid);

            var permissionLevel = Bug.GetBugPermissionLevel(this.Bugid, security);

            if (permissionLevel != Security.PermissionAll)
            {
                Response.Write("You are not allowed to edit tasks for this item");
                Response.End();
            }

            if (security.User.IsAdmin || security.User.CanEditTasks)
            {
                // allowed	
            }
            else
            {
                Response.Write("You are not allowed to edit tasks");
                Response.End();
            }

            var stringTskId = Util.SanitizeInteger(Request["id"]);
            this.tsk_id_static.InnerHtml = stringTskId;
            this.TskId = Convert.ToInt32(stringTskId);

            if (!IsPostBack)
            {
                Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                            + "edit task";

                this.bugid_label.InnerHtml =
                    Util.CapitalizeFirstLetter(Util.GetSetting("SingularBugLabel", "bug")) + " ID:";
                this.bugid_static.InnerHtml = Convert.ToString(this.Bugid);

                load_users_dropdowns(this.Bugid, security);

                if (Util.GetSetting("ShowTaskAssignedTo", "1") == "0") this.assigned_to_tr.Visible = false;

                if (Util.GetSetting("ShowTaskPlannedStartDate", "1") == "0")
                    this.planned_start_date_tr.Visible = false;
                if (Util.GetSetting("ShowTaskActualStartDate", "1") == "0") this.actual_start_date_tr.Visible = false;

                if (Util.GetSetting("ShowTaskPlannedEndDate", "1") == "0") this.planned_end_date_tr.Visible = false;
                if (Util.GetSetting("ShowTaskActualEndDate", "1") == "0") this.actual_end_date_tr.Visible = false;

                if (Util.GetSetting("ShowTaskPlannedDuration", "1") == "0") this.planned_duration_tr.Visible = false;
                if (Util.GetSetting("ShowTaskActualDuration", "1") == "0") this.actual_duration_tr.Visible = false;

                if (Util.GetSetting("ShowTaskDurationUnits", "1") == "0") this.duration_units_tr.Visible = false;

                if (Util.GetSetting("ShowTaskPercentComplete", "1") == "0") this.percent_complete_tr.Visible = false;

                if (Util.GetSetting("ShowTaskStatus", "1") == "0") this.status_tr.Visible = false;

                if (Util.GetSetting("ShowTaskSortSequence", "1") == "0") this.sort_sequence_tr.Visible = false;

                // add or edit?
                if (this.TskId == 0)
                {
                    this.tsk_id_tr.Visible = false;
                    this.sub.Value = "Create";

                    var defaultDurationUnits = Util.GetSetting("TaskDefaultDurationUnits", "hours");
                    this.duration_units.Items.FindByText(defaultDurationUnits).Selected = true;

                    var defaultHour = Util.GetSetting("TaskDefaultHour", "09");
                    this.planned_start_hour.Items.FindByText(defaultHour).Selected = true;
                    this.actual_start_hour.Items.FindByText(defaultHour).Selected = true;
                    this.planned_end_hour.Items.FindByText(defaultHour).Selected = true;
                    this.actual_end_hour.Items.FindByText(defaultHour).Selected = true;

                    var defaultStatus = Util.GetSetting("TaskDefaultStatus", "[no status]");
                    this.status.Items.FindByText(defaultStatus).Selected = true;
                }
                else
                {
                    // Get this entry's data from the db and fill in the form

                    this.Sql = @"select * from bug_tasks where tsk_id = $tsk_id and tsk_bug = $bugid";
                    this.Sql = this.Sql.Replace("$tsk_id", Convert.ToString(this.TskId));
                    this.Sql = this.Sql.Replace("$bugid", Convert.ToString(this.Bugid));
                    var dr = DbUtil.GetDataRow(this.Sql);

                    this.assigned_to.Items.FindByValue(Convert.ToString(dr["tsk_assigned_to_user"])).Selected = true;

                    this.duration_units.Items.FindByText(Convert.ToString(dr["tsk_duration_units"])).Selected = true;

                    this.status.Items.FindByValue(Convert.ToString(dr["tsk_status"])).Selected = true;

                    this.planned_duration.Value = Util.FormatDbValue(dr["tsk_planned_duration"]);
                    this.actual_duration.Value = Util.FormatDbValue(dr["tsk_actual_duration"]);
                    this.percent_complete.Value = Convert.ToString(dr["tsk_percent_complete"]);
                    this.sort_sequence.Value = Convert.ToString(dr["tsk_sort_sequence"]);
                    this.desc.Value = Convert.ToString(dr["tsk_description"]);

                    load_date_hour_min(this.planned_start_date, this.planned_start_hour, this.planned_start_min,
                        dr["tsk_planned_start_date"]);

                    load_date_hour_min(this.actual_start_date, this.actual_start_hour, this.actual_start_min,
                        dr["tsk_actual_start_date"]);

                    load_date_hour_min(this.planned_end_date, this.planned_end_hour, this.planned_end_min,
                        dr["tsk_planned_end_date"]);

                    load_date_hour_min(this.actual_end_date, this.actual_end_hour, this.actual_end_min,
                        dr["tsk_actual_end_date"]);

                    this.sub.Value = "Update";
                }
            }
            else
            {
                on_update(security);
            }
        }

        public void load_date_hour_min(
            HtmlInputText dateControl,
            DropDownList hourControl,
            DropDownList minControl,
            object date)
        {
            if (Convert.IsDBNull(date))
            {
                dateControl.Value = "";
            }
            else
            {
                var dt = Convert.ToDateTime(date);
                var tempDate = dt.Year.ToString("0000") + "-" + dt.Month.ToString("00") + "-" + dt.Day.ToString("00");
                dateControl.Value = Util.FormatDbDateTime(Convert.ToDateTime(tempDate));
                hourControl.Items.FindByValue(dt.Hour.ToString("00")).Selected = true;
                minControl.Items.FindByValue(dt.Minute.ToString("00")).Selected = true;
            }
        }

        public void load_users_dropdowns(int bugid, Security security)
        {
            // What's selected now?   Save it before we refresh the dropdown.
            var currentValue = "";

            if (IsPostBack) currentValue = this.assigned_to.SelectedItem.Value;

            this.Sql = @"
declare @project int
declare @assigned_to int
select @project = bg_project, @assigned_to = bg_assigned_to_user from bugs where bg_id = $bg_id";

            // Load the user dropdown, which changes per project
            // Only users explicitly allowed will be listed
            if (Util.GetSetting("DefaultPermissionLevel", "2") == "0")
                this.Sql += @"

/* users this project */ select us_id, case when $fullnames then us_lastname + ', ' + us_firstname else us_username end us_username
from users
inner join orgs on us_org = og_id
where us_active = 1
and og_can_be_assigned_to = 1
and ($og_other_orgs_permission_level <> 0 or $og_id = og_id or og_external_user = 0)
and us_id in
    (select pu_user from project_user_xref
        where pu_project = @project
        and pu_permission_level <> 0)
order by us_username; ";
            // Only users explictly DISallowed will be omitted
            else
                this.Sql += @"
/* users this project */ select us_id, case when $fullnames then us_lastname + ', ' + us_firstname else us_username end us_username
from users
inner join orgs on us_org = og_id
where us_active = 1
and og_can_be_assigned_to = 1
and ($og_other_orgs_permission_level <> 0 or $og_id = og_id or og_external_user = 0)
and us_id not in
    (select pu_user from project_user_xref
        where pu_project = @project
        and pu_permission_level = 0)
order by us_username; ";

            this.Sql += "\nselect st_id, st_name from statuses order by st_sort_seq, st_name";

            this.Sql += "\nselect isnull(@assigned_to,0) ";

            this.Sql = this.Sql.Replace("$og_id", Convert.ToString(security.User.Org));
            this.Sql = this.Sql.Replace("$og_other_orgs_permission_level",
                Convert.ToString(security.User.OtherOrgsPermissionLevel));
            this.Sql = this.Sql.Replace("$bg_id", Convert.ToString(bugid));

            if (Util.GetSetting("UseFullNames", "0") == "0")
                // false condition
                this.Sql = this.Sql.Replace("$fullnames", "0 = 1");
            else
                // true condition
                this.Sql = this.Sql.Replace("$fullnames", "1 = 1");

            this.assigned_to.DataSource = new DataView(DbUtil.GetDataSet(this.Sql).Tables[0]);
            this.assigned_to.DataTextField = "us_username";
            this.assigned_to.DataValueField = "us_id";
            this.assigned_to.DataBind();
            this.assigned_to.Items.Insert(0, new ListItem("[not assigned]", "0"));

            this.status.DataSource = new DataView(DbUtil.GetDataSet(this.Sql).Tables[1]);
            this.status.DataTextField = "st_name";
            this.status.DataValueField = "st_id";
            this.status.DataBind();
            this.status.Items.Insert(0, new ListItem("[no status]", "0"));

            // by default, assign the entry to the same user to whom the bug is assigned to?
            // or should it be assigned to the logged in user?
            if (this.TskId == 0)
            {
                var defaultAssignedToUser = (int) DbUtil.GetDataSet(this.Sql).Tables[2].Rows[0][0];
                var li = this.assigned_to.Items.FindByValue(Convert.ToString(defaultAssignedToUser));
                if (li != null) li.Selected = true;
            }
        }

        public bool validate()
        {
            var good = true;

            if (this.sort_sequence.Value != "")
            {
                if (!Util.IsInt(this.sort_sequence.Value))
                {
                    good = false;
                    this.sort_sequence_err.InnerText = "Sort Sequence must be an integer.";
                }
                else
                {
                    this.sort_sequence_err.InnerText = "";
                }
            }
            else
            {
                this.sort_sequence_err.InnerText = "";
            }

            if (this.percent_complete.Value != "")
            {
                if (!Util.IsInt(this.percent_complete.Value))
                {
                    good = false;
                    this.percent_complete_err.InnerText = "Percent Complete must be from 0 to 100.";
                }
                else
                {
                    var percentCompleteInt = Convert.ToInt32(this.percent_complete.Value);
                    if (percentCompleteInt >= 0 && percentCompleteInt <= 100)
                    {
                        // good
                        this.percent_complete_err.InnerText = "";
                    }
                    else
                    {
                        good = false;
                        this.percent_complete_err.InnerText = "Percent Complete must be from 0 to 100.";
                    }
                }
            }
            else
            {
                this.percent_complete_err.InnerText = "";
            }

            if (this.planned_duration.Value != "")
            {
                var err = Util.IsValidDecimal("Planned Duration", this.planned_duration.Value, 4, 2);

                if (err != "")
                {
                    good = false;
                    this.planned_duration_err.InnerText = err;
                }
                else
                {
                    this.planned_duration_err.InnerText = "";
                }
            }
            else
            {
                this.planned_duration_err.InnerText = "";
            }

            if (this.actual_duration.Value != "")
            {
                var err = Util.IsValidDecimal("Actual Duration", this.actual_duration.Value, 4, 2);

                if (err != "")
                {
                    good = false;
                    this.actual_duration_err.InnerText = err;
                }
                else
                {
                    this.actual_duration_err.InnerText = "";
                }
            }
            else
            {
                this.actual_duration_err.InnerText = "";
            }

            return good;
        }

        // This might not be right.   Maybe use the commented out version, from Sergey Vasiliev
        public string format_date_hour_min(string date, string hour, string min)
        {
            if (!string.IsNullOrEmpty(date))
                return Util.FormatLocalDateIntoDbFormat(
                    date
                    + " "
                    + hour
                    + ":"
                    + min
                    + ":00");
            return "";
        }

        /*
    // Version from Sergey Vasiliev
    static string format_date_hour_min(string date, string hour, string min) 
    { 
        if (!string.IsNullOrEmpty(date)) 
        { 
            DateTime wDate = DateTime.ParseExact(date, 
                Util.GetSetting("JustDateFormat", "g"), 
                new System.Globalization.CultureInfo(System.Threading.Thread.CurrentThread.CurrentCulture.Name, true), 
                System.Globalization.DateTimeStyles.AllowWhiteSpaces); 

            return Util.FormatLocalDateIntoDbFormat(
                new DateTime(
                    wDate.Year,  
                    wDate.Month,  
                    wDate.Day,  
                    Convert.ToInt32(hour),  
                    Convert.ToInt32(min),0)); 
        } 
        else 
        { 
            return ""; 
        } 
    }  
    */

        public string format_decimal_for_db(string s)
        {
            if (s == "")
                return "null";
            return Util.FormatLocalDecimalIntoDbFormat(s);
        }

        public string format_number_for_db(string s)
        {
            if (s == "")
                return "null";
            return s;
        }

        public void on_update(Security security)
        {
            var good = validate();

            if (good)
            {
                if (this.TskId == 0) // insert new
                {
                    this.Sql = @"
insert into bug_tasks (
tsk_bug,
tsk_created_user,
tsk_created_date,
tsk_last_updated_user,
tsk_last_updated_date,
tsk_assigned_to_user,
tsk_planned_start_date,
tsk_actual_start_date,
tsk_planned_end_date,
tsk_actual_end_date,
tsk_planned_duration,
tsk_actual_duration,
tsk_duration_units,
tsk_percent_complete,
tsk_status,
tsk_sort_sequence,
tsk_description
)
values (
$tsk_bug,
$tsk_created_user,
getdate(),
$tsk_last_updated_user,
getdate(),
$tsk_assigned_to_user,
'$tsk_planned_start_date',
'$tsk_actual_start_date',
'$tsk_planned_end_date',
'$tsk_actual_end_date',
$tsk_planned_duration,
$tsk_actual_duration,
N'$tsk_duration_units',
$tsk_percent_complete,
$tsk_status,
$tsk_sort_sequence,
N'$tsk_description'
)

declare @tsk_id int
select @tsk_id = scope_identity()

insert into bug_posts
(bp_bug, bp_user, bp_date, bp_comment, bp_type)
values($tsk_bug, $tsk_last_updated_user, getdate(), N'added task ' + convert(varchar, @tsk_id), 'update')";

                    this.Sql = this.Sql.Replace("$tsk_created_user", Convert.ToString(security.User.Usid));
                }
                else // edit existing
                {
                    this.Sql = @"
update bug_tasks set
tsk_last_updated_user = $tsk_last_updated_user,
tsk_last_updated_date = getdate(),
tsk_assigned_to_user = $tsk_assigned_to_user,
tsk_planned_start_date = '$tsk_planned_start_date',
tsk_actual_start_date = '$tsk_actual_start_date',
tsk_planned_end_date = '$tsk_planned_end_date',
tsk_actual_end_date = '$tsk_actual_end_date',
tsk_planned_duration = $tsk_planned_duration,
tsk_actual_duration = $tsk_actual_duration,
tsk_duration_units = N'$tsk_duration_units',
tsk_percent_complete = $tsk_percent_complete,
tsk_status = $tsk_status,
tsk_sort_sequence = $tsk_sort_sequence,
tsk_description = N'$tsk_description'
where tsk_id = $tsk_id
                
insert into bug_posts
(bp_bug, bp_user, bp_date, bp_comment, bp_type)
values($tsk_bug, $tsk_last_updated_user, getdate(), N'updated task $tsk_id', 'update')";

                    this.Sql = this.Sql.Replace("$tsk_id", Convert.ToString(this.TskId));
                }

                this.Sql = this.Sql.Replace("$tsk_bug", Convert.ToString(this.Bugid));
                this.Sql = this.Sql.Replace("$tsk_last_updated_user", Convert.ToString(security.User.Usid));

                this.Sql = this.Sql.Replace("$tsk_planned_start_date",
                    format_date_hour_min(this.planned_start_date.Value, this.planned_start_hour.SelectedItem.Value,
                        this.planned_start_min.SelectedItem.Value));

                this.Sql = this.Sql.Replace("$tsk_actual_start_date",
                    format_date_hour_min(this.actual_start_date.Value, this.actual_start_hour.SelectedItem.Value,
                        this.actual_start_min.SelectedItem.Value));

                this.Sql = this.Sql.Replace("$tsk_planned_end_date",
                    format_date_hour_min(this.planned_end_date.Value, this.planned_end_hour.SelectedItem.Value,
                        this.planned_end_min.SelectedItem.Value));

                this.Sql = this.Sql.Replace("$tsk_actual_end_date",
                    format_date_hour_min(this.actual_end_date.Value, this.actual_end_hour.SelectedItem.Value,
                        this.actual_end_min.SelectedItem.Value));

                this.Sql = this.Sql.Replace("$tsk_planned_duration",
                    format_decimal_for_db(this.planned_duration.Value));
                this.Sql = this.Sql.Replace("$tsk_actual_duration", format_decimal_for_db(this.actual_duration.Value));
                this.Sql = this.Sql.Replace("$tsk_percent_complete", format_number_for_db(this.percent_complete.Value));
                this.Sql = this.Sql.Replace("$tsk_status", this.status.SelectedItem.Value);
                this.Sql = this.Sql.Replace("$tsk_sort_sequence", format_number_for_db(this.sort_sequence.Value));
                this.Sql = this.Sql.Replace("$tsk_assigned_to_user", this.assigned_to.SelectedItem.Value);
                this.Sql = this.Sql.Replace("$tsk_description", this.desc.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$tsk_duration_units",
                    this.duration_units.SelectedItem.Value.Replace("'", "''"));

                DbUtil.ExecuteNonQuery(this.Sql);

                Bug.SendNotifications(Bug.Update, this.Bugid, security);

                Response.Redirect("Tasks.aspx?bugid=" + Convert.ToString(this.Bugid));
            }
            else
            {
                if (this.TskId == 0) // insert new
                    this.msg.InnerText = "Task was not created.";
                else // edit existing
                    this.msg.InnerText = "Task was not updated.";
            }
        }
    }
}