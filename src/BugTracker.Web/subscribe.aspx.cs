/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Web.UI;
    using Core;

    public partial class Subscribe : Page
    {
        public ISecurity Security { get; set; }

        public string Sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            var bugid = Convert.ToInt32(Request["id"]);
            var permissionLevel = Bug.GetBugPermissionLevel(bugid, Security);
            if (permissionLevel == SecurityPermissionLevel.PermissionNone) Response.End();

            if (Request.QueryString["ses"] != (string) Session["session_cookie"])
            {
                Response.Write("session in URL doesn't match session cookie");
                Response.End();
            }

            if (Request.QueryString["actn"] == "1")
                this.Sql = @"insert into bug_subscriptions (bs_bug, bs_user)
			values($bg, $us)";
            else
                this.Sql = @"delete from bug_subscriptions
			where bs_bug = $bg and bs_user = $us";

            this.Sql = this.Sql.Replace("$bg", Util.SanitizeInteger(Request["id"]));
            this.Sql = this.Sql.Replace("$us", Convert.ToString(Security.User.Usid));
            DbUtil.ExecuteNonQuery(this.Sql);
        }
    }
}