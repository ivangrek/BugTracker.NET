namespace BugTracker.Web.Areas.Versioning.Controllers
{
    using BugTracker.Web.Core;
    using BugTracker.Web.Core.Controls;
    using BugTracker.Web.Models;
    using System;
    using System.Web;
    using System.Web.Mvc;

    public class GitController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;

        public GitController(
            IApplicationSettings applicationSettings,
            ISecurity security)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
        }

        [HttpGet]
        public ActionResult Index(int id)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            var permissionLevel = Bug.GetBugPermissionLevel(id, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content("You are not allowed to view this item");
            }

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
                    '<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Versioning/Git/Diff") + @"?revpathid=' + convert(varchar,gitap_id) + '>diff</a>'
                    else
                    ''
                end [view<br>diff],

                case when gitap_action not like '%D%' then
                '<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Versioning/Git/Log") + @"?revpathid =' + convert(varchar,gitap_id) + '>history</a>'
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
                DataSet = DbUtil.GetDataSet(sql),
                HtmlEncode = false
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Show(int revpathid, string commit)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            var sql = @"
                select gitcom_commit, gitcom_bug, gitcom_repository, gitap_path 
                from git_commits
                inner join git_affected_paths on gitap_gitcom_id = gitcom_id
                where gitap_id = $id"
                .Replace("$id", Convert.ToString(revpathid));

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int)dr["gitcom_bug"], this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content("You are not allowed to view this item");
            }

            var repo = (string)dr["gitcom_repository"];
            var path = (string)dr["gitap_path"];

            var text = VersionControl.HgGetFileContents(repo, commit, path);

            return Content(text);
        }

        [HttpGet]
        public ActionResult Blame(int revpathid, string commit)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            var sql = @"
            select gitcom_commit, gitcom_bug, gitcom_repository, gitap_path 
            from git_commits
            inner join git_affected_paths on gitap_gitcom_id = gitcom_id
            where gitap_id = $id"
                .Replace("$id", Convert.ToString(revpathid));

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int)dr["gitcom_bug"], this.security);
            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            ViewBag.BlameText.BlameText = VersionControl.GitBlame((string)dr["gitcom_repository"], (string)dr["gitap_path"], commit);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"git blame {commit} -- {HttpUtility.HtmlEncode((string)dr["gitap_path"])}",
                SelectedItem = MainMenuSections.Administration
            };

            return View();
        }

        [HttpGet]
        public ActionResult Log(int revpathid)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

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
            var bugid = (int)dr["gitcom_bug"];
            var permissionLevel = Bug.GetBugPermissionLevel(bugid, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content("You are not allowed to view this item");
            }

            ViewBag.RevPathId = revpathid;
            ViewBag.Commit = (string)dr["gitcom_commit"];

            ViewBag.LogResult = VersionControl.GitLog((string)dr["gitcom_repository"], (string)dr["gitcom_commit"], (string)dr["gitap_path"]);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"git log {HttpUtility.HtmlEncode((string)dr["gitap_path"])}",
                SelectedItem = MainMenuSections.Administration
            };

            return View();
        }

        [HttpGet]
        public ActionResult Diff(int revpathid)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            // get info about revision
            var sql = @"
                select gitcom_commit, gitcom_bug, gitcom_repository, gitap_path 
                from git_commits
                inner join git_affected_paths on gitap_gitcom_id = gitcom_id
                where gitap_id = $id";

            sql = sql.Replace("$id", Convert.ToString(revpathid));

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int)dr["gitcom_bug"], this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content("You are not allowed to view this item");
            }

            var repo = (string)dr["gitcom_repository"];

            var error = string.Empty;
            var commit0 = Request["rev_0"];
            var leftOut = string.Empty;
            var rightOut = string.Empty;

            if (string.IsNullOrEmpty(commit0))
            {
                var commit = (string)dr["gitcom_commit"];

                ViewBag.UnifiedDiffText = VersionControl.GitGetUnifiedDiffOneCommit(repo, commit, (string)dr["gitap_path"]);

                // get the source code for both the left and right
                var leftText = VersionControl.GitGetFileContents(repo, commit + "^", (string)dr["gitap_path"]);
                var rightText = VersionControl.GitGetFileContents(repo, commit, (string)dr["gitap_path"]);

                ViewBag.LeftTitle = commit + "^";
                ViewBag.RightTitle = commit;

                error = VersionControl.VisualDiff(ViewBag.UnifiedDiffText, leftText, rightText, ref leftOut, ref rightOut);
            }
            else
            {
                var commit1 = Request["rev_1"];

                ViewBag.UnifiedDiffText = VersionControl.GitGetUnifiedDiffTwoCommits(repo, commit0, commit1, ViewBag.Path);

                // get the source code for both the left and right
                var leftText = VersionControl.GitGetFileContents(repo, commit0, (string)dr["gitap_path"]);
                var rightText = VersionControl.GitGetFileContents(repo, commit1, (string)dr["gitap_path"]);

                ViewBag.LeftTitle = commit0;
                ViewBag.RightTitle = commit1;

                error = VersionControl.VisualDiff(ViewBag.UnifiedDiffText, leftText, rightText, ref leftOut, ref rightOut);
            }

            ViewBag.LeftOut = leftOut;
            ViewBag.RightOut = rightOut;

            if (!string.IsNullOrEmpty(error))
            {
                return Content(HttpUtility.HtmlEncode(error));
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"git diff {HttpUtility.HtmlEncode((string)dr["gitap_path"])}",
                SelectedItem = MainMenuSections.Administration
            };

            return View();
        }
    }
}