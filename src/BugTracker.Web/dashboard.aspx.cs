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

    public partial class Dashboard : Page
    {
        public DataSet Ds;
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.AnyUserOk);

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "dashboard";

            if (this.Security.User.IsAdmin || this.Security.User.CanUseReports)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            var sql = @"
select ds.*, rp_desc
from dashboard_items ds
inner join reports on rp_id = ds_report
where ds_user = $us
order by ds_col, ds_row";

            sql = sql.Replace("$us", Convert.ToString(this.Security.User.Usid));
            this.Ds = DbUtil.GetDataSet(sql);
        }

        public void write_column(int col)
        {
            var iframeId = 0;

            foreach (DataRow dr in this.Ds.Tables[0].Rows)
                if ((int) dr["ds_col"] == col)
                {
                    if ((string) dr["ds_chart_type"] == "data")
                    {
                        iframeId++;
                        Response.Write("\n<div class=panel>");
                        Response.Write("\n<iframe frameborder='0' src=ViewReport.aspx?view=data&id="
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
                        Response.Write("\n<img src=ViewReport.aspx?scale=2&view=" + dr["ds_chart_type"] + "&id=" +
                                       dr["ds_report"] + ">");
                        Response.Write("\n</div>");
                    }
                }
        }
    }
}