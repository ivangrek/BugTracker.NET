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

    public partial class flag : Page
    {
        public Security security;
        public string sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            if (!this.security.user.is_guest)
                if (Request.QueryString["ses"] != (string) Session["session_cookie"])
                {
                    Response.Write("session in URL doesn't match session cookie");
                    Response.End();
                }

            var dv = (DataView) Session["bugs"];
            if (dv == null) Response.End();

            var bugid = Convert.ToInt32(Util.sanitize_integer(Request["bugid"]));

            var permission_level = Bug.get_bug_permission_level(bugid, this.security);
            if (permission_level == Security.PERMISSION_NONE) Response.End();

            for (var i = 0; i < dv.Count; i++)
                if ((int) dv[i][1] == bugid)
                {
                    var flag = Convert.ToInt32(Util.sanitize_integer(Request["flag"]));
                    dv[i]["$FLAG"] = flag;

                    this.sql = @"
if not exists (select bu_bug from bug_user where bu_bug = $bg and bu_user = $us)
	insert into bug_user (bu_bug, bu_user, bu_flag, bu_seen, bu_vote) values($bg, $us, 1, 0, 0) 
update bug_user set bu_flag = $fl, bu_flag_datetime = getdate() where bu_bug = $bg and bu_user = $us and bu_flag <> $fl";

                    this.sql = this.sql.Replace("$bg", Convert.ToString(bugid));
                    this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));
                    this.sql = this.sql.Replace("$fl", Convert.ToString(flag));

                    DbUtil.execute_nonquery(this.sql);
                    break;
                }
        }
    }
}