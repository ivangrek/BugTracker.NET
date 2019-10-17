/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Versioning.Svn
{
    using System;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class Diff : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public string LeftOut = string.Empty;
        public string LeftTitle = string.Empty;
        public string Path0 = string.Empty;
        public string Path1 = string.Empty;

        public string Repo;
        public string RightOut = string.Empty;

        public string RightTitle = string.Empty;
        /*
The following is an explanation of Unified Diff Format from this web page, from Guido van Rossum, the Python guy:
http://www.artima.com/weblogs/viewpost.jsp?thread=164293

The header lines look like this:

indicator ' ' filename '\t' date ' ' time ' ' timezone

where:

		* indicator is '---' for the old file and '+++' for the new
		* date has the form YYYY-MM-DD
		* time has the form hh:mm:ss.nnnnnnnnn on a 24-hour clock
		* timezone is has the form ('+'|'-') hhmm where hhmm is hours and minutes east
		(if the sign is +) or west (if the sign is -) of GMT/UTC

Each chunk starts with a line that looks like this:

'@@ -' range ' +' range ' @@'

where range is either one unsigned decimal number or two separated by a comma.
The first number is the start line of the chunk in the old or new file.
The second number is chunk size in that file;
it and the comma are omitted if the chunk size is 1.
If the chunk size is 0, the first number is one lower than one would expect
(it is the line number after which the chunk should be inserted or deleted;
in all other cases it gives the first line number or the replaced range of lines).

A chunk then continues with lines starting with
' ' (common line),
'-' (only in old file), or
'+' (only in new file).

If the last line of a file doesn't end in a newline character,
it is displayed with a newline character, and the following line in the chunk has
the literal text (starting in the first column):

'\ No newline at end of file'

*/
        public string UnifiedDiffText = string.Empty;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            Page.Title = "svn diff " + HttpUtility.HtmlEncode(this.Path1);

            var sql = @"
select svnrev_revision, svnrev_repository, svnap_path, svnrev_bug
from svn_revisions
inner join svn_affected_paths on svnap_svnrev_id = svnrev_id
where svnap_id = $id";

            var svnapId = Convert.ToInt32(Util.SanitizeInteger(Request["revpathid"]));
            sql = sql.Replace("$id", Convert.ToString(svnapId));

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int) dr["svnrev_bug"], Security);
            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            this.Repo = (string) dr["svnrev_repository"];

            if (ApplicationSettings.SvnTrustPathsInUrls)
            {
                this.Path0 = Request["path_0"];
                this.Path1 = Request["path_1"];
            }
            else
            {
                this.Path0 = this.Path1 = (string) dr["svnap_path"];
            }

            var error = string.Empty;

            var stringRevision0 = Request["rev_0"];

            if (string.IsNullOrEmpty(stringRevision0))
            {
                var revision = (int) dr["svnrev_revision"];

                this.UnifiedDiffText = VersionControl.SvnDiff(this.Repo, this.Path1, revision, 0);
                examine_diff(this.UnifiedDiffText);

                // get the old revision number
                var regex = new Regex("\n");
                var diffLines = regex.Split(this.UnifiedDiffText.Replace("\r\n", "\n"));

                var line = diffLines[2];
                var oldRevPos1 = line.ToLower().IndexOf("(revision "); // 10 chars long
                var oldRevPosStartOfInt = oldRevPos1 + 10;
                var oldRevAfterInt = line.IndexOf(")", oldRevPosStartOfInt);
                var oldRevisionString = line.Substring(oldRevPosStartOfInt,
                    oldRevAfterInt - oldRevPosStartOfInt);

                var oldRevision = Convert.ToInt32(oldRevisionString);

                // get the source code for both the left and right
                var leftText = VersionControl.SvnCat(this.Repo, this.Path0, oldRevision);
                var rightText = VersionControl.SvnCat(this.Repo, this.Path1, revision);
                this.LeftTitle = Convert.ToString(oldRevision);
                this.RightTitle = Convert.ToString(revision);

                error = VersionControl.VisualDiff(this.UnifiedDiffText, leftText, rightText, ref this.LeftOut,
                    ref this.RightOut);
            }
            else
            {
                var revision1 = Convert.ToInt32(Request["rev_1"]);
                var revision0 = Convert.ToInt32(stringRevision0);

                this.UnifiedDiffText = VersionControl.SvnDiff(this.Repo, this.Path1, revision1, revision0);
                examine_diff(this.UnifiedDiffText);

                // get the source code for both the left and right
                var leftText = VersionControl.SvnCat(this.Repo, this.Path0, revision0);
                var rightText = VersionControl.SvnCat(this.Repo, this.Path1, revision1);
                this.LeftTitle = Convert.ToString(revision0);
                this.RightTitle = Convert.ToString(revision1);

                error = VersionControl.VisualDiff(this.UnifiedDiffText, leftText, rightText, ref this.LeftOut,
                    ref this.RightOut);
            }

            if (!string.IsNullOrEmpty(error))
            {
                Response.Write(HttpUtility.HtmlEncode(error));
                Response.End();
            }
        }

        public void examine_diff(string unifiedDiffText)
        {
            if (string.IsNullOrEmpty(unifiedDiffText))
            {
                Response.Write("No differences.");
                Response.End();
            }

            var errorPos = unifiedDiffText.IndexOf("Cannot display: file marked as a binary type.");
            if (errorPos > -1)
            {
                Response.Write("<div style='color:red; font-weight: bold; font-size: 10pt;'>");
                Response.Write(unifiedDiffText.Substring(errorPos));
                Response.Write("<br>Subversion thinks this is a binary file.</div>");
                Response.End();
            }
        }
    }
}