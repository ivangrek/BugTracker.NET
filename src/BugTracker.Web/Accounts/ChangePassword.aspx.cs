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

    public partial class ChangePassword : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.SetContext(HttpContext.Current);
            Util.DoNotCache(Response);

            if (!IsPostBack)
            {
                Page.Title = $"{ApplicationSettings.AppTitle}- change password";
            }
            else
            {
                this.msg.InnerHtml = "";

                if (string.IsNullOrEmpty(this.password.Value))
                {
                    this.msg.InnerHtml = "Enter your password twice.";
                }
                else if (this.password.Value != this.confirm.Value)
                {
                    this.msg.InnerHtml = "Re-entered password doesn't match password.";
                }
                else if (!Util.CheckPasswordStrength(this.password.Value))
                {
                    this.msg.InnerHtml = "Password is not difficult enough to guess.";
                    this.msg.InnerHtml += "<br>Avoid common words.";
                    this.msg.InnerHtml +=
                        "<br>Try using a mixture of lowercase, uppercase, digits, and special characters.";
                }
                else
                {
                    var guid = Request["id"];

                    if (string.IsNullOrEmpty(guid))
                    {
                        Response.Write("no guid");
                        Response.End();
                    }

                    var sql = @"
declare @expiration datetime
set @expiration = dateadd(n,-$minutes,getdate())

select *,
    case when el_date < @expiration then 1 else 0 end [expired]
    from emailed_links
    where el_id = '$guid'

delete from emailed_links
    where el_date < dateadd(n,-240,getdate())";

                    sql = sql.Replace("$minutes", ApplicationSettings.RegistrationExpiration.ToString());
                    sql = sql.Replace("$guid", guid.Replace("'", "''"));

                    var dr = DbUtil.GetDataRow(sql);

                    if (dr == null)
                    {
                        this.msg.InnerHtml =
                            "The link you clicked on is expired or invalid.<br>Please start over again.";
                    }
                    else if ((int) dr["expired"] == 1)
                    {
                        this.msg.InnerHtml = "The link you clicked has expired.<br>Please start over again.";
                    }
                    else
                    {
                        Util.UpdateUserPassword((int) dr["el_user_id"], this.password.Value);
                        this.msg.InnerHtml = "Your password has been changed.";
                    }
                }
            }
        }
    }
}