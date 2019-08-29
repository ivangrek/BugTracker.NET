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

    public partial class tasks_frame : Page
    {
        public string string_bugid;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "tasks";

            this.string_bugid = Util.sanitize_integer(Request["bugid"]);
        }
    }
}