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

    public partial class CompleteRegistration : Page
    {
        public void Page_Load(object sender, EventArgs e)
        {
            Util.SetContext(HttpContext.Current);
            Util.DoNotCache(Response);

            var guid = Request["id"];

            var sql = @"
declare @expiration datetime
set @expiration = dateadd(n,-$minutes,getdate())

select *,
    case when el_date < @expiration then 1 else 0 end [expired]
    from emailed_links
    where el_id = '$guid'

delete from emailed_links
    where el_date < dateadd(n,-240,getdate())";

            sql = sql.Replace("$minutes", Util.GetSetting("RegistrationExpiration", "20"));
            sql = sql.Replace("$guid", guid.Replace("'", "''"));

            var dr = DbUtil.GetDataRow(sql);

            if (dr == null)
            {
                this.msg.InnerHtml = "The link you clicked on is expired or invalid.<br>Please start over again.";
            }
            else if ((int) dr["expired"] == 1)
            {
                this.msg.InnerHtml = "The link you clicked has expired.<br>Please start over again.";
            }
            else
            {
                Core.User.CopyUser(
                    (string) dr["el_username"],
                    (string) dr["el_email"],
                    (string) dr["el_firstname"],
                    (string) dr["el_lastname"],
                    "",
                    (int) dr["el_salt"],
                    (string) dr["el_password"],
                    Util.GetSetting("SelfRegisteredUserTemplate", "[error - missing user template]"),
                    false);

                //  Delete the temp link
                sql = @"delete from emailed_links where el_id = '$guid'";
                sql = sql.Replace("$guid", guid.Replace("'", "''"));
                DbUtil.ExecuteNonQuery(sql);

                this.msg.InnerHtml = "Your registration is complete.";
            }
        }
    }
}