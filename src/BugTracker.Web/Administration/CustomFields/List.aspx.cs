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
    using Core;

    public partial class List : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }

        public DataSet Ds;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.MustBeAdmin);

            MainMenu.Security = security;
            MainMenu.SelectedItem = "admin";

            Page.Title = $"{ApplicationSettings.AppTitle} - custom fields";

            this.Ds = Util.GetCustomColumns();
        }
    }
}