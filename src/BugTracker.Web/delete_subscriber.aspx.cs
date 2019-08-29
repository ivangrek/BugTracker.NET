/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class delete_subscriber : Page
    {
        public Security security;

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

            var sql = "delete from bug_subscriptions where bs_bug = $bg_id and bs_user = $us_id";
            sql = sql.Replace("$bg_id", Util.sanitize_integer(Request["bg_id"]));
            sql = sql.Replace("$us_id", Util.sanitize_integer(Request["us_id"]));
            DbUtil.execute_nonquery(sql);

            Response.Redirect("view_subscribers.aspx?id=" + Util.sanitize_integer(Request["bg_id"]));
        }
    }
}