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

    public partial class view_whatsnew : Page
    {
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            if (Util.get_setting("EnableWhatsNewPage", "0") != "1") Response.End();

            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "news?";
        }
    }
}