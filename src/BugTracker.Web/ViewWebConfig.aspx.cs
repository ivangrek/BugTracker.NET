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

    public partial class ViewWebConfig : Page
    {
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);

            // create path
            var path = Request.MapPath(Request.Path);
            path = path.Replace("ViewWebConfig.aspx", "Web.config");

            Response.ContentType = "application/xml";
            Response.WriteFile(path);
        }
    }
}