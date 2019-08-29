/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class update_dashboard : Page
    {
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK_EXCEPT_GUEST);

            if (this.security.user.is_admin || this.security.user.can_use_reports)
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
                var rp_id = Convert.ToInt32(Util.sanitize_integer(Request["rp_id"]));
                var rp_col = Convert.ToInt32(Util.sanitize_integer(Request["rp_col"]));

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

                sql = sql.Replace("$user", Convert.ToString(this.security.user.usid));
                sql = sql.Replace("$report", Convert.ToString(rp_id));
                sql = sql.Replace("$chart_type", Request["rp_chart_type"].Replace("'", "''"));
                sql = sql.Replace("$col", Convert.ToString(rp_col));
            }
            else if (action == "delete")
            {
                var ds_id = Convert.ToInt32(Util.sanitize_integer(Request["ds_id"]));
                sql = "delete from dashboard_items where ds_id = $ds_id and ds_user = $user";
                sql = sql.Replace("$ds_id", Convert.ToString(ds_id));
                sql = sql.Replace("$user", Convert.ToString(this.security.user.usid));
            }
            else if (action == "moveup" || action == "movedown")
            {
                var ds_id = Convert.ToInt32(Util.sanitize_integer(Request["ds_id"]));

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
                sql = sql.Replace("$ds_id", Convert.ToString(ds_id));
                sql = sql.Replace("$user", Convert.ToString(this.security.user.usid));
            }

            if (action != "")
            {
                DbUtil.execute_nonquery(sql);
                Response.Redirect("edit_dashboard.aspx");
            }
            else
            {
                Response.Write("?");
                Response.End();
            }
        }
    }
}