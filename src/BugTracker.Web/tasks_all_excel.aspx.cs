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

    public partial class tasks_all_excel : Page
    {
        public DataSet ds_tasks;
        public Security security;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            if (this.security.user.is_admin || this.security.user.can_view_tasks)
            {
                // allowed
            }
            else
            {
                Response.Write("You are not allowed to view tasks");
                Response.End();
            }

            this.ds_tasks = Util.get_all_tasks(this.security, 0);
            var dv = new DataView(this.ds_tasks.Tables[0]);

            Util.print_as_excel(Response, dv);
        }
    }
}