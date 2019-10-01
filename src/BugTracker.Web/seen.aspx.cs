/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.Web.UI;
    using Core;

    public partial class Seen : Page
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
                    var seen = Convert.ToInt32(Util.SanitizeInteger(Request["seen"]));
                    dv[i]["$SEEN"] = seen;
                    this.Sql = @"
if not exists (select bu_bug from bug_user where bu_bug = $bg and bu_user = $us)
	insert into bug_user (bu_bug, bu_user, bu_flag, bu_seen, bu_vote) values($bg, $us, 0, 1, 0) 
update bug_user set bu_seen = $seen, bu_seen_datetime = getdate() where bu_bug = $bg and bu_user = $us and bu_seen <> $seen";

                    this.Sql = this.Sql.Replace("$seen", Convert.ToString(seen));
                    this.Sql = this.Sql.Replace("$bg", Convert.ToString(bugid));
                    this.Sql = this.Sql.Replace("$us", Convert.ToString(Security.User.Usid));

                    DbUtil.ExecuteNonQuery(this.Sql);

                    break;
                }
        }
    }
}