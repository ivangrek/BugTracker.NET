namespace BugTracker.Web.Areas.Versioning.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;
    using Core;
    using Core.Controls;
    using Core.Identification;
    using Models;

    [Authorize]
    [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
    public class GitController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly IAuthenticate authenticate;
        private readonly ISecurity security;

        public GitController(
            IApplicationSettings applicationSettings,
            ISecurity security,
            IAuthenticate authenticate)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
            this.authenticate = authenticate;
        }

        [HttpGet]
        public ActionResult Index(int id)
        {
            var permissionLevel = Bug.GetBugPermissionLevel(id, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
                return Content("You are not allowed to view this item");

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - view git file commits",
                SelectedItem = MainMenuSections.Administration
            };

            var sql = @"
                select 
                gitcom_commit [commit],
                gitcom_repository [repo],
                gitap_action [action],
                gitap_path [file],
                replace(replace(gitcom_author,'<','&lt;'),'>','&gt;') [user],
                substring(gitcom_git_date,1,19) [date],
                replace(substring(gitcom_msg,1,4000),char(13),'<br>') [msg],

                case when gitap_action not like '%D%' and gitap_action not like 'A%' then
                    '<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Versioning/Git/Diff") +
                      @"?revpathid=' + convert(varchar,gitap_id) + '>diff</a>'
                    else
                    ''
                end [view<br>diff],

                case when gitap_action not like '%D%' then
                '<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Versioning/Git/Log") +
                      @"?revpathid =' + convert(varchar,gitap_id) + '>history</a>'
                    else
                    ''
                end [view<br>history<br>(git log)]

                from git_commits
                inner join git_affected_paths on gitap_gitcom_id = gitcom_id
                where gitcom_bug = $bg
                order by gitcom_git_date desc, gitap_path"
                          .Replace("$bg", Convert.ToString(id));

            var model = new SortableTableModel
            {
                DataTable = DbUtil.GetDataSet(sql)
                    .Tables[0],
                HtmlEncode = false
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Show(int revpathid, string commit)
        {
            var sql = @"
                select gitcom_commit, gitcom_bug, gitcom_repository, gitap_path 
                from git_commits
                inner join git_affected_paths on gitap_gitcom_id = gitcom_id
                where gitap_id = $id"
                .Replace("$id", Convert.ToString(revpathid));

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int) dr["gitcom_bug"], this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
                return Content("You are not allowed to view this item");

            var repo = (string) dr["gitcom_repository"];
            var path = (string) dr["gitap_path"];

            var text = VersionControl.HgGetFileContents(repo, commit, path);

            return Content(text);
        }

        [HttpGet]
        public ActionResult Blame(int revpathid, string commit)
        {
            var sql = @"
            select gitcom_commit, gitcom_bug, gitcom_repository, gitap_path 
            from git_commits
            inner join git_affected_paths on gitap_gitcom_id = gitcom_id
            where gitap_id = $id"
                .Replace("$id", Convert.ToString(revpathid));

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int) dr["gitcom_bug"], this.security);
            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            ViewBag.BlameText.BlameText =
                VersionControl.GitBlame((string) dr["gitcom_repository"], (string) dr["gitap_path"], commit);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"git blame {commit} -- {HttpUtility.HtmlEncode((string) dr["gitap_path"])}",
                SelectedItem = MainMenuSections.Administration
            };

            return View();
        }

        [HttpGet]
        public ActionResult Log(int revpathid)
        {
            // get info about commit

            var sql = @"
                select gitcom_repository, gitcom_commit, gitap_path, gitcom_bug
                from git_commits
                inner join git_affected_paths on gitap_gitcom_id = gitcom_id
                where gitap_id = $id
                order by gitcom_commit desc, gitap_path"
                .Replace("$id", Convert.ToString(revpathid));

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var bugid = (int) dr["gitcom_bug"];
            var permissionLevel = Bug.GetBugPermissionLevel(bugid, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
                return Content("You are not allowed to view this item");

            ViewBag.RevPathId = revpathid;
            ViewBag.Commit = (string) dr["gitcom_commit"];

            ViewBag.LogResult = VersionControl.GitLog((string) dr["gitcom_repository"], (string) dr["gitcom_commit"],
                (string) dr["gitap_path"]);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"git log {HttpUtility.HtmlEncode((string) dr["gitap_path"])}",
                SelectedItem = MainMenuSections.Administration
            };

            return View();
        }

        [HttpGet]
        public ActionResult Diff(int revpathid)
        {
            // get info about revision
            var sql = @"
                select gitcom_commit, gitcom_bug, gitcom_repository, gitap_path 
                from git_commits
                inner join git_affected_paths on gitap_gitcom_id = gitcom_id
                where gitap_id = $id";

            sql = sql.Replace("$id", Convert.ToString(revpathid));

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int) dr["gitcom_bug"], this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
                return Content("You are not allowed to view this item");

            var repo = (string) dr["gitcom_repository"];

            var error = string.Empty;
            var commit0 = Request["rev_0"];
            var leftOut = string.Empty;
            var rightOut = string.Empty;

            if (string.IsNullOrEmpty(commit0))
            {
                var commit = (string) dr["gitcom_commit"];

                ViewBag.UnifiedDiffText =
                    VersionControl.GitGetUnifiedDiffOneCommit(repo, commit, (string) dr["gitap_path"]);

                // get the source code for both the left and right
                var leftText = VersionControl.GitGetFileContents(repo, commit + "^", (string) dr["gitap_path"]);
                var rightText = VersionControl.GitGetFileContents(repo, commit, (string) dr["gitap_path"]);

                ViewBag.LeftTitle = commit + "^";
                ViewBag.RightTitle = commit;

                error = VersionControl.VisualDiff(ViewBag.UnifiedDiffText, leftText, rightText, ref leftOut,
                    ref rightOut);
            }
            else
            {
                var commit1 = Request["rev_1"];

                ViewBag.UnifiedDiffText =
                    VersionControl.GitGetUnifiedDiffTwoCommits(repo, commit0, commit1, ViewBag.Path);

                // get the source code for both the left and right
                var leftText = VersionControl.GitGetFileContents(repo, commit0, (string) dr["gitap_path"]);
                var rightText = VersionControl.GitGetFileContents(repo, commit1, (string) dr["gitap_path"]);

                ViewBag.LeftTitle = commit0;
                ViewBag.RightTitle = commit1;

                error = VersionControl.VisualDiff(ViewBag.UnifiedDiffText, leftText, rightText, ref leftOut,
                    ref rightOut);
            }

            ViewBag.LeftOut = leftOut;
            ViewBag.RightOut = rightOut;

            if (!string.IsNullOrEmpty(error)) return Content(HttpUtility.HtmlEncode(error));

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"git diff {HttpUtility.HtmlEncode((string) dr["gitap_path"])}",
                SelectedItem = MainMenuSections.Administration
            };

            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Hook()
        {
            var username = Request["username"];
            var password = Request["password"];
            var gitLog = Request["GitLog"];
            var repo = Request["repo"];

            if (string.IsNullOrEmpty(username))
            {
                Response.AddHeader("BTNET", "ERROR: username required");

                return Content("ERROR: username required");
            }

            if (username != this.applicationSettings.GitHookUsername)
            {
                Response.AddHeader("BTNET", "ERROR: wrong username. See Web.config GitHookUsername");

                return Content("ERROR: wrong username. See Web.config GitHookUsernam");
            }

            if (string.IsNullOrEmpty(password))
            {
                Response.AddHeader("BTNET", "ERROR: password required");

                return Content("ERROR: password required");
            }

            // authenticate user
            var authenticated = this.authenticate.CheckPassword(username, password);

            if (!authenticated)
            {
                Response.AddHeader("BTNET", "ERROR: invalid username or password");

                return Content("ERROR: invalid username or password");
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
            var msg = string.Empty;

            var actions = new List<string>();
            var paths = new List<string>();

            var regexPattern = this.applicationSettings.GitBugidRegexPattern;
            var reInteger = new Regex(regexPattern);

            for (var i = 0; i < lines.Length; i++)
                if (lines[i].StartsWith("commit "))
                {
                    if (commit != null)
                    {
                        UpdateDb(bug, repo, commit, author, date, msg, actions, paths);
                        msg = string.Empty;
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
                    if (!string.IsNullOrEmpty(msg))
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

            if (commit != null) UpdateDb(bug, repo, commit, author, date, msg, actions, paths);

            return Content("OK:");
        }

        private static void UpdateDb(int bug, string repo, string commit, string author, string date, string msg,
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