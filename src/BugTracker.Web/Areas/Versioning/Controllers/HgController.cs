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
    public class HgController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly IAuthenticate authenticate;
        private readonly ISecurity security;

        public HgController(
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
                Title = $"{this.applicationSettings.AppTitle} - view hg file revisions",
                SelectedItem = MainMenuSections.Administration
            };

            var sql = @"
                select 
                    hgrev_revision [revision],
                    hgrev_repository [repo],
                    hgap_action [action],
                    hgap_path [file],
                    replace(replace(hgrev_author,'<','&lt;'),'>','&gt;') [user],
                    substring(hgrev_hg_date,1,19) [date],
                    replace(substring(hgrev_msg,1,4000),char(13),'<br>') [msg],

                    case when hgap_action not like '%D%' and hgap_action not like 'A%' then
                        '<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Versioning/Hg/Diff") +
                      @"?revpathid=' + convert(varchar,hgap_id) + '>diff</a>'
                        else
                        ''
                    end [view<br>diff],

                    case when hgap_action not like '%D%' then
                    '<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Versioning/Hg/Log") +
                      @"?revpathid=' + convert(varchar,hgap_id) + '>history</a>'
                        else
                        ''
                    end [view<br>history<br>(hg log)]

                    from hg_revisions
                    inner join hg_affected_paths on hgap_hgrev_id = hgrev_id
                    where hgrev_bug = $bg
                    order by hgrev_hg_date desc, hgap_path"
                          .Replace("$bg", Convert.ToString(id));

            var model = new SortableTableModel
            {
                DataTable = DbUtil.GetDataSet(sql).Tables[0],
                HtmlEncode = false
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Show(int revpathid, string revision)
        {
            Response.ContentType = "text/plain";

            var sql = @"
                select hgrev_revision, hgrev_bug, hgrev_repository, hgap_path 
                from hg_revisions
                inner join hg_affected_paths on hgap_hgrev_id = hgrev_id
                where hgap_id = $id";

            sql = sql.Replace("$id", Convert.ToString(revpathid));

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int)dr["hgrev_bug"], this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
                return Content("You are not allowed to view this item");

            var repo = (string)dr["hgrev_repository"];
            var path = (string)dr["hgap_path"];

            var text = VersionControl.HgGetFileContents(repo, revision, path);

            return Content(text);
        }

        [HttpGet]
        public ActionResult Blame(int revpathid, string revision)
        {
            var sql = @"
                select hgrev_revision, hgrev_bug, hgrev_repository, hgap_path 
                from hg_revisions
                inner join hg_affected_paths on hgap_hgrev_id = hgrev_id
                where hgap_id = $id";

            sql = sql.Replace("$id", Convert.ToString(revpathid));

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int)dr["hgrev_bug"], this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var repo = (string)dr["hgrev_repository"];

            ViewBag.BlameText = VersionControl.HgBlame(repo, (string)dr["hgap_path"], revision);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = "hg blame " + HttpUtility.HtmlEncode(revision) + " -- " +
                        HttpUtility.HtmlEncode((string)dr["hgap_path"]),
                SelectedItem = MainMenuSections.Administration
            };

            return View();
        }

        [HttpGet]
        public ActionResult Log(int revpathid)
        {
            // get info about commit

            var sql = @"
                select hgrev_repository, hgrev_revision, hgap_path, hgrev_bug
                from hg_revisions
                inner join hg_affected_paths on hgap_hgrev_id = hgrev_id
                where hgap_id = $id
                order by hgrev_revision desc, hgap_path";

            var stringAffectedPathId = Convert.ToString(revpathid);
            sql = sql.Replace("$id", stringAffectedPathId);

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var bugid = (int)dr["hgrev_bug"];

            var permissionLevel = Bug.GetBugPermissionLevel(bugid, this.security);
            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            ViewBag.RevPathId = stringAffectedPathId;

            var repo = (string)dr["hgrev_repository"];
            var revision = Convert.ToString((int)dr["hgrev_revision"]);
            var filePath = (string)dr["hgap_path"];

            ViewBag.LogResult = VersionControl.HgLog(repo, revision, filePath);
            ViewBag.HistoryHtml = FetchAndWriteHistory(ViewBag.LogResult, stringAffectedPathId);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"hg log {HttpUtility.HtmlEncode(filePath)}",
                SelectedItem = MainMenuSections.Administration
            };

            return View();
        }

        [HttpGet]
        public ActionResult Diff(int revpathid)
        {
            // get info about revision
            var sql = @"
                select hgrev_revision, hgrev_bug, hgrev_repository, hgap_path 
                from hg_revisions
                inner join hg_affected_paths on hgap_hgrev_id = hgrev_id
                where hgap_id = $id";

            var hgapId = Convert.ToInt32(Util.SanitizeInteger(Request["revpathid"]));
            sql = sql.Replace("$id", Convert.ToString(hgapId));

            var dr = DbUtil.GetDataRow(sql);

            // check if user has permission for this bug
            var permissionLevel = Bug.GetBugPermissionLevel((int)dr["hgrev_bug"], this.security);
            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var repo = (string)dr["hgrev_repository"];
            var path = (string)dr["hgap_path"];
            var leftOut = string.Empty;
            var rightOut = string.Empty;

            var error = string.Empty;

            var revision0 = Request["rev_0"];

            if (string.IsNullOrEmpty(revision0))
            {
                var revision = Convert.ToString((int)dr["hgrev_revision"]);

                // we need to find the previous revision
                var log = VersionControl.HgLog(repo, revision, path);
                var prevRevision = GetPreviousRevision(log, revision);

                if (string.IsNullOrEmpty(prevRevision))
                {
                    Response.Write("unable to determine previous revision from log");
                    Response.End();
                }

                ViewBag.UnifiedDiffText =
                    VersionControl.HgGetUnifiedDiffTwoRevisions(repo, prevRevision, revision, path);

                // get the source code for both the left and right
                var leftText = VersionControl.HgGetFileContents(repo, prevRevision, path);

                var rightText = VersionControl.HgGetFileContents(repo, revision, path);
                ViewBag.LeftTitle = prevRevision;
                ViewBag.RightTitle = revision;

                error = VersionControl.VisualDiff(ViewBag.UnifiedDiffText, leftText, rightText, ref leftOut,
                    ref rightOut);
            }
            else
            {
                var revision1 = Request["rev_1"];

                ViewBag.UnifiedDiffText = VersionControl.HgGetUnifiedDiffTwoRevisions(repo, revision0, revision1, path);

                // get the source code for both the left and right
                var leftText = VersionControl.HgGetFileContents(repo, revision0, path);
                var rightText = VersionControl.HgGetFileContents(repo, revision1, path);
                ViewBag.LeftTitle = revision0;
                ViewBag.RightTitle = revision1;

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
                Title = $"git diff {HttpUtility.HtmlEncode((string)dr["gitap_path"])}",
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

            var hgLog = Request["hg_log"];
            var repo = Request["repo"];

            if (string.IsNullOrEmpty(username))
            {
                Response.AddHeader("BTNET", "ERROR: username required");

                return Content("ERROR: username required");
            }

            if (username != this.applicationSettings.MercurialHookUsername)
            {
                Response.AddHeader("BTNET", "ERROR: wrong username. See Web.config MercurialHookUsername");

                return Content("ERROR: wrong username. See Web.config MercurialHookUsernam");
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

            Util.WriteToLog("hg_log follows");
            Util.WriteToLog(hgLog);

            Util.WriteToLog("repo follows");
            Util.WriteToLog(repo);

            var doc = new XmlDocument();

            doc.LoadXml("<log>" + hgLog + "</log>");

            var revisions = doc.GetElementsByTagName("changeset");

            for (var i = 0; i < revisions.Count; i++)
            {
                var changeset = (XmlElement)revisions[i];

                var desc = changeset.GetElementsByTagName("desc")[0].InnerText;
                var bug = GetBugidFromDesc(desc);

                if (string.IsNullOrEmpty(bug)) bug = "0";

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
                        var pathElement = (XmlElement)paths[j];

                        var action = string.Empty; // no action in hg?  path_element.GetAttribute("action");
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

            return Content("OK:");
        }

        private string GetBugidFromDesc(string desc)
        {
            var regexPattern = this.applicationSettings.MercurialBugidRegexPattern;
            var reInteger = new Regex(regexPattern);
            var m = reInteger.Match(desc);

            if (m.Success) return m.Groups[1].ToString();

            return string.Empty;
        }

        public string FetchAndWriteHistory(string logResult, string stringAffectedPathId)
        {
            var stringBuilder = new StringBuilder();
            var doc = new XmlDocument();
            doc.LoadXml("<log>" + logResult + "</log>");

            var revisions = doc.GetElementsByTagName("changeset");

            var row = 0;

            // read backwards
            for (var i = revisions.Count - 1; i > -1; i--)
            {
                var changeset = (XmlElement)revisions[i];

                var revision = changeset.GetAttribute("node");
                var author = changeset.GetElementsByTagName("auth")[0].InnerText;
                var date = changeset.GetElementsByTagName("date")[0].InnerText;
                var desc = changeset.GetElementsByTagName("desc")[0].InnerText;
                var path = changeset.GetElementsByTagName("file")[0].InnerText;

                stringBuilder.Append("<tr><td class=datad>" + revision);
                stringBuilder.Append("<td class=datad>" + author);
                stringBuilder.Append("<td class=datad>" + date);
                stringBuilder.Append("<td class=datad>" + path);
                //        Response.Write("<td class=datad>" + action);
                //        Response.Write("<td class=datad>" + copy_from);
                //        Response.Write("<td class=datad>" + copy_from_rev);

                stringBuilder.Append("<td class=datad>" + desc.Replace(Environment.NewLine, "<br/>"));

                stringBuilder.Append(
                    "<td class=datad><a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Versioning/Hg/Show") + @"?revpathid=" + stringAffectedPathId
                                                                                    + "&revision=" + revision
                                                                                    + ">");

                stringBuilder.Append("view</a>");

                stringBuilder.Append(
                    "<td class=datad><a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Versioning/Hg/Blame") + @"?revpathid=" + stringAffectedPathId
                                                                                     + "&revision=" + revision
                                                                                     + ">");

                stringBuilder.Append("annotated</a>");

                stringBuilder.Append("<td class=datad><a id=" + revision
                                                        + " href='javascript:sel_for_diff("
                                                        + Convert.ToString(++row)
                                                        + ",\""
                                                        + revision
                                                        + "\",\"\")'>select for diff</a>");
            }

            return stringBuilder.ToString();
        }

        public static string GetPreviousRevision(string logResult, string thisRevision)
        {
            var doc = new XmlDocument();
            doc.LoadXml("<log>" + logResult + "</log>");
            var revisions = doc.GetElementsByTagName("changeset");

            // read backwards
            if (revisions.Count > 1)
            {
                var changeset = (XmlElement)revisions[revisions.Count - 2];

                return changeset.GetAttribute("rev");
            }

            return string.Empty;
        }
    }
}