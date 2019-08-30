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

    public partial class Subscribe : Page
    {
        public Security Security;
        public string Sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.AnyUserOkExceptGuest);

            var bugid = Convert.ToInt32(Request["id"]);
            var permissionLevel = Bug.GetBugPermissionLevel(bugid, this.Security);
            if (permissionLevel == Security.PermissionNone) Response.End();

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
            this.Sql = this.Sql.Replace("$us", Convert.ToString(this.Security.User.Usid));
            DbUtil.ExecuteNonQuery(this.Sql);
        }
    }
}