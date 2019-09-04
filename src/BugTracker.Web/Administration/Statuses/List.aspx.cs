/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Statuses
{
    using System;
    using System.Data;
    using System.Web.UI;
    using Core;
    using Core.Administration;
    using Core.Persistence;

    public partial class List : Page
    {
        private readonly IStatusService statusService = new StatusService(new ApplicationContext());

        protected DataSet Ds { get; set; }

        protected void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.MustBeAdmin);

            MainMenu.Security = security;
            MainMenu.SelectedItem = "admin";

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - statuses";

            Ds = this.statusService.LoadList();
        }
    }
}