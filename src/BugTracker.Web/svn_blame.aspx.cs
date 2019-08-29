/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.UI;
    using System.Xml;
    using Core;

    public partial class svn_blame : Page
    {
        public string blame_text;
        public string path;
        public string raw_text;
        public string repo;
        public int revision;
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Page.Title = "svn blame " + HttpUtility.HtmlEncode(this.path) + "@" + Convert.ToString(this.revision);

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

            this.revision = Convert.ToInt32(Request["rev"]);

            this.repo = (string) dr["svnrev_repository"];

            if (Util.get_setting("SvnTrustPathsInUrls", "0") == "1")
                this.path = Request["path"];
            else
                this.path = (string) dr["svnap_path"];

            this.raw_text = VersionControl.svn_cat(this.repo, this.path, this.revision);

            if (this.raw_text.StartsWith("ERROR:"))
            {
                Response.Write(HttpUtility.HtmlEncode(this.raw_text));
                Response.End();
            }

            this.blame_text = VersionControl.svn_blame(this.repo, this.path, this.revision);

            if (this.blame_text.StartsWith("ERROR:"))
            {
                Response.Write(HttpUtility.HtmlEncode(this.blame_text));
                Response.End();
            }
        }

        public void write_blame()
        {
            var doc = new XmlDocument();
            doc.LoadXml(this.blame_text);
            var commits = doc.GetElementsByTagName("commit");

            // split the source text into lines
            var regex = new Regex("\n");
            var lines = regex.Split(this.raw_text.Replace("\r\n", "\n"));

            for (var i = 0; i < commits.Count; i++)
            {
                var commit = (XmlElement) commits[i];
                Response.Write("<tr><td nowrap>" + commit.GetAttribute("revision"));

                var author = "";
                var date = "";

                foreach (XmlNode node in commit.ChildNodes)
                    if (node.Name == "author") author = node.InnerText;
                    else if (node.Name == "date")
                        date = Util.format_db_date_and_time(XmlConvert.ToDateTime(node.InnerText,
                            XmlDateTimeSerializationMode.Local));

                Response.Write("<td nowrap>" + author);
                Response.Write("<td nowrap style='background: #ddffdd'><pre style='display:inline;'> " +
                               HttpUtility.HtmlEncode(lines[i]));
                Response.Write(" </pre><td nowrap>" + date);
            }
        }
    }
}