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

    public partial class svn_hook : Page
    {
        public void Page_Load(object sender, EventArgs e)
        {
            Util.set_context(HttpContext.Current);
            Util.do_not_cache(Response);

            var username = Request["username"];
            var password = Request["password"];

            var svn_log = Request["svn_log"];
            var repo = Request["repo"];

            if (username == null
                || username == "")
            {
                Response.AddHeader("BTNET", "ERROR: username required");
                Response.Write("ERROR: username required");
                Response.End();
            }

            if (username != Util.get_setting("SvnHookUsername", ""))
            {
                Response.AddHeader("BTNET", "ERROR: wrong username. See Web.config SvnHookUsername");
                Response.Write("ERROR: wrong username. See Web.config SvnHookUsername");
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

            var authenticated = Authenticate.check_password(username, password);

            if (!authenticated)
            {
                Response.AddHeader("BTNET", "ERROR: invalid username or password");
                Response.Write("ERROR: invalid username or password");
                Response.End();
            }

            Util.write_to_log("svn_log follows");
            Util.write_to_log(svn_log);

            Util.write_to_log("repo follows");
            Util.write_to_log(repo);

            var doc = new XmlDocument();

            doc.LoadXml(svn_log);
            var revisions = doc.GetElementsByTagName("logentry");

            for (var i = 0; i < revisions.Count; i++)
            {
                var logentry = (XmlElement) revisions[i];

                var msg = logentry.GetElementsByTagName("msg")[0].InnerText;
                var revision = logentry.GetAttribute("revision");
                var author = logentry.GetElementsByTagName("author")[0].InnerText;
                var date = logentry.GetElementsByTagName("date")[0].InnerText;

                var bugids = get_bugids_from_msg(msg);

                if (bugids == "") bugids = "0";

                foreach (var bugid in bugids.Split(','))
                    if (Util.is_int(bugid))
                        insert_revision_row_per_bug(bugid, repo, revision, author, date, msg, logentry);
            } // end for each revision

            Response.Write("OK:");
            Response.End();
        }

        public void insert_revision_row_per_bug(string bugid, string repo, string revision, string author, string date,
            string msg, XmlElement logentry)
        {
            var sql = @"
declare @cnt int
select @cnt = count(1) from svn_revisions 
where svnrev_revision = '$svnrev_revision'
and svnrev_repository = N'$svnrev_repository'
and svnrev_bug = $svnrev_bug

if @cnt = 0 
BEGIN
insert into svn_revisions
(
	svnrev_revision,
	svnrev_bug,
	svnrev_repository,
	svnrev_author,
	svnrev_svn_date,
	svnrev_btnet_date,
	svnrev_msg
)
values
(
	'$svnrev_revision',
	$svnrev_bug,
	N'$svnrev_repository',
	N'$svnrev_author',
	N'$svnrev_svn_date',
	getdate(),
	N'$svnrev_msg'
)

select scope_identity()
END	
ELSE
select 0
";

            sql = sql.Replace("$svnrev_revision", revision.Replace("'", "''"));
            sql = sql.Replace("$svnrev_bug", bugid);
            sql = sql.Replace("$svnrev_repository", repo.Replace("'", "''"));
            sql = sql.Replace("$svnrev_author", author.Replace("'", "''"));
            sql = sql.Replace("$svnrev_svn_date", date.Replace("'", "''"));
            sql = sql.Replace("$svnrev_msg", msg.Replace("'", "''"));

            var svnrev_id = Convert.ToInt32(DbUtil.execute_scalar(sql));

            if (svnrev_id > 0)
            {
                var paths = logentry.GetElementsByTagName("path");

                for (var j = 0; j < paths.Count; j++)
                {
                    var path_element = (XmlElement) paths[j];

                    var action = path_element.GetAttribute("action");
                    var file_path = path_element.InnerText;

                    sql = @"
insert into svn_affected_paths
(
svnap_svnrev_id,
svnap_action,
svnap_path
)
values
(
$svnap_svnrev_id,
N'$svnap_action',
N'$svnap_path'
)";

                    sql = sql.Replace("$svnap_svnrev_id", Convert.ToString(svnrev_id));
                    sql = sql.Replace("$svnap_action", action.Replace("'", "''"));
                    sql = sql.Replace("$svnap_path", file_path.Replace("'", "''"));

                    DbUtil.execute_nonquery(sql);
                } // end for each path
            } // if we inserted a revision
        }

        public string get_bugids_from_msg(string msg)
        {
            var without_line_breaks = msg.Replace("\r\n", "").Replace("\n", "");

            var regex_pattern1 = Util.get_setting("SvnBugidRegexPattern1", "([0-9,]+$)"); // at end

            var reIntegerAtEnd = new Regex(regex_pattern1);
            var m = reIntegerAtEnd.Match(without_line_breaks);

            if (m.Success) return m.Groups[1].ToString();

            var regex_pattern2 = Util.get_setting("SvnBugidRegexPattern2", "(^[0-9,]+ )"); // comma delimited at start
            var reIntegerAtStart = new Regex(regex_pattern2);
            var m2 = reIntegerAtStart.Match(without_line_breaks);

            if (m2.Success)
            {
                var bugids = m2.Groups[1].ToString().Trim();
                Util.write_to_log("bugids string: " + bugids);
                return bugids;
            }

            return "";
        }
    }
}