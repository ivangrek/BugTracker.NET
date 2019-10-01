/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Versioning.Hg
{
    using System;
    using System.Data;
    using System.Web.UI;
    using Core;

    public partial class ViewRevisions : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public int Bugid;
        public DataSet Ds;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            this.Bugid = Convert.ToInt32(Util.SanitizeInteger(Request["id"]));

            var permissionLevel = Bug.GetBugPermissionLevel(this.Bugid, Security);
            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            Page.Title = $"{ApplicationSettings.AppTitle} - view hg file revisions";

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
    '<a target=_blank href=" + ResolveUrl("~/Versioning/Hg/Diff.aspx") + @"?revpathid=' + convert(varchar,hgap_id) + '>diff</a>'
    else
    ''
end [view<br>diff],

case when hgap_action not like '%D%' then
'<a target=_blank href=" + ResolveUrl("~/Versioning/Hg/Log.aspx") + @"?revpathid=' + convert(varchar,hgap_id) + '>history</a>'
    else
    ''
end [view<br>history<br>(hg log)]

from hg_revisions
inner join hg_affected_paths on hgap_hgrev_id = hgrev_id
where hgrev_bug = $bg
order by hgrev_hg_date desc, hgap_path";

            sql = sql.Replace("$bg", Convert.ToString(this.Bugid));

            this.Ds = DbUtil.GetDataSet(sql);
        }
    }
}