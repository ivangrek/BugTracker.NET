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

    public partial class TasksFrame : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }

        public string StringBugid;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Page.Title = $"{ApplicationSettings.AppTitle} - tasks";

            this.StringBugid = Util.SanitizeInteger(Request["bugid"]);
        }
    }
}