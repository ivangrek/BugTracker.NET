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

    public partial class notifications : Page
    {
        public DataSet ds;

        public Security security;
        public string ses;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.MUST_BE_ADMIN);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "queued notifications";

            this.ds = DbUtil.get_dataset(
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

            this.ses = (string) Session["session_cookie"];
        }
    }
}