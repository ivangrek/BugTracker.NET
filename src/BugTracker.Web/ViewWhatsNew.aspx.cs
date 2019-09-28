/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Web.UI;
    using Core;

    public partial class ViewWhatsNew : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            if (ApplicationSettings.EnableWhatsNewPage)
            {
                Response.End();
            }

            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.AnyUserOk);

            MainMenu.Security = security;
            MainMenu.SelectedItem = "news";

            Page.Title = $"{ApplicationSettings.AppTitle} - news?";
        }
    }
}