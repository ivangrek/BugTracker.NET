/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Controllers
{
    using BugTracker.Web.Areas.Administration.Models.CustomField;
    using BugTracker.Web.Core;
    using BugTracker.Web.Core.Controls;
    using BugTracker.Web.Models;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;

    [OutputCache(Location = OutputCacheLocation.None)]
    public class CustomFieldController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;

        public CustomFieldController(
            IApplicationSettings applicationSettings,
            ISecurity security)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
        }

        [HttpGet]
        public ActionResult Index()
        {
            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - custom fields",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new SortableTableModel
            {
                DataSet = Util.GetCustomColumns(),
                EditUrl = VirtualPathUtility.ToAbsolute("~/Administration/CustomField/Update/"),
                DeleteUrl = VirtualPathUtility.ToAbsolute("~/Administration/CustomField/Delete/")
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            var sql = @"select sc.name
                from syscolumns sc
                inner join sysobjects so on sc.id = so.id
                left outer join sysobjects df on df.id = sc.cdefault
                where so.name = 'bugs'
                and sc.colorder = $id"
                .Replace("$id", id.ToString());

            var dr = DbUtil.GetDataRow(sql);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete custom field",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new DeleteModel
            {
                Id = id,
                Name = (string)dr["name"]
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(DeleteModel model)
        {
            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            // do delete here
            var sql = @"select sc.name [column_name], df.name [default_constraint_name]
                from syscolumns sc
                inner join sysobjects so on sc.id = so.id
                left outer join sysobjects df on df.id = sc.cdefault
                where so.name = 'bugs'
                and sc.colorder = $id"
                .Replace("$id", model.Id.ToString());

            var dr = DbUtil.GetDataRow(sql);

            // if there is a default, delete it
            if (!string.IsNullOrEmpty(dr["default_constraint_name"].ToString()))
            {
                sql = @"alter table bugs drop constraint [$df]"
                    .Replace("$df", (string)dr["default_constraint_name"]);

                DbUtil.ExecuteNonQuery(sql);
            }

            // delete column itself
            sql = @"
                alter table orgs drop column [og_$nm_field_permission_level]
                alter table bugs drop column [$nm]"
                .Replace("$nm", (string)dr["column_name"]);

            DbUtil.ExecuteNonQuery(sql);

            //delete row from custom column table
            sql = @"delete from custom_col_metadata
                    where ccm_colorder = $num"
                .Replace("$num", model.Id.ToString());

            System.Web.HttpContext.Current.Application["custom_columns_dataset"] = null;

            DbUtil.ExecuteNonQuery(sql);

            return RedirectToAction(nameof(Index));
        }
    }
}