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

    public partial class tasks : Page
    {
        public int bugid;
        public DataSet ds;
        public int permission_level;

        public Security security;
        public string ses;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "tasks";

            this.bugid = Convert.ToInt32(Util.sanitize_integer(Request["bugid"]));

            this.permission_level = Bug.get_bug_permission_level(this.bugid, this.security);
            if (this.permission_level == Security.PERMISSION_NONE)
            {
                Response.Write("You are not allowed to view tasks for this item");
                Response.End();
            }

            if (this.security.user.is_admin || this.security.user.can_view_tasks)
            {
                // allowed
            }
            else
            {
                Response.Write("You are not allowed to view tasks");
                Response.End();
            }

            this.ses = (string) Session["session_cookie"];

            var sql = "select tsk_id [id],";

            if (this.permission_level == Security.PERMISSION_ALL && !this.security.user.is_guest &&
                (this.security.user.is_admin || this.security.user.can_edit_tasks))
                sql += @"
'<a   href=edit_task.aspx?bugid=$bugid&id=' + convert(varchar,tsk_id) + '>edit</a>'   [$no_sort_edit],
'<a href=delete_task.aspx?ses=$ses&bugid=$bugid&id=' + convert(varchar,tsk_id) + '>delete</a>' [$no_sort_delete],";

            sql += "tsk_description [description]";

            if (Util.get_setting("ShowTaskAssignedTo", "1") == "1") sql += ",us_username [assigned to]";

            if (Util.get_setting("ShowTaskPlannedStartDate", "1") == "1")
                sql += ", tsk_planned_start_date [planned start]";
            if (Util.get_setting("ShowTaskActualStartDate", "1") == "1")
                sql += ", tsk_actual_start_date [actual start]";

            if (Util.get_setting("ShowTaskPlannedEndDate", "1") == "1") sql += ", tsk_planned_end_date [planned end]";
            if (Util.get_setting("ShowTaskActualEndDate", "1") == "1") sql += ", tsk_actual_end_date [actual end]";

            if (Util.get_setting("ShowTaskPlannedDuration", "1") == "1")
                sql += ", tsk_planned_duration [planned<br>duration]";
            if (Util.get_setting("ShowTaskActualDuration", "1") == "1")
                sql += ", tsk_actual_duration  [actual<br>duration]";

            if (Util.get_setting("ShowTaskDurationUnits", "1") == "1")
                sql += ", tsk_duration_units [duration<br>units]";

            if (Util.get_setting("ShowTaskPercentComplete", "1") == "1")
                sql += ", tsk_percent_complete [percent<br>complete]";

            if (Util.get_setting("ShowTaskStatus", "1") == "1") sql += ", st_name  [status]";

            if (Util.get_setting("ShowTaskSortSequence", "1") == "1") sql += ", tsk_sort_sequence  [seq]";

            sql += @"
from bug_tasks 
left outer join statuses on tsk_status = st_id
left outer join users on tsk_assigned_to_user = us_id
where tsk_bug = $bugid 
order by tsk_sort_sequence, tsk_id";

            sql = sql.Replace("$bugid", Convert.ToString(this.bugid));
            sql = sql.Replace("$ses", this.ses);

            this.ds = DbUtil.get_dataset(sql);
        }
    }
}