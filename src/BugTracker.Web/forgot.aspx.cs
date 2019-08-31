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

    public partial class Forgot : Page
    {
        public void Page_Load(object sender, EventArgs e)
        {
            Util.SetContext(HttpContext.Current);
            Util.DoNotCache(Response);

            if (Util.GetSetting("ShowForgotPasswordLink", "0") == "0")
            {
                Response.Write("Sorry, Web.config ShowForgotPasswordLink is set to 0");
                Response.End();
            }

            if (!IsPostBack)
            {
                Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                            + "forgot password";
            }
            else
            {
                this.msg.InnerHtml = "";

                if (this.email.Value == "" && this.username.Value == "")
                {
                    this.msg.InnerHtml = "Enter either your Username or your Email address.";
                }
                else if (this.email.Value != "" && !Util.ValidateEmail(this.email.Value))
                {
                    this.msg.InnerHtml = "Format of email address is invalid.";
                }
                else
                {
                    var userCount = 0;
                    var userId = 0;

                    if (this.email.Value != "" && this.username.Value == "")
                    {
                        // check if email exists
                        userCount = (int) DbUtil.ExecuteScalar(
                            "select count(1) from users where us_email = N'" + this.email.Value.Replace("'", "''") +
                            "'");

                        if (userCount == 1)
                            userId = (int) DbUtil.ExecuteScalar(
                                "select us_id from users where us_email = N'" + this.email.Value.Replace("'", "''") +
                                "'");
                    }
                    else if (this.email.Value == "" && this.username.Value != "")
                    {
                        // check if email exists
                        userCount = (int) DbUtil.ExecuteScalar(
                            "select count(1) from users where isnull(us_email,'') != '' and  us_username = N'" +
                            this.username.Value.Replace("'", "''") + "'");

                        if (userCount == 1)
                            userId = (int) DbUtil.ExecuteScalar(
                                "select us_id from users where us_username = N'" +
                                this.username.Value.Replace("'", "''") +
                                "'");
                    }
                    else if (this.email.Value != "" && this.username.Value != "")
                    {
                        // check if email exists
                        userCount = (int) DbUtil.ExecuteScalar(
                            "select count(1) from users where us_username = N'" +
                            this.username.Value.Replace("'", "''") +
                            "' and us_email = N'"
                            + this.email.Value.Replace("'", "''") + "'");

                        if (userCount == 1)
                            userId = (int) DbUtil.ExecuteScalar(
                                "select us_id from users where us_username = N'" +
                                this.username.Value.Replace("'", "''") +
                                "' and us_email = N'"
                                + this.email.Value.Replace("'", "''") + "'");
                    }

                    if (userCount == 1)
                    {
                        var guid = Guid.NewGuid().ToString();
                        var sql = @"
declare @username nvarchar(255)
declare @email nvarchar(255)

select @username = us_username, @email = us_email
	from users where us_id = $user_id

insert into emailed_links
	(el_id, el_date, el_email, el_action, el_user_id)
	values ('$guid', getdate(), @email, N'forgot', $user_id)

select @username us_username, @email us_email";

                        sql = sql.Replace("$guid", guid);
                        sql = sql.Replace("$user_id", Convert.ToString(userId));

                        var dr = DbUtil.GetDataRow(sql);

                        var result = Email.SendEmail(
                            (string) dr["us_email"],
                            Util.GetSetting("NotificationEmailFrom", ""),
                            "", // cc
                            "reset password",
                            "Click to <a href='"
                            + Util.GetSetting("AbsoluteUrlPrefix", "")
                            + "ChangePassword.aspx?id="
                            + guid
                            + "'>reset password</a> for user \""
                            + (string) dr["us_username"]
                            + "\".",
                            BtnetMailFormat.Html);

                        if (result == "")
                        {
                            this.msg.InnerHtml = "An email with password info has been sent to you.";
                        }
                        else
                        {
                            this.msg.InnerHtml = "There was a problem sending the email.";
                            this.msg.InnerHtml += "<br>" + result;
                        }
                    }
                    else
                    {
                        this.msg.InnerHtml =
                            "Unknown username or email address.<br>Are you sure you spelled everything correctly?<br>Try just username, just email, or both.";
                    }
                }
            }
        }
    }
}