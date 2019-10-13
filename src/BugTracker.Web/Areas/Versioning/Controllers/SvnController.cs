namespace BugTracker.Web.Areas.Versioning.Controllers
{
    using BugTracker.Web.Core;
    using BugTracker.Web.Core.Controls;
    using BugTracker.Web.Models;
    using System;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;

    [OutputCache(Location = OutputCacheLocation.None)]
    public class SvnController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;

        public SvnController(
            IApplicationSettings applicationSettings,
            ISecurity security)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
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
    }
}