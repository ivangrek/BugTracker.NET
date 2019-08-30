/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class TasksAllExcel : Page
    {
        public DataSet DsTasks;
        public Security Security;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.AnyUserOk);

            if (this.Security.User.IsAdmin || this.Security.User.CanViewTasks)
            {
                // allowed
            }
            else
            {
                Response.Write("You are not allowed to view tasks");
                Response.End();
            }

            this.DsTasks = Util.GetAllTasks(this.Security, 0);
            var dv = new DataView(this.DsTasks.Tables[0]);

            Util.PrintAsExcel(Response, dv);
        }
    }
}