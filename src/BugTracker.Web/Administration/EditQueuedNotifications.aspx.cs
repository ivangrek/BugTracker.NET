/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration
{
    using System;
    using System.Threading;
    using System.Web.UI;
    using Core;

    public partial class EditQueuedNotifications : Page
    {
        public ISecurity Security { get; set; }

        protected string Sql {get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.MustBeAdmin);

            if (Request.QueryString["ses"] != (string) Session["session_cookie"])
            {
                Response.Write("session in URL doesn't match session cookie");
                Response.End();
            }

            if (Request.QueryString["actn"] == "delete")
            {
                this.Sql = @"delete from queued_notifications where qn_status = N'not sent'";
                DbUtil.ExecuteNonQuery(this.Sql);
            }
            else if (Request.QueryString["actn"] == "reset")
            {
                this.Sql = @"update queued_notifications set qn_retries = 0 where qn_status = N'not sent'";
                DbUtil.ExecuteNonQuery(this.Sql);
            }
            else if (Request.QueryString["actn"] == "resend")
            {
                // spawn a worker thread to send the emails
                var thread = new Thread(Bug.ThreadProcNotifications);
                thread.Start();
            }

            Response.Redirect("~/Administration/Notifications.aspx");
        }
    }
}