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
    using Core;

    public partial class Tasks : Page
    {
        public int Bugid;
        public DataSet Ds;
        public int PermissionLevel;

        public Security Security;
        public string Ses;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.AnyUserOk);

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "tasks";

            this.Bugid = Convert.ToInt32(Util.SanitizeInteger(Request["bugid"]));

            this.PermissionLevel = Bug.GetBugPermissionLevel(this.Bugid, this.Security);
            if (this.PermissionLevel == Security.PermissionNone)
            {
                Response.Write("You are not allowed to view tasks for this item");
                Response.End();
            }

            if (this.Security.User.IsAdmin || this.Security.User.CanViewTasks)
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

            if (this.PermissionLevel == Security.PermissionAll && !this.Security.User.IsGuest &&
                (this.Security.User.IsAdmin || this.Security.User.CanEditTasks))
                sql += @"
'<a   href=EditTask.aspx?bugid=$bugid&id=' + convert(varchar,tsk_id) + '>edit</a>'   [$no_sort_edit],
'<a href=DeleteTask.aspx?ses=$ses&bugid=$bugid&id=' + convert(varchar,tsk_id) + '>delete</a>' [$no_sort_delete],";

            sql += "tsk_description [description]";

            if (Util.GetSetting("ShowTaskAssignedTo", "1") == "1") sql += ",us_username [assigned to]";

            if (Util.GetSetting("ShowTaskPlannedStartDate", "1") == "1")
                sql += ", tsk_planned_start_date [planned start]";
            if (Util.GetSetting("ShowTaskActualStartDate", "1") == "1")
                sql += ", tsk_actual_start_date [actual start]";

            if (Util.GetSetting("ShowTaskPlannedEndDate", "1") == "1") sql += ", tsk_planned_end_date [planned end]";
            if (Util.GetSetting("ShowTaskActualEndDate", "1") == "1") sql += ", tsk_actual_end_date [actual end]";

            if (Util.GetSetting("ShowTaskPlannedDuration", "1") == "1")
                sql += ", tsk_planned_duration [planned<br>duration]";
            if (Util.GetSetting("ShowTaskActualDuration", "1") == "1")
                sql += ", tsk_actual_duration  [actual<br>duration]";

            if (Util.GetSetting("ShowTaskDurationUnits", "1") == "1")
                sql += ", tsk_duration_units [duration<br>units]";

            if (Util.GetSetting("ShowTaskPercentComplete", "1") == "1")
                sql += ", tsk_percent_complete [percent<br>complete]";

            if (Util.GetSetting("ShowTaskStatus", "1") == "1") sql += ", st_name  [status]";

            if (Util.GetSetting("ShowTaskSortSequence", "1") == "1") sql += ", tsk_sort_sequence  [seq]";

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