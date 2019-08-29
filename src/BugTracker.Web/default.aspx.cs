/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data.SqlClient;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class Default : Page
    {
        public string sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.set_context(HttpContext.Current);

            Util.do_not_cache(Response);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "logon";

            this.msg.InnerText = "";

            // see if the connection string works
            try
            {
                // Intentionally getting an extra connection here so that we fall into the right "catch"
                var conn = DbUtil.get_sqlconnection();
                conn.Close();

                try
                {
                    DbUtil.execute_nonquery("select count(1) from users");
                }
                catch (SqlException e1)
                {
                    Util.write_to_log(e1.Message);
                    Util.write_to_log(Util.get_setting("ConnectionString", "?"));
                    this.msg.InnerHtml = "Unable to find \"bugs\" table.<br>"
                                         + "Click to <a href=install.aspx>setup database tables</a>";
                }
            }
            catch (SqlException e2)
            {
                this.msg.InnerHtml = "Unable to connect.<br>"
                                     + e2.Message + "<br>"
                                     + "Check Web.config file \"ConnectionString\" setting.<br>"
                                     + "Check also README.html<br>"
                                     + "Check also <a href=http://sourceforge.net/projects/btnet/forums/forum/226938>Help Forum</a> on Sourceforge.";
            }

            // Get authentication mode
            var auth_mode = Util.get_setting("WindowsAuthentication", "0");
            var username_cookie = Request.Cookies["user"];
            var previous_auth_mode = "0";
            if (username_cookie != null) previous_auth_mode = username_cookie["NTLM"];

            // If an error occured, then force the authentication to manual
            if (Request.QueryString["msg"] == null)
            {
                // If windows authentication only, then redirect
                if (auth_mode == "1") Util.redirect("loginNT.aspx", Request, Response);

                // If previous login was with windows authentication, then try it again
                if (previous_auth_mode == "1" && auth_mode == "2")
                {
                    Response.Cookies["user"]["name"] = "";
                    Response.Cookies["user"]["NTLM"] = "0";
                    Util.redirect("loginNT.aspx", Request, Response);
                }
            }
            else
            {
                if (Request.QueryString["msg"] != "logged off")
                    this.msg.InnerHtml = "Error during windows authentication:<br>"
                                         + HttpUtility.HtmlEncode(Request.QueryString["msg"]);
            }

            // fill in the username first time in
            if (!IsPostBack)
            {
                if (previous_auth_mode == "0")
                {
                    if (Request.QueryString["user"] == null || Request.QueryString["password"] == null)
                    {
                        //	User name and password are not on the querystring.

                        if (username_cookie != null)
                            //	Set the user name from the last logon.

                            this.user.Value = username_cookie["name"];
                    }
                    else
                    {
                        //	User name and password have been passed on the querystring.

                        this.user.Value = Request.QueryString["user"];
                        this.pw.Value = Request.QueryString["password"];

                        on_logon();
                    }
                }
            }
            else
            {
                on_logon();
            }
        }

        public void on_logon()
        {
            var auth_mode = Util.get_setting("WindowsAuthentication", "0");
            if (auth_mode != "0")
                if (this.user.Value.Trim() == "")
                    Util.redirect("loginNT.aspx", Request, Response);

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
                else
                {
                    // How could this happen?  If someday the authentication
                    // method uses, say LDAP, then check_password could return
                    // true, even though there's no user in the database";
                    this.msg.InnerText = "User not found in database";
                }
            }
            else
            {
                this.msg.InnerText = "Invalid User or Password.";
            }
        }
    }
}