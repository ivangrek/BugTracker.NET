/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Reports
{
    using System;
    using System.Web.UI;
    using Core;

    public partial class Delete : Page
    {
        public string Sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.AnyUserOkExceptGuest);

            MainMenu.Security = security;
            MainMenu.SelectedItem = "reports";

            if (security.User.IsAdmin || security.User.CanEditReports)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            if (IsPostBack)
            {
                // do delete here
                this.Sql = @"
delete reports where rp_id = $1;
delete dashboard_items where ds_report = $1";
                this.Sql = this.Sql.Replace("$1", Util.SanitizeInteger(this.row_id.Value));
                DbUtil.ExecuteNonQuery(this.Sql);
                Server.Transfer("~/Reports/List.aspx");
            }
            else
            {
                Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                            + "delete report";

                var id = Util.SanitizeInteger(Request["id"]);

                this.Sql = @"select rp_desc from reports where rp_id = $1";
                this.Sql = this.Sql.Replace("$1", id);

                var dr = DbUtil.GetDataRow(this.Sql);

                this.confirm_href.InnerText = "confirm delete of report: "
                                              + Convert.ToString(dr["rp_desc"]);

                this.row_id.Value = id;
            }
        }
    }
}