/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Priorities
{
    using System;
    using System.Data;
    using System.Web.UI;
    using Core;
    using Core.Administration;
    using Core.Persistence;

    public partial class List : Page
    {
        private readonly IPriorityService priorityService = new PriorityService(new ApplicationContext());

        protected DataSet Ds;

        protected void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.MustBeAdmin);

            MainMenu.Security = security;
            MainMenu.SelectedItem = "admin";

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - priorities";

            Ds = this.priorityService.LoadList();
        }
    }
}