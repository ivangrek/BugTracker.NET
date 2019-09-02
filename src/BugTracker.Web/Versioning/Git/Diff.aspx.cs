/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Versioning.Git
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class Diff : Page
    {
        public string LeftOut = "";
        public string LeftTitle = "";
        public string Path = "";
        public string RightOut = "";
        public string RightTitle = "";
        public Security Security;
        public string UnifiedDiffText = "";

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.AnyUserOk);

            Page.Title = "git diff " + HttpUtility.HtmlEncode(this.Path);

            // get info about revision

            var sql = @"
select gitcom_commit, gitcom_bug, gitcom_repository, gitap_path 
from git_commits
inner join git_affected_paths on gitap_gitcom_id = gitcom_id
where gitap_id = $id";

            var gitapId = Convert.ToInt32(Util.SanitizeInteger(Request["revpathid"]));
            sql = sql.Replace("$id", Convert.ToString(gitapId));

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int) dr["gitcom_bug"], this.Security);
            if (permissionLevel == Security.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var repo = (string) dr["gitcom_repository"];
            this.Path = (string) dr["gitap_path"];

            var error = "";

            var commit0 = Request["rev_0"];

            if (string.IsNullOrEmpty(commit0))
            {
                var commit = (string) dr["gitcom_commit"];

                this.UnifiedDiffText = VersionControl.GitGetUnifiedDiffOneCommit(repo, commit, this.Path);

                // get the source code for both the left and right
                var leftText = VersionControl.GitGetFileContents(repo, commit + "^", this.Path);
                var rightText = VersionControl.GitGetFileContents(repo, commit, this.Path);
                this.LeftTitle = commit + "^";
                this.RightTitle = commit;

                error = VersionControl.VisualDiff(this.UnifiedDiffText, leftText, rightText, ref this.LeftOut,
                    ref this.RightOut);
            }
            else
            {
                var commit1 = Request["rev_1"];

                this.UnifiedDiffText =
                    VersionControl.GitGetUnifiedDiffTwoCommits(repo, commit0, commit1, this.Path);

                // get the source code for both the left and right
                var leftText = VersionControl.GitGetFileContents(repo, commit0, this.Path);
                var rightText = VersionControl.GitGetFileContents(repo, commit1, this.Path);
                this.LeftTitle = commit0;
                this.RightTitle = commit1;

                error = VersionControl.VisualDiff(this.UnifiedDiffText, leftText, rightText, ref this.LeftOut,
                    ref this.RightOut);
            }

            if (error != "")
            {
                Response.Write(HttpUtility.HtmlEncode(error));
                Response.End();
            }
        }
    }
}