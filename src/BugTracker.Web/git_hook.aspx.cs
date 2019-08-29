/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class git_hook : Page
    {
        public void Page_Load(object sender, EventArgs e)
        {
            Util.set_context(HttpContext.Current);
            Util.do_not_cache(Response);

            var username = Request["username"];
            var password = Request["password"];

            var git_log = Request["git_log"];
            var repo = Request["repo"];

            if (username == null
                || username == "")
            {
                Response.AddHeader("BTNET", "ERROR: username required");
                Response.Write("ERROR: username required");
                Response.End();
            }

            if (username != Util.get_setting("GitHookUsername", ""))
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

            var authenticated = Authenticate.check_password(username, password);

            if (!authenticated)
            {
                Response.AddHeader("BTNET", "ERROR: invalid username or password");
                Response.Write("ERROR: invalid username or password");
                Response.End();
            }

            Util.write_to_log("git_log follows");
            Util.write_to_log(git_log);

            Util.write_to_log("repo follows");
            Util.write_to_log(repo);

            var regex = new Regex("\n");
            var lines = regex.Split(git_log);

            var bug = 0;
            string commit = null;
            string author = null;
            string date = null;
            var msg = "";

            var actions = new List<string>();
            var paths = new List<string>();

            var regex_pattern = Util.get_setting("GitBugidRegexPattern", "(^[0-9]+)");
            var reInteger = new Regex(regex_pattern);

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
            Util.write_to_log(commit);
            Util.write_to_log(author);
            Util.write_to_log(date);
            Util.write_to_log(msg);

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

            var gitcom_id = Convert.ToInt32(DbUtil.execute_scalar(sql));

            if (gitcom_id != 0)
            {
                var gitcom_id_string = Convert.ToString(gitcom_id);

                Util.write_to_log(Convert.ToString(gitcom_id));

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

                    sql = sql.Replace("$gitap_gitcom_id", gitcom_id_string);
                    sql = sql.Replace("$gitap_action", actions[i]);
                    sql = sql.Replace("$gitap_path", paths[i].Replace("'", "''"));

                    DbUtil.execute_nonquery(sql);
                }
            }
        }
    }
}