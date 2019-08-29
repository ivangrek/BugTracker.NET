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

    public partial class edit_dashboard : Page
    {
        public DataSet ds;
        public Security security;
        public string ses = "";

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK_EXCEPT_GUEST);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "edit dashboard";

            if (this.security.user.is_admin || this.security.user.can_use_reports)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            this.ses = (string) Session["session_cookie"];

            var sql = @"
select ds_id, ds_col, ds_row, ds_chart_type, rp_desc
from dashboard_items ds
inner join reports on rp_id = ds_report
where ds_user = $user
order by ds_col, ds_row";

            sql = sql.Replace("$user", Convert.ToString(this.security.user.usid));

            this.ds = DbUtil.get_dataset(sql);
        }

        public void write_link(int id, string action, string text)
        {
            Response.Write("<a href=update_dashboard.aspx?actn=");
            Response.Write(action);
            Response.Write("&ds_id=");
            Response.Write(Convert.ToString(id));
            Response.Write("&ses=");
            Response.Write(this.ses);
            Response.Write(">[");
            Response.Write(text);
            Response.Write("]</a>&nbsp;&nbsp;&nbsp;");
        }

        public void write_column(int col)
        {
            var first_row = true;
            var last_row = -1;

            foreach (DataRow dr in this.ds.Tables[0].Rows)
                if ((int) dr["ds_col"] == col)
                    last_row = (int) dr["ds_row"];

            foreach (DataRow dr in this.ds.Tables[0].Rows)
                if ((int) dr["ds_col"] == col)
                {
                    Response.Write("<div class=panel>");

                    write_link((int) dr["ds_id"], "delete", "delete");

                    if (first_row)
                        first_row = false;
                    else
                        write_link((int) dr["ds_id"], "moveup", "move up");

                    if ((int) dr["ds_row"] == last_row)
                    {
                        // skip
                    }
                    else
                    {
                        write_link((int) dr["ds_id"], "movedown", "move down");
                    }

                    //write_link((int) dr["ds_id"], "switchcols", "switch columns");

                    Response.Write("<p><div style='text-align: center; font-weight: bold;'>");
                    Response.Write((string) dr["rp_desc"] + "&nbsp;-&nbsp; " + (string) dr["ds_chart_type"]);
                    Response.Write("</div>");

                    Response.Write("</div>");
                }
        }
    }
}