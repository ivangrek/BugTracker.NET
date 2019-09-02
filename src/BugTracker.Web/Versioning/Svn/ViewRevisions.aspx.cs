/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Versioning.Svn
{
    using System;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class ViewRevisions : Page
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
    '<a target=_blank href=" + ResolveUrl("~/Versioning/Svn/Diff.aspx") + @"?revpathid=' + convert(varchar,svnap_id) + '>diff</a>'
    else
    ''
end [view<br>diff],

case when svnap_action not like '%D%' then
'<a target=_blank href=" + ResolveUrl("~/Versioning/Svn/Log.aspx") + @"?revpathid=' + convert(varchar,svnap_id) + '>history</a>'
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

            sql = sql.Replace("$bg", Convert.ToString(this.Bugid));

            this.Ds = DbUtil.GetDataSet(sql);
        }
    }
}