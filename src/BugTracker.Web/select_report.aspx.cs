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

    public partial class select_report : Page
    {
        public DataSet ds;
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            if (this.security.user.is_admin || this.security.user.can_use_reports)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "reports";

            var sql = @"
select
rp_desc [report],
case
	when rp_chart_type = 'pie' then
		'<a href=''javascript:select_report(""pie"",' + convert(varchar, rp_id) + ')''>select pie</a>'
	when rp_chart_type = 'line' then
		'<a href=''javascript:select_report(""line"",' + convert(varchar, rp_id) + ')''>select line</a>'
	when rp_chart_type = 'bar' then
		'<a href=''javascript:select_report(""bar"",' + convert(varchar, rp_id) + ')''>select bar</a>'
	else
		'&nbsp;' end [chart],
'<a href=''javascript:select_report(""data"",' + convert(varchar, rp_id) + ')''>select data</a>' [data]
from reports order by rp_desc";

            this.ds = DbUtil.get_dataset(sql);
        }
    }
}