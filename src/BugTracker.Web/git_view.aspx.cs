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

    public partial class git_view : Page
    {
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);
            Response.ContentType = "text/plain";

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            var sql = @"
select gitcom_commit, gitcom_bug, gitcom_repository, gitap_path 
from git_commits
inner join git_affected_paths on gitap_gitcom_id = gitcom_id
where gitap_id = $id";

            var gitap_id = Convert.ToInt32(Util.sanitize_integer(Request["revpathid"]));
            sql = sql.Replace("$id", Convert.ToString(gitap_id));

            var dr = DbUtil.get_datarow(sql);

            // check if user has permission for this bug
            var permission_level = Bug.get_bug_permission_level((int) dr["gitcom_bug"], this.security);
            if (permission_level == Security.PERMISSION_NONE)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var repo = (string) dr["gitcom_repository"];
            var path = (string) dr["gitap_path"];
            var commit = Request["commit"];

            var text = VersionControl.git_get_file_contents(repo, commit, path);

            Response.Write(text);
        }
    }
}