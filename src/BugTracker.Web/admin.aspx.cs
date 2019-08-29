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

    public partial class admin : Page
    {
        public bool nag;
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.MUST_BE_ADMIN);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "admin";

            if (false) // change this to if(true) to make the donation nag message go away
            {
            }

            var bugs = Convert.ToInt32(DbUtil.execute_scalar("select count(1) from bugs"));
            if (bugs > 100) this.nag = true;
        }
    }
}