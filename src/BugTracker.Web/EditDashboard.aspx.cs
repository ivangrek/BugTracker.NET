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
    using BugTracker.Web.Core.Controls;
    using Core;

    public partial class EditDashboard : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        protected DataSet Ds { get; set; }
        public string Ses = "";

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            MainMenu.SelectedItem = MainMenuSections.Administration;

            Page.Title = $"{ApplicationSettings.AppTitle} - edit dashboard";

            if (Security.User.IsAdmin || Security.User.CanUseReports)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            this.Ses = (string) Session["session_cookie"];

            var sql = @"
select ds_id, ds_col, ds_row, ds_chart_type, rp_desc
from dashboard_items ds
inner join reports on rp_id = ds_report
where ds_user = $user
order by ds_col, ds_row";

            sql = sql.Replace("$user", Convert.ToString(Security.User.Usid));

            this.Ds = DbUtil.GetDataSet(sql);
        }

        public void write_link(int id, string action, string text)
        {
            Response.Write("<a href=UpdateDashboard.aspx?actn=");
            Response.Write(action);
            Response.Write("&ds_id=");
            Response.Write(Convert.ToString(id));
            Response.Write("&ses=");
            Response.Write(this.Ses);
            Response.Write(">[");
            Response.Write(text);
            Response.Write("]</a>&nbsp;&nbsp;&nbsp;");
        }

        public void write_column(int col)
        {
            var firstRow = true;
            var lastRow = -1;

            foreach (DataRow dr in this.Ds.Tables[0].Rows)
                if ((int) dr["ds_col"] == col)
                    lastRow = (int) dr["ds_row"];

            foreach (DataRow dr in this.Ds.Tables[0].Rows)
                if ((int) dr["ds_col"] == col)
                {
                    Response.Write("<div class=panel>");

                    write_link((int) dr["ds_id"], "delete", "delete");

                    if (firstRow)
                        firstRow = false;
                    else
                        write_link((int) dr["ds_id"], "moveup", "move up");

                    if ((int) dr["ds_row"] == lastRow)
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