/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Reports
{
    using System;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class List : Page
    {
        public DataSet Ds;
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.AnyUserOk);

            if (this.Security.User.IsAdmin || this.Security.User.CanUseReports ||
                this.Security.User.CanEditReports)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "reports";

            var sql = @"
select
rp_desc [report],
case
    when rp_chart_type = 'pie' then
        '<a target=''_blank'' href=''" + ResolveUrl("~/Reports/View.aspx") + @"?view=chart&id=' + convert(varchar, rp_id) + '''>pie</a>'
    when rp_chart_type = 'line' then
        '<a target=''_blank'' href=''" + ResolveUrl("~/Reports/View.aspx") + @"?view=chart&id=' + convert(varchar, rp_id) + '''>line</a>'
    when rp_chart_type = 'bar' then
        '<a target=''_blank'' href=''" + ResolveUrl("~/Reports/View.aspx") + @"?view=chart&id=' + convert(varchar, rp_id) + '''>bar</a>'
    else
        '&nbsp;' end [view<br>chart],
'<a target=''_blank'' href=''" + ResolveUrl("~/Reports/View.aspx") + @"?view=data&id=' + convert(varchar, rp_id) + '''>data</a>' [view<br>data]
$adm
from reports order by rp_desc";

            if (this.Security.User.IsAdmin || this.Security.User.CanEditReports)
                sql = sql.Replace("$adm", ", " +
                                          "'<a href=''" + ResolveUrl("~/Reports/Edit.aspx") +"?id=' + convert(varchar, rp_id) + '''>edit</a>' [edit], " +
                                          "'<a href=''" + ResolveUrl("~/Reports/Delete.aspx") + "?id=' + convert(varchar, rp_id) + '''>delete</a>' [delete] ");
            else
                sql = sql.Replace("$adm", "");

            this.Ds = DbUtil.GetDataSet(sql);
        }
    }
}