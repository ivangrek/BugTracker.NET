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

    public partial class subscribe : Page
    {
        public Security security;
        public string sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK_EXCEPT_GUEST);

            var bugid = Convert.ToInt32(Request["id"]);
            var permission_level = Bug.get_bug_permission_level(bugid, this.security);
            if (permission_level == Security.PERMISSION_NONE) Response.End();

            if (Request.QueryString["ses"] != (string) Session["session_cookie"])
            {
                Response.Write("session in URL doesn't match session cookie");
                Response.End();
            }

            if (Request.QueryString["actn"] == "1")
                this.sql = @"insert into bug_subscriptions (bs_bug, bs_user)
			values($bg, $us)";
            else
                this.sql = @"delete from bug_subscriptions
			where bs_bug = $bg and bs_user = $us";

            this.sql = this.sql.Replace("$bg", Util.sanitize_integer(Request["id"]));
            this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));
            DbUtil.execute_nonquery(this.sql);
        }
    }
}