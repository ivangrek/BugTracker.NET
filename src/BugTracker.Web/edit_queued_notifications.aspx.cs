/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Threading;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class edit_queued_notifications : Page
    {
        public Security security;
        public string sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.MUST_BE_ADMIN);

            if (Request.QueryString["ses"] != (string) Session["session_cookie"])
            {
                Response.Write("session in URL doesn't match session cookie");
                Response.End();
            }

            if (Request.QueryString["actn"] == "delete")
            {
                this.sql = @"delete from queued_notifications where qn_status = N'not sent'";
                DbUtil.execute_nonquery(this.sql);
            }
            else if (Request.QueryString["actn"] == "reset")
            {
                this.sql = @"update queued_notifications set qn_retries = 0 where qn_status = N'not sent'";
                DbUtil.execute_nonquery(this.sql);
            }
            else if (Request.QueryString["actn"] == "resend")
            {
                // spawn a worker thread to send the emails
                var thread = new Thread(Bug.threadproc_notifications);
                thread.Start();
            }

            Response.Redirect("notifications.aspx");
        }
    }
}