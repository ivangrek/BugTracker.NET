/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Accounts
{
    using System;
    using System.Data.SqlClient;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class Home : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public IAuthenticate Authenticate { get; set; }
        public ISecurity Security { get; set; }

        protected string Sql {get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.SetContext(HttpContext.Current);
            Util.DoNotCache(Response);

            Page.Title = $"{ApplicationSettings.AppTitle} - logon";

            this.msg.InnerText = "";

            // see if the connection string works
            try
            {
                // Intentionally getting an extra connection here so that we fall into the right "catch"
                var conn = DbUtil.GetSqlConnection();
                conn.Close();

                try
                {
                    DbUtil.ExecuteNonQuery("select count(1) from users");
                }
                catch (SqlException e1)
                {
                    Util.WriteToLog(e1.Message);
                    Util.WriteToLog(ApplicationSettings.ConnectionString);
                    this.msg.InnerHtml = "Unable to find \"bugs\" table.<br>"
                                         + "Click to <a href=Install.aspx>setup database tables</a>";
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
            var authMode = ApplicationSettings.WindowsAuthentication;
            var usernameCookie = Request.Cookies["user"];
            var previousAuthMode = "0";
            if (usernameCookie != null) previousAuthMode = usernameCookie["NTLM"];

            // If an error occured, then force the authentication to manual
            if (Request.QueryString["msg"] == null)
            {
                // If windows authentication only, then redirect
                if (authMode == 1) Util.Redirect("~/Accounts/LoginNt.aspx", Request, Response);

                // If previous login was with windows authentication, then try it again
                if (previousAuthMode == "1" && authMode == 2)
                {
                    Response.Cookies["user"]["name"] = "";
                    Response.Cookies["user"]["NTLM"] = "0";
                    Util.Redirect("~/Accounts/LoginNt.aspx", Request, Response);
                }
            }
            else
            {
                this.msg.InnerHtml = "Error during windows authentication:<br>" + HttpUtility.HtmlEncode(Request.QueryString["msg"]);
            }

            // fill in the username first time in
            if (!IsPostBack)
            {
                if (previousAuthMode == "0")
                {
                    if (Request.QueryString["user"] == null || Request.QueryString["password"] == null)
                    {
                        //	User name and password are not on the querystring.

                        if (usernameCookie != null)
                            //	Set the user name from the last logon.

                            this.user.Value = usernameCookie["name"];
                    }
                    else
                    {
                        //	User name and password have been passed on the querystring.

                        this.user.Value = Request.QueryString["user"];
                        this.pw.Value = Request.QueryString["password"];

                        OnLogon();
                    }
                }
            }
            else
            {
                OnLogon();
            }
        }

        public void OnLogon()
        {
            var authMode = ApplicationSettings.WindowsAuthentication;
            if (authMode != 0)
                if (this.user.Value.Trim() == "")
                    Util.Redirect("~/Accounts/LoginNt.aspx", Request, Response);

            var authenticated = Authenticate.CheckPassword(this.user.Value, this.pw.Value);

            if (authenticated)
            {
                this.Sql = "select us_id from users where us_username = N'$us'";
                this.Sql = this.Sql.Replace("$us", this.user.Value.Replace("'", "''"));
                var dr = DbUtil.GetDataRow(this.Sql);
                if (dr != null)
                {
                    var usId = (int)dr["us_id"];

                    Security.CreateSession(
                        Request,
                        Response,
                        usId, this.user.Value,
                        "0");

                    Util.Redirect(Request, Response);
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