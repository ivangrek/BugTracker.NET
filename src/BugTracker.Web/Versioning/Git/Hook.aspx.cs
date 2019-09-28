/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Versioning.Git
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.UI;
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

            var gitLog = Request["GitLog"];
            var repo = Request["repo"];

            if (username == null
                || username == "")
            {
                Response.AddHeader("BTNET", "ERROR: username required");
                Response.Write("ERROR: username required");
                Response.End();
            }

            if (username != ApplicationSettings.GitHookUsername)
            {
                Response.AddHeader("BTNET", "ERROR: wrong username. See Web.config GitHookUsername");
                Response.Write("ERROR: wrong username. See Web.config GitHookUsernam");
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

            Util.WriteToLog("GitLog follows");
            Util.WriteToLog(gitLog);

            Util.WriteToLog("repo follows");
            Util.WriteToLog(repo);

            var regex = new Regex("\n");
            var lines = regex.Split(gitLog);

            var bug = 0;
            string commit = null;
            string author = null;
            string date = null;
            var msg = "";

            var actions = new List<string>();
            var paths = new List<string>();

            var regexPattern = ApplicationSettings.GitBugidRegexPattern;
            var reInteger = new Regex(regexPattern);

            for (var i = 0; i < lines.Length; i++)
                if (lines[i].StartsWith("commit "))
                {
                    if (commit != null)
                    {
                        update_db(bug, repo, commit, author, date, msg, actions, paths);
                        msg = "";
                        bug = 0;
                        actions.Clear();
                        paths.Clear();
                    }

                    commit = lines[i].Substring(7);
                }
                else if (lines[i].StartsWith("Author: "))
                {
                    author = lines[i].Substring(8);
                }
                else if (lines[i].StartsWith("Date:"))
                {
                    date = lines[i].Substring(5).Trim();
                }
                else if (lines[i].StartsWith("    "))
                {
                    if (msg != "")
                    {
                        msg += Environment.NewLine;
                    }
                    else
                    {
                        var m = reInteger.Match(lines[i].Substring(4));
                        if (m.Success) bug = Convert.ToInt32(m.Groups[1].ToString());
                    }

                    msg += lines[i].Substring(4);
                }
                else if (lines[i].Length > 1 && lines[i][1] == '\t')
                {
                    actions.Add(lines[i].Substring(0, 1));
                    paths.Add(lines[i].Substring(2));
                }

            if (commit != null) update_db(bug, repo, commit, author, date, msg, actions, paths);

            Response.Write("OK:");

            Response.End();
        }

        public void update_db(int bug, string repo, string commit, string author, string date, string msg,
            List<string> actions, List<string> paths)
        {
            Util.WriteToLog(commit);
            Util.WriteToLog(author);
            Util.WriteToLog(date);
            Util.WriteToLog(msg);

            /*

        Because the python script sends us not just the most recent commit, but the most recent N commits, we need
        to have logic here not to do dupe inserts.

        */

            var sql = @"

declare @cnt int
select @cnt = count(1) from git_commits 
where gitcom_commit = '$gitcom_commit'
and gitcom_repository = N'$gitcom_repository'

if @cnt = 0 
BEGIN
	insert into git_commits
	(
		gitcom_commit,
		gitcom_bug,
		gitcom_repository,
		gitcom_author,
		gitcom_git_date,
		gitcom_btnet_date,
		gitcom_msg
	)
	values
	(
		'$gitcom_commit',
		$gitcom_bug,
		N'$gitcom_repository',
		N'$gitcom_author',
		N'$gitcom_git_date',
		getdate(),
		N'$gitcom_msg'
	)

	select scope_identity()
END	
ELSE
	select 0

";

            sql = sql.Replace("$gitcom_commit", commit.Replace("'", "''"));
            sql = sql.Replace("$gitcom_bug", Convert.ToString(bug));
            sql = sql.Replace("$gitcom_repository", repo.Replace("'", "''"));
            sql = sql.Replace("$gitcom_author", author.Replace("'", "''"));
            sql = sql.Replace("$gitcom_git_date", date.Replace("'", "''"));
            sql = sql.Replace("$gitcom_msg", msg.Replace("'", "''"));

            var gitcomId = Convert.ToInt32(DbUtil.ExecuteScalar(sql));

            if (gitcomId != 0)
            {
                var gitcomIdString = Convert.ToString(gitcomId);

                Util.WriteToLog(Convert.ToString(gitcomId));

                for (var i = 0; i < actions.Count; i++)
                {
                    sql = @"
insert into git_affected_paths
(
gitap_gitcom_id,
gitap_action,
gitap_path
)
values
(
$gitap_gitcom_id,
N'$gitap_action',
N'$gitap_path'
)
	";

                    sql = sql.Replace("$gitap_gitcom_id", gitcomIdString);
                    sql = sql.Replace("$gitap_action", actions[i]);
                    sql = sql.Replace("$gitap_path", paths[i].Replace("'", "''"));

                    DbUtil.ExecuteNonQuery(sql);
                }
            }
        }
    }
}