/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Versioning.Svn
{
    using System;
    using System.Web.UI;
    using Core;

    public partial class View : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public string Repo;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);
            Response.ContentType = "text/plain";

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            // get info about revision

            var sql = @"
select svnrev_revision, svnrev_repository, svnap_path, svnrev_bug
from svn_revisions
inner join svn_affected_paths on svnap_svnrev_id = svnrev_id
where svnap_id = $id
order by svnrev_revision desc, svnap_path";

            var svnapId = Convert.ToInt32(Util.SanitizeInteger(Request["revpathid"]));
            var stringAffectedPathId = Convert.ToString(svnapId);

            sql = sql.Replace("$id", stringAffectedPathId);

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int)dr["svnrev_bug"], Security);
            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var revision = Convert.ToInt32(Request["rev"]);

            this.Repo = (string)dr["svnrev_repository"];
            string path;
            if (ApplicationSettings.SvnTrustPathsInUrls)
                path = Request["path"];
            else
                path = (string)dr["svnap_path"];

            var rawText = VersionControl.SvnCat(this.Repo, path, revision);

            Response.Write(rawText);
        }
    }
}