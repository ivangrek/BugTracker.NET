/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class Admin : Page
    {
        public bool Nag;
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "admin";

            if (false) // change this to if(true) to make the donation nag message go away
            {
            }

            var bugs = Convert.ToInt32(DbUtil.ExecuteScalar("select count(1) from bugs"));
            if (bugs > 100) this.Nag = true;
        }
    }
}