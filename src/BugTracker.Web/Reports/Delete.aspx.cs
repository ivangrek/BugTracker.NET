/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Reports
{
    using System;
    using System.Web.UI;
    using BugTracker.Web.Core.Controls;
    using Core;

    public partial class Delete : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }
        public IReportService ReportService { get; set; }

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            if (!IsAuthorized)
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            MainMenu.SelectedItem = MainMenuSections.Reports;

            if (IsPostBack)
            {
                // do delete here
                var id = Convert.ToInt32(Util.SanitizeInteger(this.rowId.Value));

                ReportService.Delete(id);

                Response.Redirect("~/Reports/List.aspx");
            }
            else
            {
                Page.Title = $"{ApplicationSettings.AppTitle} - delete report";

                var id = Convert.ToInt32(Util.SanitizeInteger(Request["id"]));
                var (valid, name) = ReportService.CheckDeleting(id);

                if (valid)
                {
                    this.confirmHref.InnerText = $"confirm delete of report: \"{name}\"";
                    this.rowId.Value = Convert.ToString(id);
                }
            }
        }

        private bool IsAuthorized => Security.User.IsAdmin
            || Security.User.CanEditReports;
    }
}