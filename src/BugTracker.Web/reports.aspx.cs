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

    public partial class reports : Page
    {
        public DataSet ds;
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            if (this.security.user.is_admin || this.security.user.can_use_reports ||
                this.security.user.can_edit_reports)
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
		'<a target=''_blank'' href=''view_report.aspx?view=chart&id=' + convert(varchar, rp_id) + '''>pie</a>'
	when rp_chart_type = 'line' then
		'<a target=''_blank'' href=''view_report.aspx?view=chart&id=' + convert(varchar, rp_id) + '''>line</a>'
	when rp_chart_type = 'bar' then
		'<a target=''_blank'' href=''view_report.aspx?view=chart&id=' + convert(varchar, rp_id) + '''>bar</a>'
	else
		'&nbsp;' end [view<br>chart],
'<a target=''_blank'' href=''view_report.aspx?view=data&id=' + convert(varchar, rp_id) + '''>data</a>' [view<br>data]
$adm
from reports order by rp_desc";

            if (this.security.user.is_admin || this.security.user.can_edit_reports)
                sql = sql.Replace("$adm", ", " +
                                          "'<a href=''edit_report.aspx?id=' + convert(varchar, rp_id) + '''>edit</a>' [edit], " +
                                          "'<a href=''delete_report.aspx?id=' + convert(varchar, rp_id) + '''>delete</a>' [delete] ");
            else
                sql = sql.Replace("$adm", "");

            this.ds = DbUtil.get_dataset(sql);
        }
    }
}