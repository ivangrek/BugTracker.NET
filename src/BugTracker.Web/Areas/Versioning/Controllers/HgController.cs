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
    public class HgController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;

        public HgController(
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
    }
}