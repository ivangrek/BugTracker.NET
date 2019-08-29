/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class mbugs : Page
    {
        public DataSet ds;
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);
            if (Util.get_setting("EnableMobile", "0") == "0")
            {
                Response.Write("BugTracker.NET EnableMobile is not set to 1 in Web.config");
                Response.End();
            }

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - List ";
            this.my_header.InnerText = Page.Title;
            this.create.InnerText =
                "Create " + Util.capitalize_first_letter(Util.get_setting("SingularBugLabel", "bug"));
            this.only_mine_label.InnerText = "Show only " + Util.get_setting("PluralBugLabel", "bugs") +
                                             " reported by or assigned to me";

            var bug_sql = @"
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
                bug_sql = bug_sql.Replace("$WHERE$",
                    "where bg_reported_user = "
                    + Convert.ToString(this.security.user.usid)
                    + " or bg_assigned_to_user = "
                    + Convert.ToString(this.security.user.usid));
            else
                bug_sql = bug_sql.Replace("$WHERE$", "");

            bug_sql = Util.alter_sql_per_project_permissions(bug_sql, this.security);

            this.ds = DbUtil.get_dataset(bug_sql);
        }
    }
}