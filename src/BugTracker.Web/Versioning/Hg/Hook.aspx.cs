/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Versioning.Hg
{
    using System;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.UI;
    using System.Xml;
    using Core;

    public partial class Hook : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public IAuthenticate Authenticate { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.SetContext(HttpContext.Current);
            Util.DoNotCache(Response);

            var username = Request["username"];
            var password = Request["password"];

            var hgLog = Request["hg_log"];
            var repo = Request["repo"];

            if (username == null
                || username == "")
            {
                Response.AddHeader("BTNET", "ERROR: username required");
                Response.Write("ERROR: username required");
                Response.End();
            }

            if (username != ApplicationSettings.MercurialHookUsername)
            {
                Response.AddHeader("BTNET", "ERROR: wrong username. See Web.config MercurialHookUsername");
                Response.Write("ERROR: wrong username. See Web.config MercurialHookUsernam");
                Response.End();
            }

            if (password == null
                || password == "")
            {
                Response.AddHeader("BTNET", "ERROR: password required");
                Response.Write("ERROR: password required");
                Response.End();
            }

            // authenticate user

            var authenticated = Authenticate.CheckPassword(username, password);

            if (!authenticated)
            {
                Response.AddHeader("BTNET", "ERROR: invalid username or password");
                Response.Write("ERROR: invalid username or password");
                Response.End();
            }

            Util.WriteToLog("hg_log follows");
            Util.WriteToLog(hgLog);

            Util.WriteToLog("repo follows");
            Util.WriteToLog(repo);

            var doc = new XmlDocument();

            doc.LoadXml("<log>" + hgLog + "</log>");
            var revisions = doc.GetElementsByTagName("changeset");

            for (var i = 0; i < revisions.Count; i++)
            {
                var changeset = (XmlElement) revisions[i];

                var desc = changeset.GetElementsByTagName("desc")[0].InnerText;
                var bug = get_bugid_from_desc(desc);

                if (bug == "") bug = "0";

                var revision = changeset.GetAttribute("rev");
                var author = changeset.GetElementsByTagName("auth")[0].InnerText;
                var date = changeset.GetElementsByTagName("date")[0].InnerText;

                var sql = @"
declare @cnt int
select @cnt = count(1) from hg_revisions 
where hgrev_revision = '$hgrev_revision'
and hgrev_repository = N'$hgrev_repository'

if @cnt = 0 
BEGIN
insert into hg_revisions
(
	hgrev_revision,
	hgrev_bug,
	hgrev_repository,
	hgrev_author,
	hgrev_hg_date,
	hgrev_btnet_date,
	hgrev_msg
)
values
(
	$hgrev_revision,
	$hgrev_bug,
	N'$hgrev_repository',
	N'$hgrev_author',
	N'$hgrev_hg_date',
	getdate(),
	N'$hgrev_desc'
)

select scope_identity()
END	
ELSE
select 0
";

                sql = sql.Replace("$hgrev_revision", revision.Replace("'", "''"));
                sql = sql.Replace("$hgrev_bug", Convert.ToString(bug));
                sql = sql.Replace("$hgrev_repository", repo.Replace("'", "''"));
                sql = sql.Replace("$hgrev_author", author.Replace("'", "''"));
                sql = sql.Replace("$hgrev_hg_date", date.Replace("'", "''"));
                sql = sql.Replace("$hgrev_desc", desc.Replace("'", "''"));

                var hgrevId = Convert.ToInt32(DbUtil.ExecuteScalar(sql));

                if (hgrevId > 0)
                {
                    var paths = changeset.GetElementsByTagName("file");

                    for (var j = 0; j < paths.Count; j++)
                    {
                        var pathElement = (XmlElement) paths[j];

                        var action = ""; // no action in hg?  path_element.GetAttribute("action");
                        var filePath = pathElement.InnerText;

                        sql = @"
insert into hg_affected_paths
(
hgap_hgrev_id,
hgap_action,
hgap_path
)
values
(
$hgap_hgrev_id,
N'$hgap_action',
N'$hgap_path'
)";

                        sql = sql.Replace("$hgap_hgrev_id", Convert.ToString(hgrevId));
                        sql = sql.Replace("$hgap_action", action.Replace("'", "''"));
                        sql = sql.Replace("$hgap_path", filePath.Replace("'", "''"));

                        DbUtil.ExecuteNonQuery(sql);
                    } // end for each path
                } // if we inserted a revision
            } // end for each revision

            Response.Write("OK:");
            Response.End();
        }

        public string get_bugid_from_desc(string desc)
        {
            var regexPattern = ApplicationSettings.MercurialBugidRegexPattern;
            var reInteger = new Regex(regexPattern);
            var m = reInteger.Match(desc);
            if (m.Success)
                return m.Groups[1].ToString();
            return "";
        }
    }
}