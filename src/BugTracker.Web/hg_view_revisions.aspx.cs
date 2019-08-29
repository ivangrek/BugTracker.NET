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

    public partial class hg_view_revisions : Page
    {
        public int bugid;
        public DataSet ds;

        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            this.bugid = Convert.ToInt32(Util.sanitize_integer(Request["id"]));

            var permission_level = Bug.get_bug_permission_level(this.bugid, this.security);
            if (permission_level == Security.PERMISSION_NONE)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "view hg file revisions";

            var sql = @"
select 
hgrev_revision [revision],
hgrev_repository [repo],
hgap_action [action],
hgap_path [file],
replace(replace(hgrev_author,'<','&lt;'),'>','&gt;') [user],
substring(hgrev_hg_date,1,19) [date],
replace(substring(hgrev_msg,1,4000),char(13),'<br>') [msg],

case when hgap_action not like '%D%' and hgap_action not like 'A%' then
	'<a target=_blank href=hg_diff.aspx?revpathid=' + convert(varchar,hgap_id) + '>diff</a>'
	else
	''
end [view<br>diff],

case when hgap_action not like '%D%' then
'<a target=_blank href=hg_log.aspx?revpathid=' + convert(varchar,hgap_id) + '>history</a>'
	else
	''
end [view<br>history<br>(hg log)]

from hg_revisions
inner join hg_affected_paths on hgap_hgrev_id = hgrev_id
where hgrev_bug = $bg
order by hgrev_hg_date desc, hgap_path";

            sql = sql.Replace("$bg", Convert.ToString(this.bugid));

            this.ds = DbUtil.get_dataset(sql);
        }
    }
}