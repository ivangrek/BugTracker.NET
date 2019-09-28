/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration
{
    using System;
    using System.Web.UI;
    using Core;

    public partial class Home : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }

        public bool Nag;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.MustBeAdmin);

            MainMenu.Security = security;
            MainMenu.SelectedItem = "admin";

            Page.Title = $"{ApplicationSettings.AppTitle} - admin";

            if (false) // change this to if(true) to make the donation nag message go away
            {
            }

            var bugs = Convert.ToInt32(DbUtil.ExecuteScalar("select count(1) from bugs"));
            if (bugs > 100) this.Nag = true;
        }
    }
}