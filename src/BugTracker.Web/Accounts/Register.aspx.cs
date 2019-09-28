/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Accounts
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class Register : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.SetContext(HttpContext.Current);
            Util.DoNotCache(Response);

            if (!ApplicationSettings.AllowSelfRegistration)
            {
                Response.Write("Sorry, Web.config AllowSelfRegistration is set to 0");
                Response.End();
            }

            if (!IsPostBack)
            {
                Page.Title = $"{ApplicationSettings.AppTitle} - register";
            }
            else
            {
                this.msg.InnerHtml = "&nbsp;";
                this.username_err.InnerHtml = "&nbsp;";
                this.email_err.InnerHtml = "&nbsp;";
                this.password_err.InnerHtml = "&nbsp;";
                this.confirm_err.InnerHtml = "&nbsp;";
                this.firstname_err.InnerHtml = "&nbsp;";
                this.lastname_err.InnerHtml = "&nbsp;";

                var valid = ValidateForm();

                if (!valid)
                {
                    this.msg.InnerHtml = "Registration was not submitted.";
                }
                else
                {
                    var guid = Guid.NewGuid().ToString();

                    // encrypt the password
                    var random = new Random();
                    var salt = random.Next(10000, 99999);
                    var encrypted = Util.EncryptStringUsingMd5(this.password.Value + Convert.ToString(salt));

                    var sql = @"
insert into emailed_links
    (el_id, el_date, el_email, el_action,
        el_username, el_salt, el_password, el_firstname, el_lastname)
    values ('$guid', getdate(), N'$email', N'register',
        N'$username', $salt, N'$password', N'$firstname', N'$lastname')";

                    sql = sql.Replace("$guid", guid);
                    sql = sql.Replace("$password", encrypted);
                    sql = sql.Replace("$salt", Convert.ToString(salt));
                    sql = sql.Replace("$username", this.username.Value.Replace("'", "''"));
                    sql = sql.Replace("$email", this.email.Value.Replace("'", "''"));
                    sql = sql.Replace("$firstname", this.firstname.Value.Replace("'", "''"));
                    sql = sql.Replace("$lastname", this.lastname.Value.Replace("'", "''"));

                    DbUtil.ExecuteNonQuery(sql);

                    var result = Email.SendEmail(this.email.Value,
                        ApplicationSettings.NotificationEmailFrom,
                        "", // cc
                        "Please complete registration",
                        "Click to <a href='"
                        + ApplicationSettings.AbsoluteUrlPrefix
                        + ResolveUrl("~/Accounts/CompleteRegistration.aspx?id=")
                        + guid
                        + "'>complete registration</a>.",
                        BtnetMailFormat.Html);

                    this.msg.InnerHtml = "An email has been sent to " + this.email.Value;
                    this.msg.InnerHtml += "<br>Please click on the link in the email message to complete registration.";
                }
            }
        }

        public bool ValidateForm()
        {
            var valid = true;

            if (this.username.Value == "")
            {
                this.username_err.InnerText = "Username is required.";
                valid = false;
            }

            if (this.email.Value == "")
            {
                this.email_err.InnerText = "Email is required.";
                valid = false;
            }
            else
            {
                if (!Util.ValidateEmail(this.email.Value))
                {
                    this.email_err.InnerHtml = "Format of email address is invalid.";
                    valid = false;
                }
            }

            if (this.password.Value == "")
            {
                this.password_err.InnerText = "Password is required.";
                valid = false;
            }

            if (this.confirm.Value == "")
            {
                this.confirm_err.InnerText = "Confirm password is required.";
                valid = false;
            }

            if (this.password.Value != "" && this.confirm.Value != "")
            {
                if (this.password.Value != this.confirm.Value)
                {
                    this.confirm_err.InnerText = "Confirm doesn't match password.";
                    valid = false;
                }
                else if (!Util.CheckPasswordStrength(this.password.Value))
                {
                    this.password_err.InnerHtml = "Password is not difficult enough to guess.";
                    this.password_err.InnerHtml += "<br>Avoid common words.";
                    this.password_err.InnerHtml +=
                        "<br>Try using a mixture of lowercase, uppercase, digits, and special characters.";
                    valid = false;
                }
            }

            if (this.firstname.Value == "")
            {
                this.firstname_err.InnerText = "Firstname is required.";
                valid = false;
            }

            if (this.lastname.Value == "")
            {
                this.lastname_err.InnerText = "Lastname is required.";
                valid = false;
            }

            // check for dupes

            var sql = @"
declare @user_cnt int
declare @email_cnt int
declare @pending_user_cnt int
declare @pending_email_cnt int
select @user_cnt = count(1) from users where us_username = N'$us'
select @email_cnt = count(1) from users where us_email = N'$em'
select @pending_user_cnt = count(1) from emailed_links where el_username = N'$us'
select @pending_email_cnt = count(1) from emailed_links where el_email = N'$em'
select @user_cnt, @email_cnt, @pending_user_cnt, @pending_email_cnt";
            sql = sql.Replace("$us", this.username.Value.Replace("'", "''"));
            sql = sql.Replace("$em", this.email.Value.Replace("'", "''"));

            var dr = DbUtil.GetDataRow(sql);

            if ((int) dr[0] > 0)
            {
                this.username_err.InnerText = "Username already being used. Choose another.";
                valid = false;
            }

            if ((int) dr[1] > 0)
            {
                this.email_err.InnerText = "Email already being used. Choose another.";
                valid = false;
            }

            if ((int) dr[2] > 0)
            {
                this.username_err.InnerText = "Registration pending for this username. Choose another.";
                valid = false;
            }

            if ((int) dr[3] > 0)
            {
                this.email_err.InnerText = "Registration pending for this email. Choose another.";
                valid = false;
            }

            return valid;
        }
    }
}