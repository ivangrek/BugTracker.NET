/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Bugs
{
    using System;
    using System.Data;
    using System.Web.UI;
    using Core;

    public partial class Flag : Page
    {
        public ISecurity Security { get; set; }

        protected string Sql {get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            if (!Security.User.IsGuest)
                if (Request.QueryString["ses"] != (string) Session["session_cookie"])
                {
                    Response.Write("session in URL doesn't match session cookie");
                    Response.End();
                }

            var dv = (DataView) Session["bugs"];
            if (dv == null) Response.End();

            var bugid = Convert.ToInt32(Util.SanitizeInteger(Request["bugid"]));

            var permissionLevel = Bug.GetBugPermissionLevel(bugid, Security);
            if (permissionLevel == SecurityPermissionLevel.PermissionNone) Response.End();

            for (var i = 0; i < dv.Count; i++)
                if ((int) dv[i][1] == bugid)
                {
                    var flag = Convert.ToInt32(Util.SanitizeInteger(Request["flag"]));
                    dv[i]["$FLAG"] = flag;

                    this.Sql = @"
if not exists (select bu_bug from bug_user where bu_bug = $bg and bu_user = $us)
    insert into bug_user (bu_bug, bu_user, bu_flag, bu_seen, bu_vote) values($bg, $us, 1, 0, 0) 
update bug_user set bu_flag = $fl, bu_flag_datetime = getdate() where bu_bug = $bg and bu_user = $us and bu_flag <> $fl";

                    this.Sql = this.Sql.Replace("$bg", Convert.ToString(bugid));
                    this.Sql = this.Sql.Replace("$us", Convert.ToString(Security.User.Usid));
                    this.Sql = this.Sql.Replace("$fl", Convert.ToString(flag));

                    DbUtil.ExecuteNonQuery(this.Sql);
                    break;
                }
        }
    }
}