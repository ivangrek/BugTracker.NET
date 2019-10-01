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

    public partial class ViewWebConfig : Page
    {
        public ISecurity Security { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.MustBeAdmin);

            // create path
            var path = Request.MapPath(Request.Path);
            path = path.Replace("ViewWebConfig.aspx", "Web.config");

            Response.ContentType = "application/xml";
            Response.WriteFile(path);
        }
    }
}