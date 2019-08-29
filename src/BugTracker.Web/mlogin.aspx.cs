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

    public partial class mlogin : Page
    {
        public string sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.set_context(HttpContext.Current);
            Util.do_not_cache(Response);

            if (Util.get_setting("EnableMobile", "0") == "0")
            {
                Response.Write("BugTracker.NET EnableMobile is not set to 1 in Web.config");
                Response.End();
            }

            this.msg.InnerText = "";

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - Logon";
            this.my_header.InnerText = Page.Title;

            // fill in the username first time in
            if (IsPostBack) on_logon();
        }

        public void on_logon()
        {
            var authenticated = Authenticate.check_password(this.user.Value, this.pw.Value);

            if (authenticated)
            {
                this.sql = "select us_id from users where us_username = N'$us'";
                this.sql = this.sql.Replace("$us", this.user.Value.Replace("'", "''"));
                var dr = DbUtil.get_datarow(this.sql);
                if (dr != null)
                {
                    var us_id = (int) dr["us_id"];

                    Security.create_session(
                        Request,
                        Response,
                        us_id, this.user.Value,
                        "0");

                    Util.redirect(Request, Response);
                }
            }
            else
            {
                this.msg.InnerText = "Invalid User or Password.";
            }
        }
    }
}