/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Accounts
{
    using System;
    using System.Web.UI;
    using Core;

    public partial class Home : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public IAuthenticate Authenticate { get; set; }
        public ISecurity Security { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            // TODO change after migration
            Response.Redirect("~/Account/Login");
        }
    }
}