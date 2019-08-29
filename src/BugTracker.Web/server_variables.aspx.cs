/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Collections.Specialized;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class server_variables : Page
    {
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.MUST_BE_ADMIN);

            int loop1, loop2;
            NameValueCollection coll;

            // Load ServerVariable collection into NameValueCollection object.
            coll = Request.ServerVariables;
            // Get names of all keys into a string array.
            var arr1 = coll.AllKeys;
            for (loop1 = 0; loop1 < arr1.Length; loop1++)
            {
                Response.Write("Key: " + arr1[loop1] + "<br>");
                var arr2 = coll.GetValues(arr1[loop1]);
                for (loop2 = 0; loop2 < arr2.Length; loop2++)
                    Response.Write("Value " + loop2 + ": " + arr2[loop2] + "<br>");
            }
        }
    }
}