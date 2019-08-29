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

    public partial class git_view_revisions : Page
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
	'<a target=_blank href=git_diff.aspx?revpathid=' + convert(varchar,gitap_id) + '>diff</a>'
	else
	''
end [view<br>diff],

case when gitap_action not like '%D%' then
'<a target=_blank href=git_log.aspx?revpathid=' + convert(varchar,gitap_id) + '>history</a>'
	else
	''
end [view<br>history<br>(git log)]

from git_commits
inner join git_affected_paths on gitap_gitcom_id = gitcom_id
where gitcom_bug = $bg
order by gitcom_git_date desc, gitap_path";

            sql = sql.Replace("$bg", Convert.ToString(this.bugid));

            this.ds = DbUtil.get_dataset(sql);
        }
    }
}