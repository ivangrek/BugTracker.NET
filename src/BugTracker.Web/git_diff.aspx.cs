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

    public partial class git_diff : Page
    {
        public string left_out = "";
        public string left_title = "";
        public string path = "";
        public string right_out = "";
        public string right_title = "";
        public Security security;
        public string unified_diff_text = "";

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Page.Title = "git diff " + HttpUtility.HtmlEncode(this.path);

            // get info about revision

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
            this.path = (string) dr["gitap_path"];

            var error = "";

            var commit0 = Request["rev_0"];

            if (string.IsNullOrEmpty(commit0))
            {
                var commit = (string) dr["gitcom_commit"];

                this.unified_diff_text = VersionControl.git_get_unified_diff_one_commit(repo, commit, this.path);

                // get the source code for both the left and right
                var left_text = VersionControl.git_get_file_contents(repo, commit + "^", this.path);
                var right_text = VersionControl.git_get_file_contents(repo, commit, this.path);
                this.left_title = commit + "^";
                this.right_title = commit;

                error = VersionControl.visual_diff(this.unified_diff_text, left_text, right_text, ref this.left_out,
                    ref this.right_out);
            }
            else
            {
                var commit1 = Request["rev_1"];

                this.unified_diff_text =
                    VersionControl.git_get_unified_diff_two_commits(repo, commit0, commit1, this.path);

                // get the source code for both the left and right
                var left_text = VersionControl.git_get_file_contents(repo, commit0, this.path);
                var right_text = VersionControl.git_get_file_contents(repo, commit1, this.path);
                this.left_title = commit0;
                this.right_title = commit1;

                error = VersionControl.visual_diff(this.unified_diff_text, left_text, right_text, ref this.left_out,
                    ref this.right_out);
            }

            if (error != "")
            {
                Response.Write(HttpUtility.HtmlEncode(error));
                Response.End();
            }
        }
    }
}