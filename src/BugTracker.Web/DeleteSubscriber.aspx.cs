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

    public partial class DeleteSubscriber : Page
    {
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);

            if (Request.QueryString["ses"] != (string) Session["session_cookie"])
            {
                Response.Write("session in URL doesn't match session cookie");
                Response.End();
            }

            var sql = "delete from bug_subscriptions where bs_bug = $bg_id and bs_user = $us_id";
            sql = sql.Replace("$bg_id", Util.SanitizeInteger(Request["bg_id"]));
            sql = sql.Replace("$us_id", Util.SanitizeInteger(Request["us_id"]));
            DbUtil.ExecuteNonQuery(sql);

            Response.Redirect("ViewSubscribers.aspx?id=" + Util.SanitizeInteger(Request["bg_id"]));
        }
    }
}