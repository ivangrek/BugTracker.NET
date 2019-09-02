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

    public partial class View : Page
    {
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);
            Response.ContentType = "text/plain";

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.AnyUserOk);

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
            var path = (string) dr["gitap_path"];
            var commit = Request["commit"];

            var text = VersionControl.HgGetFileContents(repo, commit, path);

            Response.Write(text);
        }
    }
}