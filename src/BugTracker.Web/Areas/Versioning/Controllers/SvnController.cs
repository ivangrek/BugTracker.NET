namespace BugTracker.Web.Areas.Versioning.Controllers
{
    using System;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;
    using System.Xml;
    using Core;
    using Core.Controls;
    using Models;

    [Authorize]
    [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
    public class SvnController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly IAuthenticate authenticate;
        private readonly ISecurity security;

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
            var permissionLevel = Bug.GetBugPermissionLevel(id, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
                return Content("You are not allowed to view this item");

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
                    '<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Versioning/Svn/Diff") +
                      @"?revpathid=' + convert(varchar,svnap_id) + '>diff</a>'
                    else
                    ''
                end [view<br>diff],

                case when svnap_action not like '%D%' then
                '<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Versioning/Svn/Log") +
                      @"?revpathid=' + convert(varchar,svnap_id) + '>history</a>'
                    else
                    ''
                end [view<br>history<br>(svn log)]";

            //	if (websvn_url != string.Empty)
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
                DataTable = DbUtil.GetDataSet(sql).Tables[0],
                HtmlEncode = false
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Show(int revpathid, string revision, string path)
        {
            Response.ContentType = "text/plain";

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
                return Content("You are not allowed to view this item");

            string realPath;
            if (this.applicationSettings.SvnTrustPathsInUrls)
                realPath = path;
            else
                realPath = (string)dr["svnap_path"];

            var rawText = VersionControl.SvnCat((string)dr["svnrev_repository"], realPath, Convert.ToInt32(revpathid));

            return Content(rawText);
        }

        [HttpGet]
        public ActionResult Blame(int revpathid)
        {
            var sql = @"
                select svnrev_revision, svnrev_repository, svnap_path, svnrev_bug
                from svn_revisions
                inner join svn_affected_paths on svnap_svnrev_id = svnrev_id
                where svnap_id = $id
                order by svnrev_revision desc, svnap_path";

            var stringAffectedPathId = Convert.ToString(revpathid);

            sql = sql.Replace("$id", stringAffectedPathId);

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int)dr["svnrev_bug"], this.security);
            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var revision = Convert.ToInt32(Request["rev"]);
            var repo = (string)dr["svnrev_repository"];
            var path = string.Empty;

            if (this.applicationSettings.SvnTrustPathsInUrls)
                path = Request["path"];
            else
                path = (string)dr["svnap_path"];

            var rawText = VersionControl.SvnCat(repo, path, revision);

            if (rawText.StartsWith("ERROR:"))
            {
                Response.Write(HttpUtility.HtmlEncode(rawText));
                Response.End();
            }

            var blameText = VersionControl.SvnBlame(repo, path, revision);

            if (blameText.StartsWith("ERROR:"))
            {
                Response.Write(HttpUtility.HtmlEncode(blameText));
                Response.End();
            }

            ViewBag.BlameHtml = WriteBlame(blameText, rawText);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"svn blame {HttpUtility.HtmlEncode(path)} {Convert.ToString(revision)}",
                SelectedItem = MainMenuSections.Administration
            };

            return View();
        }

        [HttpGet]
        public ActionResult Log(int revpathid)
        {
            var sql = @"
                select svnrev_revision, svnrev_repository, svnap_path, svnrev_bug
                from svn_revisions
                inner join svn_affected_paths on svnap_svnrev_id = svnrev_id
                where svnap_id = $id
                order by svnrev_revision desc, svnap_path";

            var stringAffectedPathId = Convert.ToString(revpathid);

            sql = sql.Replace("$id", stringAffectedPathId);

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int)dr["svnrev_bug"], this.security);
            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            ViewBag.RevPathId = stringAffectedPathId;

            var repo = (string)dr["svnrev_repository"];
            var filePath = (string)dr["svnap_path"];
            var rev = (int)dr["svnrev_revision"];

            var logResult = VersionControl.SvnLog(repo, filePath, rev);

            if (logResult.StartsWith("ERROR:"))
            {
                Response.Write(HttpUtility.HtmlEncode(logResult));
                Response.End();
            }

            ViewBag.HistoryHtml = FetchAndWriteHistory(filePath, logResult, stringAffectedPathId);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"svn log {HttpUtility.HtmlEncode(filePath)}",
                SelectedItem = MainMenuSections.Administration
            };

            return View();
        }

        [HttpGet]
        public ActionResult Diff(int revpathid)
        {
            var sql = @"
                select svnrev_revision, svnrev_repository, svnap_path, svnrev_bug
                from svn_revisions
                inner join svn_affected_paths on svnap_svnrev_id = svnrev_id
                where svnap_id = $id";

            sql = sql.Replace("$id", Convert.ToString(revpathid));

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int)dr["svnrev_bug"], this.security);
            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var repo = (string)dr["svnrev_repository"];
            string path0;
            string path1;

            if (this.applicationSettings.SvnTrustPathsInUrls)
            {
                path0 = Request["path_0"];
                path1 = Request["path_1"];
            }
            else
            {
                path0 = path1 = (string)dr["svnap_path"];
            }

            var error = string.Empty;

            var stringRevision0 = Request["rev_0"];
            var leftOut = string.Empty;
            var rightOut = string.Empty;

            if (string.IsNullOrEmpty(stringRevision0))
            {
                var revision = (int)dr["svnrev_revision"];
                var unifiedDiffText = VersionControl.SvnDiff(repo, path1, revision, 0);

                ExamineDiff(unifiedDiffText);

                // get the old revision number
                var regex = new Regex("\n");
                var diffLines = regex.Split(unifiedDiffText.Replace("\r\n", "\n"));

                var line = diffLines[2];
                var oldRevPos1 = line.ToLower().IndexOf("(revision "); // 10 chars long
                var oldRevPosStartOfInt = oldRevPos1 + 10;
                var oldRevAfterInt = line.IndexOf(")", oldRevPosStartOfInt);
                var oldRevisionString = line.Substring(oldRevPosStartOfInt,
                    oldRevAfterInt - oldRevPosStartOfInt);

                var oldRevision = Convert.ToInt32(oldRevisionString);

                // get the source code for both the left and right
                var leftText = VersionControl.SvnCat(repo, path0, oldRevision);
                var rightText = VersionControl.SvnCat(repo, path1, revision);

                ViewBag.LeftTitle = Convert.ToString(oldRevision);
                ViewBag.RightTitle = Convert.ToString(revision);

                error = VersionControl.VisualDiff(unifiedDiffText, leftText, rightText, ref leftOut,
                    ref rightOut);
            }
            else
            {
                var revision1 = Convert.ToInt32(Request["rev_1"]);
                var revision0 = Convert.ToInt32(stringRevision0);

                ViewBag.UnifiedDiffText = VersionControl.SvnDiff(repo, path1, revision1, revision0);

                ExamineDiff(ViewBag.UnifiedDiffText);

                // get the source code for both the left and right
                var leftText = VersionControl.SvnCat(repo, path0, revision0);
                var rightText = VersionControl.SvnCat(repo, path1, revision1);
                ViewBag.LeftTitle = Convert.ToString(revision0);
                ViewBag.RightTitle = Convert.ToString(revision1);

                error = VersionControl.VisualDiff(ViewBag.UnifiedDiffText, leftText, rightText, ref leftOut,
                    ref rightOut);
            }

            if (!string.IsNullOrEmpty(error))
            {
                Response.Write(HttpUtility.HtmlEncode(error));
                Response.End();
            }

            ViewBag.LeftOut = leftOut;
            ViewBag.RightOut = rightOut;

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"svn diff {HttpUtility.HtmlEncode(path1)}",
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

                if (string.IsNullOrEmpty(bugids)) bugids = "0";

                foreach (var bugid in bugids.Split(','))
                    if (Util.IsInt(bugid))
                        InsertRevisionRowPerBug(bugid, repo, revision, author, date, msg, logentry);
            } // end for each revision

            return Content("OK:");
        }

        private static void InsertRevisionRowPerBug(string bugid, string repo, string revision, string author,
            string date,
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

            if (m.Success) return m.Groups[1].ToString();

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

        public static string WriteBlame(string blameText, string rawText)
        {
            var stringBuilder = new StringBuilder();
            var doc = new XmlDocument();

            doc.LoadXml(blameText);

            var commits = doc.GetElementsByTagName("commit");

            // split the source text into lines
            var regex = new Regex("\n");
            var lines = regex.Split(rawText.Replace("\r\n", "\n"));

            for (var i = 0; i < commits.Count; i++)
            {
                var commit = (XmlElement)commits[i];
                stringBuilder.Append("<tr><td nowrap>" + commit.GetAttribute("revision"));

                var author = string.Empty;
                var date = string.Empty;

                foreach (XmlNode node in commit.ChildNodes)
                    if (node.Name == "author") author = node.InnerText;
                    else if (node.Name == "date")
                        date = Util.FormatDbDateTime(XmlConvert.ToDateTime(node.InnerText,
                            XmlDateTimeSerializationMode.Local));

                stringBuilder.Append("<td nowrap>" + author);
                stringBuilder.Append("<td nowrap style='background: #ddffdd'><pre style='display:inline;'> " +
                               HttpUtility.HtmlEncode(lines[i]));
                stringBuilder.Append(" </pre><td nowrap>" + date);
            }

            return stringBuilder.ToString();
        }

        public static string FetchAndWriteHistory(string filePath, string logResult, string stringAffectedPathId)
        {
            var stringBuilder = new StringBuilder();
            var doc = new XmlDocument();
            doc.LoadXml(logResult);

            var logNode = doc.ChildNodes[1];
            //string adjusted_file_path = "/" + file_path; // when/why did this stop working?
            var adjustedFilePath = filePath;

            var row = 0;
            foreach (XmlElement logentry in logNode)
            {
                var revision = logentry.GetAttribute("revision");
                var author = string.Empty;
                var date = string.Empty;
                var path = string.Empty;
                var action = string.Empty;
                //string copy_from = string.Empty;
                //string copy_from_rev = string.Empty;
                var msg = string.Empty;

                foreach (XmlNode node in logentry.ChildNodes)
                    if (node.Name == "author") author = node.InnerText;
                    else if (node.Name == "date")
                        date = Util.FormatDbDateTime(XmlConvert.ToDateTime(node.InnerText,
                            XmlDateTimeSerializationMode.Local));
                    else if (node.Name == "msg") msg = node.InnerText;
                    else if (node.Name == "paths")
                        foreach (XmlNode pathNode in node.ChildNodes)
                            if (pathNode.InnerText == adjustedFilePath)
                            {
                                var pathEl = (XmlElement)pathNode;
                                action = pathEl.GetAttribute("action");
                                if (!action.Contains("D"))
                                {
                                    path = pathNode.InnerText;
                                    path = adjustedFilePath;
                                    if (!string.IsNullOrEmpty(pathEl.GetAttribute("copyfrom-path")))
                                        adjustedFilePath = pathEl.GetAttribute("copyfrom-path");
                                }
                            }

                stringBuilder.Append("<tr><td class=datad>" + revision);
                stringBuilder.Append("<td class=datad>" + author);
                stringBuilder.Append("<td class=datad>" + date);
                stringBuilder.Append("<td class=datad>" + path);
                stringBuilder.Append("<td class=datad>" + action);
                //        Response.Write("<td class=datad>" + copy_from);
                //        Response.Write("<td class=datad>" + copy_from_rev);
                stringBuilder.Append("<td class=datad>" + msg.Replace(Environment.NewLine, "<br/>"));

                stringBuilder.Append(
                    "<td class=datad><a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Versioning/Svn/Show") + @"?revpathid=" + stringAffectedPathId
                                                                                     + "&revision=" + revision
                                                                                     + "&path=" +
                                                                                     HttpUtility.UrlEncode(path)
                                                                                     + ">");

                stringBuilder.Append("view</a>");

                stringBuilder.Append(
                    "<td class=datad><a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Versioning/Svn/Blame") + @"?revpathid=" + stringAffectedPathId
                                                                                      + "&revision=" + revision
                                                                                      + "&path=" +
                                                                                      HttpUtility.UrlEncode(path)
                                                                                      + ">");

                stringBuilder.Append("annotated</a>");

                stringBuilder.Append("<td class=datad><a id=" + revision
                                                        + " href='javascript:sel_for_diff("
                                                        + Convert.ToString(row)
                                                        + ",\""
                                                        + revision
                                                        + "\",\""
                                                        + path
                                                        + "\")'>select for diff</a>");
            }

            return stringBuilder.ToString();
        }

        public void ExamineDiff(string unifiedDiffText)
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