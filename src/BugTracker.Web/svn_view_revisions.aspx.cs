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

    public partial class svn_view_revisions : Page
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
                                                                        + "view svn file revisions";

            var sql = @"
select
svnrev_revision [revision],
svnrev_repository [repository],
svnap_action [action],
svnap_path [file],
svnrev_author [user],
svnrev_svn_date [revision date],
replace(substring(svnrev_msg,1,4000),char(13),'<br>') [msg],

case when svnap_action not like '%D%' and svnap_action not like 'A%' then
	'<a target=_blank href=svn_diff.aspx?revpathid=' + convert(varchar,svnap_id) + '>diff</a>'
	else
	''
end [view<br>diff],

case when svnap_action not like '%D%' then
'<a target=_blank href=svn_log.aspx?revpathid=' + convert(varchar,svnap_id) + '>history</a>'
	else
	''
end [view<br>history<br>(svn log)]";

            //	if (websvn_url != "")
            //	{
            //		sql += ",\n '<a target=_blank href=\"" + websvn_url + "\">WebSvn</a>' [WebSvn<br>URL]";
            //		sql = sql.Replace("$PATH","' + svnap_path + '");
            //		sql = sql.Replace("$REV", "' + convert(varchar,svnrev_revision) + '");
            //	}

            sql += @"
		from svn_revisions
		inner join svn_affected_paths on svnap_svnrev_id = svnrev_id
		where svnrev_bug = $bg
		order by svnrev_revision desc, svnap_path";

            sql = sql.Replace("$bg", Convert.ToString(this.bugid));

            this.ds = DbUtil.get_dataset(sql);
        }
    }
}