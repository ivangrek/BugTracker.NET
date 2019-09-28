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
    using System.Xml;
    using Core;

    public partial class Blame : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }

        public string BlameText;
        public string Path;
        public string RawText;
        public string Repo;
        public int Revision;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.AnyUserOk);

            Page.Title = "svn blame " + HttpUtility.HtmlEncode(this.Path) + "@" + Convert.ToString(this.Revision);

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
            var permissionLevel = Bug.GetBugPermissionLevel((int) dr["svnrev_bug"], security);
            if (permissionLevel == Security.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            this.Revision = Convert.ToInt32(Request["rev"]);

            this.Repo = (string) dr["svnrev_repository"];

            if (ApplicationSettings.SvnTrustPathsInUrls)
                this.Path = Request["path"];
            else
                this.Path = (string) dr["svnap_path"];

            this.RawText = VersionControl.SvnCat(this.Repo, this.Path, this.Revision);

            if (this.RawText.StartsWith("ERROR:"))
            {
                Response.Write(HttpUtility.HtmlEncode(this.RawText));
                Response.End();
            }

            this.BlameText = VersionControl.SvnBlame(this.Repo, this.Path, this.Revision);

            if (this.BlameText.StartsWith("ERROR:"))
            {
                Response.Write(HttpUtility.HtmlEncode(this.BlameText));
                Response.End();
            }
        }

        public void write_blame()
        {
            var doc = new XmlDocument();
            doc.LoadXml(this.BlameText);
            var commits = doc.GetElementsByTagName("commit");

            // split the source text into lines
            var regex = new Regex("\n");
            var lines = regex.Split(this.RawText.Replace("\r\n", "\n"));

            for (var i = 0; i < commits.Count; i++)
            {
                var commit = (XmlElement) commits[i];
                Response.Write("<tr><td nowrap>" + commit.GetAttribute("revision"));

                var author = "";
                var date = "";

                foreach (XmlNode node in commit.ChildNodes)
                    if (node.Name == "author") author = node.InnerText;
                    else if (node.Name == "date")
                        date = Util.FormatDbDateTime(XmlConvert.ToDateTime(node.InnerText,
                            XmlDateTimeSerializationMode.Local));

                Response.Write("<td nowrap>" + author);
                Response.Write("<td nowrap style='background: #ddffdd'><pre style='display:inline;'> " +
                               HttpUtility.HtmlEncode(lines[i]));
                Response.Write(" </pre><td nowrap>" + date);
            }
        }
    }
}