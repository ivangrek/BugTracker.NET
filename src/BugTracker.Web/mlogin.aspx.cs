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

    public partial class MLogin : Page
    {
        public string Sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.SetContext(HttpContext.Current);
            Util.DoNotCache(Response);

            if (Util.GetSetting("EnableMobile", "0") == "0")
            {
                Response.Write("BugTracker.NET EnableMobile is not set to 1 in Web.config");
                Response.End();
            }

            this.msg.InnerText = "";

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - Logon";
            this.my_header.InnerText = Page.Title;

            // fill in the username first time in
            if (IsPostBack) on_logon();
        }

        public void on_logon()
        {
            var authenticated = Authenticate.CheckPassword(this.user.Value, this.pw.Value);

            if (authenticated)
            {
                this.Sql = "select us_id from users where us_username = N'$us'";
                this.Sql = this.Sql.Replace("$us", this.user.Value.Replace("'", "''"));
                var dr = DbUtil.GetDataRow(this.Sql);
                if (dr != null)
                {
                    var usId = (int) dr["us_id"];

                    Security.CreateSession(
                        Request,
                        Response,
                        usId, this.user.Value,
                        "0");

                    Util.Redirect(Request, Response);
                }
            }
            else
            {
                this.msg.InnerText = "Invalid User or Password.";
            }
        }
    }
}