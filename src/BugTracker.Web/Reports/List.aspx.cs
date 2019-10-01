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

    public partial class List : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }
        public IReportService ReportService { get; set; }

        public DataSet Ds;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            if (!IsAuthorized)
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            MainMenu.SelectedItem = MainMenuSections.Reports;

            Page.Title = $"{ApplicationSettings.AppTitle} - reports";

            Ds = ReportService.LoadList();
        }

        private bool IsAuthorized => Security.User.IsAdmin
                || Security.User.CanUseReports
                || Security.User.CanEditReports;
    }
}