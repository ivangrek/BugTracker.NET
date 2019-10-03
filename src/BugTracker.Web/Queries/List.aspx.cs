/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Queries
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
        public IQueryService QueryService { get; set; }

        protected DataSet Ds { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            MainMenu.SelectedItem = MainMenuSections.Queries;

            Page.Title = $"{ApplicationSettings.AppTitle} - queries";

            Ds = QueryService.LoadList(this.showAll.Checked);
        }
    }
}