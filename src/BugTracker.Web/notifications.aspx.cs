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

    public partial class Notifications : Page
    {
        public DataSet Ds;

        public Security Security;
        public string Ses;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "queued notifications";

            this.Ds = DbUtil.GetDataSet(
                @"select
		qn_id [id],
		qn_date_created [date created],
		qn_to [to],
		qn_bug [bug],
		qn_status [status],
		qn_retries [retries],
		qn_last_exception [last error]
		from queued_notifications
		order by id;");

            this.Ses = (string) Session["session_cookie"];
        }
    }
}