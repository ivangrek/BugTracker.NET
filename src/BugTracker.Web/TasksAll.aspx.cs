/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.Web.UI;
    using Core;

    public partial class TasksAll : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public DataSet DsTasks;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            Page.Title = $"{ApplicationSettings.AppTitle} - all tasks";

            if (Security.User.IsAdmin || Security.User.CanViewTasks)
            {
                // allowed
            }
            else
            {
                Response.Write("You are not allowed to view tasks");
                Response.End();
            }

            this.DsTasks = Util.GetAllTasks(Security, 0);
        }
    }
}