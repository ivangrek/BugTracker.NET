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

    public partial class GitViewRevisions : Page
    {
        public int Bugid;
        public DataSet Ds;

        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.AnyUserOk);

            this.Bugid = Convert.ToInt32(Util.SanitizeInteger(Request["id"]));

            var permissionLevel = Bug.GetBugPermissionLevel(this.Bugid, this.Security);
            if (permissionLevel == Security.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "view git file commits";

            var sql = @"
select 
gitcom_commit [commit],
gitcom_repository [repo],
gitap_action [action],
gitap_path [file],
replace(replace(gitcom_author,'<','&lt;'),'>','&gt;') [user],
substring(gitcom_git_date,1,19) [date],
replace(substring(gitcom_msg,1,4000),char(13),'<br>') [msg],

case when gitap_action not like '%D%' and gitap_action not like 'A%' then
	'<a target=_blank href=GitDiff.aspx?revpathid=' + convert(varchar,gitap_id) + '>diff</a>'
	else
	''
end [view<br>diff],

case when gitap_action not like '%D%' then
'<a target=_blank href=GitLog.aspx?revpathid=' + convert(varchar,gitap_id) + '>history</a>'
	else
	''
end [view<br>history<br>(git log)]

from git_commits
inner join git_affected_paths on gitap_gitcom_id = gitcom_id
where gitcom_bug = $bg
order by gitcom_git_date desc, gitap_path";

            sql = sql.Replace("$bg", Convert.ToString(this.Bugid));

            this.Ds = DbUtil.GetDataSet(sql);
        }
    }
}