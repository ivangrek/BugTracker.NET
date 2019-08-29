/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class svn_view : Page
    {
        public string repo;
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);
            Response.ContentType = "text/plain";

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            // get info about revision

            var sql = @"
select svnrev_revision, svnrev_repository, svnap_path, svnrev_bug
from svn_revisions
inner join svn_affected_paths on svnap_svnrev_id = svnrev_id
where svnap_id = $id
order by svnrev_revision desc, svnap_path";

            var svnap_id = Convert.ToInt32(Util.sanitize_integer(Request["revpathid"]));
            var string_affected_path_id = Convert.ToString(svnap_id);

            sql = sql.Replace("$id", string_affected_path_id);

            var dr = DbUtil.get_datarow(sql);

            // check if user has permission for this bug
            var permission_level = Bug.get_bug_permission_level((int) dr["svnrev_bug"], this.security);
            if (permission_level == Security.PERMISSION_NONE)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var revision = Convert.ToInt32(Request["rev"]);

            this.repo = (string) dr["svnrev_repository"];
            string path;
            if (Util.get_setting("SvnTrustPathsInUrls", "0") == "1")
                path = Request["path"];
            else
                path = (string) dr["svnap_path"];

            var raw_text = VersionControl.svn_cat(this.repo, path, revision);

            Response.Write(raw_text);
        }
    }
}