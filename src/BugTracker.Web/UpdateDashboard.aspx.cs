/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Web.UI;
    using Core;

    public partial class UpdateDashboard : Page
    {
        public ISecurity Security { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            if (Security.User.IsAdmin || Security.User.CanUseReports)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            if (Request.QueryString["ses"] != (string) Session["session_cookie"])
            {
                Response.Write("session in URL doesn't match session cookie");
                Response.End();
            }

            var action = Request["actn"];

            var sql = "";

            if (action == "add")
            {
                var rpId = Convert.ToInt32(Util.SanitizeInteger(Request["rp_id"]));
                var rpCol = Convert.ToInt32(Util.SanitizeInteger(Request["rp_col"]));

                sql = @"
declare @last_row int
set @last_row = -1

select @last_row = max(ds_row) from dashboard_items
where ds_user = $user
and ds_col = $col

if @last_row = -1 or @last_row is null
	set @last_row = 1
else
	set @last_row = @last_row + 1

insert into dashboard_items
(ds_user, ds_report, ds_chart_type, ds_col, ds_row)
values ($user, $report, '$chart_type', $col, @last_row)";

                sql = sql.Replace("$user", Convert.ToString(Security.User.Usid));
                sql = sql.Replace("$report", Convert.ToString(rpId));
                sql = sql.Replace("$chart_type", Request["rp_chart_type"].Replace("'", "''"));
                sql = sql.Replace("$col", Convert.ToString(rpCol));
            }
            else if (action == "delete")
            {
                var dsId = Convert.ToInt32(Util.SanitizeInteger(Request["ds_id"]));
                sql = "delete from dashboard_items where ds_id = $ds_id and ds_user = $user";
                sql = sql.Replace("$ds_id", Convert.ToString(dsId));
                sql = sql.Replace("$user", Convert.ToString(Security.User.Usid));
            }
            else if (action == "moveup" || action == "movedown")
            {
                var dsId = Convert.ToInt32(Util.SanitizeInteger(Request["ds_id"]));

                sql = @"
/* swap positions */
declare @other_row int
declare @this_row int
declare @col int

select @this_row = ds_row, @col = ds_col
from dashboard_items
where ds_id = $ds_id and ds_user = $user

set @other_row = @this_row + $delta

update dashboard_items
set ds_row = @this_row
where ds_user = $user
and ds_col = @col
and ds_row = @other_row

update dashboard_items
set ds_row = @other_row
where ds_user = $user
and ds_id = $ds_id
";

                if (action == "moveup")
                    sql = sql.Replace("$delta", "-1");
                else
                    sql = sql.Replace("$delta", "1");
                sql = sql.Replace("$ds_id", Convert.ToString(dsId));
                sql = sql.Replace("$user", Convert.ToString(Security.User.Usid));
            }

            if (action != "")
            {
                DbUtil.ExecuteNonQuery(sql);
                Response.Redirect("EditDashboard.aspx");
            }
            else
            {
                Response.Write("?");
                Response.End();
            }
        }
    }
}