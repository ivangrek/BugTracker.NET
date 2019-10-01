/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Reports
{
    using System;
    using System.Data;
    using System.Web.UI;
    using BugTracker.Web.Core.Controls;
    using Core;

    public partial class Dashboard : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        protected DataSet Ds { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.MustBeAdmin);

            if (!IsAuthorized)
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            MainMenu.SelectedItem = MainMenuSections.Reports;

            Page.Title = $"{ApplicationSettings.AppTitle} - dashboard";

            var sql = @"
select ds.*, rp_desc
from dashboard_items ds
inner join reports on rp_id = ds_report
where ds_user = $us
order by ds_col, ds_row";

            sql = sql.Replace("$us", Convert.ToString(Security.User.Usid));
            this.Ds = DbUtil.GetDataSet(sql);
        }

        protected void WriteColumn(int col)
        {
            var iframeId = 0;

            foreach (DataRow dr in this.Ds.Tables[0].Rows)
                if ((int)dr["ds_col"] == col)
                {
                    if ((string)dr["ds_chart_type"] == "data")
                    {
                        iframeId++;
                        Response.Write("\n<div class=panel>");
                        Response.Write("\n<iframe frameborder='0' src=" + ResolveUrl("~/Reports/View.aspx") + @"?view=data&id="
                                       + dr["ds_report"]
                                       // this didn't work
                                       //+ "&parent_iframe="
                                       //+ Convert.ToString(iframe_id)
                                       //+ " id="
                                       //+ Convert.ToString(iframe_id)
                                       + "></iframe>");
                        Response.Write("\n</div>");
                    }
                    else
                    {
                        Response.Write("\n<div class=panel>");
                        Response.Write("\n<img src=" + ResolveUrl("~/Reports/View.aspx") + @"?scale=2&view=" + dr["ds_chart_type"] + "&id=" +
                                       dr["ds_report"] + ">");
                        Response.Write("\n</div>");
                    }
                }
        }

        private bool IsAuthorized => Security.User.IsAdmin
            || Security.User.CanUseReports;
    }
}