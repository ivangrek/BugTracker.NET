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

    [Authorize]
    [OutputCache(Location = OutputCacheLocation.None)]
    public class HgController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;
        private readonly IAuthenticate authenticate;

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
            {
                return Content("You are not allowed to view this item");
            }

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
                        '<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Versioning/Hg/Diff.aspx") + @"?revpathid=' + convert(varchar,hgap_id) + '>diff</a>'
                        else
                        ''
                    end [view<br>diff],

                    case when hgap_action not like '%D%' then
                    '<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Versioning/Hg/Log.aspx") + @"?revpathid=' + convert(varchar,hgap_id) + '>history</a>'
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
                DataSet = DbUtil.GetDataSet(sql),
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
            {
                return Content("You are not allowed to view this item");
            }

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
                Title = "hg blame " + HttpUtility.HtmlEncode(revision) + " -- " + HttpUtility.HtmlEncode((string)dr["hgap_path"]),
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
                        var pathElement = (XmlElement)paths[j];

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

            return Content("OK:");
        }

        private string GetBugidFromDesc(string desc)
        {
            var regexPattern = this.applicationSettings.MercurialBugidRegexPattern;
            var reInteger = new Regex(regexPattern);
            var m = reInteger.Match(desc);

            if (m.Success)
            {
                return m.Groups[1].ToString();
            }

            return string.Empty;
        }
    }
}