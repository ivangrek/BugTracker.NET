/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Collections.Generic;
    using System.Web.UI;
    using Core;

    public partial class ViewMemoryLog : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            if (!ApplicationSettings.MemoryLogEnabled)
            {
                Response.End();
            }

            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

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