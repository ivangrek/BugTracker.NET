/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class view_memory_log : Page
    {
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            if (Util.get_setting("MemoryLogEnabled", "1") != "1") Response.End();

            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Response.ContentType = "text/plain";
            Response.AddHeader("content-disposition", "inline; filename=\"memory_log.txt\"");

            var list = (List<string>) Application["log"];

            Response.Write(DateTime.Now.ToString("yyy-MM-dd HH:mm:ss:fff"));
            Response.Write("\n\n");

            if (list != null)
                for (var i = 0; i < list.Count; i++)
                {
                    Response.Write(list[i]);
                    Response.Write("\n");
                }
            else
                Response.Write("list is null");
        }
    }
}