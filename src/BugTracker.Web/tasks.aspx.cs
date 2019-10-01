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
    using Core;

    public partial class Tasks : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public int Bugid;
        public DataSet Ds;
        public SecurityPermissionLevel PermissionLevel;
        public string Ses;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            Page.Title = $"{ApplicationSettings.AppTitle} - tasks";

            this.Bugid = Convert.ToInt32(Util.SanitizeInteger(Request["bugid"]));

            this.PermissionLevel = Bug.GetBugPermissionLevel(this.Bugid, Security);
            if (this.PermissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                Response.Write("You are not allowed to view tasks for this item");
                Response.End();
            }

            if (Security.User.IsAdmin || Security.User.CanViewTasks)
            {
                // allowed
            }
            else
            {
                Response.Write("You are not allowed to view tasks");
                Response.End();
            }

            this.Ses = (string) Session["session_cookie"];

            var sql = "select tsk_id [id],";

            if (this.PermissionLevel == SecurityPermissionLevel.PermissionAll && !Security.User.IsGuest &&
                (Security.User.IsAdmin || Security.User.CanEditTasks))
                sql += @"
'<a   href=EditTask.aspx?bugid=$bugid&id=' + convert(varchar,tsk_id) + '>edit</a>'   [$no_sort_edit],
'<a href=DeleteTask.aspx?ses=$ses&bugid=$bugid&id=' + convert(varchar,tsk_id) + '>delete</a>' [$no_sort_delete],";

            sql += "tsk_description [description]";

            if (ApplicationSettings.ShowTaskAssignedTo)
            {
                sql += ",us_username [assigned to]";
            }

            if (ApplicationSettings.ShowTaskPlannedStartDate)
            {
                sql += ", tsk_planned_start_date [planned start]";
            }

            if (ApplicationSettings.ShowTaskActualStartDate)
            {
                sql += ", tsk_actual_start_date [actual start]";
            }

            if (ApplicationSettings.ShowTaskPlannedEndDate)
            {
                sql += ", tsk_planned_end_date [planned end]";
            }

            if (ApplicationSettings.ShowTaskActualEndDate)
            {
                sql += ", tsk_actual_end_date [actual end]";
            }

            if (ApplicationSettings.ShowTaskPlannedDuration)
            {
                sql += ", tsk_planned_duration [planned<br>duration]";
            }

            if (ApplicationSettings.ShowTaskActualDuration)
            {
                sql += ", tsk_actual_duration  [actual<br>duration]";
            }

            if (ApplicationSettings.ShowTaskDurationUnits)
            {
                sql += ", tsk_duration_units [duration<br>units]";
            }

            if (ApplicationSettings.ShowTaskPercentComplete)
            {
                sql += ", tsk_percent_complete [percent<br>complete]";
            }

            if (ApplicationSettings.ShowTaskStatus)
            {
                sql += ", st_name  [status]";
            }

            if (ApplicationSettings.ShowTaskSortSequence)
            {
                sql += ", tsk_sort_sequence  [seq]";
            }

            sql += @"
from bug_tasks 
left outer join statuses on tsk_status = st_id
left outer join users on tsk_assigned_to_user = us_id
where tsk_bug = $bugid 
order by tsk_sort_sequence, tsk_id";

            sql = sql.Replace("$bugid", Convert.ToString(this.Bugid));
            sql = sql.Replace("$ses", this.Ses);

            this.Ds = DbUtil.GetDataSet(sql);
        }
    }
}