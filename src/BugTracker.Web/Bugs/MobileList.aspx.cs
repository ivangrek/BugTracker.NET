/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Bugs
{
    using System;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class MobileList : Page
    {
        public DataSet Ds;
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.AnyUserOk);
            if (Util.GetSetting("EnableMobile", "0") == "0")
            {
                Response.Write("BugTracker.NET EnableMobile is not set to 1 in Web.config");
                Response.End();
            }

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - List ";
            this.my_header.InnerText = Page.Title;
            this.create.InnerText =
                "Create " + Util.CapitalizeFirstLetter(Util.GetSetting("SingularBugLabel", "bug"));
            this.only_mine_label.InnerText = "Show only " + Util.GetSetting("PluralBugLabel", "bugs") +
                                             " reported by or assigned to me";

            var bugSql = @"
select top 200
bg_id [id],
bg_short_desc [desc], 
pj_name [project],
rpt.us_username [reported_user],
asg.us_username [assigned_user],
st_name [status],
bg_last_updated_date [last_updated]
from bugs
left outer join users rpt on rpt.us_id = bg_reported_user
left outer join users asg on asg.us_id = bg_assigned_to_user
--left outer join users lu on lu.us_id = bg_last_updated_user
left outer join projects on pj_id = bg_project
--left outer join orgs on og_id = bg_org
--left outer join categories on ct_id = bg_category
--left outer join priorities on pr_id = bg_priority
left outer join statuses on st_id = bg_status
$WHERE$
order by bg_last_updated_date desc";

            if (this.only_mine.Checked)
                bugSql = bugSql.Replace("$WHERE$",
                    "where bg_reported_user = "
                    + Convert.ToString(this.Security.User.Usid)
                    + " or bg_assigned_to_user = "
                    + Convert.ToString(this.Security.User.Usid));
            else
                bugSql = bugSql.Replace("$WHERE$", "");

            bugSql = Util.AlterSqlPerProjectPermissions(bugSql, this.Security);

            this.Ds = DbUtil.GetDataSet(bugSql);
        }
    }
}