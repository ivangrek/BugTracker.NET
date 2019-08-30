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
    using System.Xml;
    using Core;

    public partial class HgDiff : Page
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

            Page.Title = "hg diff " + HttpUtility.HtmlEncode(this.Path);

            // get info about revision

            var sql = @"
select hgrev_revision, hgrev_bug, hgrev_repository, hgap_path 
from hg_revisions
inner join hg_affected_paths on hgap_hgrev_id = hgrev_id
where hgap_id = $id";

            var hgapId = Convert.ToInt32(Util.SanitizeInteger(Request["revpathid"]));
            sql = sql.Replace("$id", Convert.ToString(hgapId));

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int)dr["hgrev_bug"], this.Security);
            if (permissionLevel == Security.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var repo = (string)dr["hgrev_repository"];
            this.Path = (string)dr["hgap_path"];

            var error = "";

            var revision0 = Request["rev_0"];

            if (string.IsNullOrEmpty(revision0))
            {
                var revision = Convert.ToString((int)dr["hgrev_revision"]);

                // we need to find the previous revision
                var log = VersionControl.HgLog(repo, revision, this.Path);
                var prevRevision = get_previous_revision(log, revision);

                if (prevRevision == "")
                {
                    Response.Write("unable to determine previous revision from log");
                    Response.End();
                }

                this.UnifiedDiffText =
                    VersionControl.HgGetUnifiedDiffTwoRevisions(repo, prevRevision, revision, this.Path);

                // get the source code for both the left and right
                var leftText = VersionControl.HgGetFileContents(repo, prevRevision, this.Path);

                var rightText = VersionControl.HgGetFileContents(repo, revision, this.Path);
                this.LeftTitle = prevRevision;
                this.RightTitle = revision;

                error = VersionControl.VisualDiff(this.UnifiedDiffText, leftText, rightText, ref this.LeftOut,
                    ref this.RightOut);
            }
            else
            {
                var revision1 = Request["rev_1"];

                this.UnifiedDiffText =
                    VersionControl.HgGetUnifiedDiffTwoRevisions(repo, revision0, revision1, this.Path);

                // get the source code for both the left and right
                var leftText = VersionControl.HgGetFileContents(repo, revision0, this.Path);
                var rightText = VersionControl.HgGetFileContents(repo, revision1, this.Path);
                this.LeftTitle = revision0;
                this.RightTitle = revision1;

                error = VersionControl.VisualDiff(this.UnifiedDiffText, leftText, rightText, ref this.LeftOut,
                    ref this.RightOut);
            }

            if (error != "")
            {
                Response.Write(HttpUtility.HtmlEncode(error));
                Response.End();
            }
        }

        public string get_previous_revision(string logResult, string thisRevision)
        {
            var doc = new XmlDocument();
            doc.LoadXml("<log>" + logResult + "</log>");
            var revisions = doc.GetElementsByTagName("changeset");

            // read backwards
            if (revisions.Count > 1)
            {
                var changeset = (XmlElement)revisions[revisions.Count - 2];

                return changeset.GetAttribute("rev");
            }

            return "";
        }
    }
}