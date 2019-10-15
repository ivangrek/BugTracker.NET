namespace BugTracker.Web.Areas.Versioning.Controllers
{
    using BugTracker.Web.Core;
    using BugTracker.Web.Core.Controls;
    using BugTracker.Web.Models;
    using System;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;
    using System.Xml;

    [OutputCache(Location = OutputCacheLocation.None)]
    public class SvnController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;
        private readonly IAuthenticate authenticate;

        public SvnController(
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
                Title = $"{this.applicationSettings.AppTitle} -  view svn file revisions",
                SelectedItem = MainMenuSections.Administration
            };

            var sql = @"
                select
                svnrev_revision [revision],
                svnrev_repository [repository],
                svnap_action [action],
                svnap_path [file],
                svnrev_author [user],
                svnrev_svn_date [revision date],
                replace(substring(svnrev_msg,1,4000),char(13),'<br>') [msg],

                case when svnap_action not like '%D%' and svnap_action not like 'A%' then
                    '<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Versioning/Svn/Diff.aspx") + @"?revpathid=' + convert(varchar,svnap_id) + '>diff</a>'
                    else
                    ''
                end [view<br>diff],

                case when svnap_action not like '%D%' then
                '<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Versioning/Svn/Log.aspx") + @"?revpathid=' + convert(varchar,svnap_id) + '>history</a>'
                    else
                    ''
                end [view<br>history<br>(svn log)]";

            //	if (websvn_url != "")
            //	{
            //		sql += ",\n '<a target=_blank href=\"" + websvn_url + "\">WebSvn</a>' [WebSvn<br>URL]";
            //		sql = sql.Replace("$PATH","' + svnap_path + '");
            //		sql = sql.Replace("$REV", "' + convert(varchar,svnrev_revision) + '");
            //	}

            sql += @"
            from svn_revisions
            inner join svn_affected_paths on svnap_svnrev_id = svnrev_id
            where svnrev_bug = $bg
            order by svnrev_revision desc, svnap_path"
                .Replace("$bg", Convert.ToString(id));

            var model = new SortableTableModel
            {
                DataSet = DbUtil.GetDataSet(sql),
                HtmlEncode = false
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Show(int revpathid, string revision, string path)
        {
            Response.ContentType = "text/plain";

            this.security.CheckSecurity(SecurityLevel.AnyUserOk);

            // get info about revision

            var sql = @"
                select svnrev_revision, svnrev_repository, svnap_path, svnrev_bug
                from svn_revisions
                inner join svn_affected_paths on svnap_svnrev_id = svnrev_id
                where svnap_id = $id
                order by svnrev_revision desc, svnap_path";

            var stringAffectedPathId = Convert.ToString(Convert.ToInt32(revpathid));

            sql = sql.Replace("$id", stringAffectedPathId);

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int)dr["svnrev_bug"], this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content("You are not allowed to view this item");
            }

            string realPath;
            if (this.applicationSettings.SvnTrustPathsInUrls)
            {
                realPath = path;
            }
            else
            {
                realPath = (string)dr["svnap_path"];
            }

            var rawText = VersionControl.SvnCat((string)dr["svnrev_repository"], realPath, Convert.ToInt32(revpathid));

            return Content(rawText);
        }

        [HttpGet]
        public ActionResult Hook()
        {
            var username = Request["username"];
            var password = Request["password"];

            var svnLog = Request["SvnLog"];
            var repo = Request["repo"];

            if (string.IsNullOrEmpty(username))
            {
                Response.AddHeader("BTNET", "ERROR: username required");

                return Content("ERROR: username required");
            }

            if (username != this.applicationSettings.SvnHookUsername)
            {
                Response.AddHeader("BTNET", "ERROR: wrong username. See Web.config SvnHookUsername");

                return Content("ERROR: wrong username. See Web.config SvnHookUsername");
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

            Util.WriteToLog("SvnLog follows");
            Util.WriteToLog(svnLog);

            Util.WriteToLog("repo follows");
            Util.WriteToLog(repo);

            var doc = new XmlDocument();

            doc.LoadXml(svnLog);

            var revisions = doc.GetElementsByTagName("logentry");

            for (var i = 0; i < revisions.Count; i++)
            {
                var logentry = (XmlElement)revisions[i];

                var msg = logentry.GetElementsByTagName("msg")[0].InnerText;
                var revision = logentry.GetAttribute("revision");
                var author = logentry.GetElementsByTagName("author")[0].InnerText;
                var date = logentry.GetElementsByTagName("date")[0].InnerText;

                var bugids = GetBugidsFromMsg(msg);

                if (bugids == string.Empty) bugids = "0";

                foreach (var bugid in bugids.Split(','))
                    if (Util.IsInt(bugid))
                        InsertRevisionRowPerBug(bugid, repo, revision, author, date, msg, logentry);
            } // end for each revision

            return Content("OK:");
        }

        private void InsertRevisionRowPerBug(string bugid, string repo, string revision, string author, string date,
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

            var svnrevId = Convert.ToInt32(DbUtil.ExecuteScalar(sql));

            if (svnrevId > 0)
            {
                var paths = logentry.GetElementsByTagName("path");

                for (var j = 0; j < paths.Count; j++)
                {
                    var pathElement = (XmlElement)paths[j];

                    var action = pathElement.GetAttribute("action");
                    var filePath = pathElement.InnerText;

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

                    sql = sql.Replace("$svnap_svnrev_id", Convert.ToString(svnrevId));
                    sql = sql.Replace("$svnap_action", action.Replace("'", "''"));
                    sql = sql.Replace("$svnap_path", filePath.Replace("'", "''"));

                    DbUtil.ExecuteNonQuery(sql);
                } // end for each path
            } // if we inserted a revision
        }

        private string GetBugidsFromMsg(string msg)
        {
            var withoutLineBreaks = msg.Replace("\r\n", "").Replace("\n", "");
            var regexPattern1 = this.applicationSettings.SvnBugidRegexPattern1; // at end
            var reIntegerAtEnd = new Regex(regexPattern1);
            var m = reIntegerAtEnd.Match(withoutLineBreaks);

            if (m.Success)
            {
                return m.Groups[1].ToString();
            }

            var regexPattern2 = this.applicationSettings.SvnBugidRegexPattern2; // comma delimited at start
            var reIntegerAtStart = new Regex(regexPattern2);
            var m2 = reIntegerAtStart.Match(withoutLineBreaks);

            if (m2.Success)
            {
                var bugids = m2.Groups[1].ToString().Trim();

                Util.WriteToLog("bugids string: " + bugids);

                return bugids;
            }

            return string.Empty;
        }
    }
}