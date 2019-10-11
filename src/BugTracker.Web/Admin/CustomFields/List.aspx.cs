/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.CustomFields
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

        protected DataSet Ds { get; set; }

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.MustBeAdmin);

            MainMenu.SelectedItem = MainMenuSections.Administration;

            Page.Title = $"{ApplicationSettings.AppTitle} - custom fields";

            Ds = Util.GetCustomColumns();
        }
    }
}