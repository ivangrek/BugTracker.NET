/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Controllers
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;
    using Core;
    using Core.Controls;
    using Models.CustomField;
    using Web.Models;

    [Authorize(Roles = ApplicationRoles.Administrator)]
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
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - custom fields",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new SortableTableModel
            {
                DataTable = Util.GetCustomColumns().Tables[0],
                EditUrl = VirtualPathUtility.ToAbsolute("~/Administration/CustomField/Update/"),
                DeleteUrl = VirtualPathUtility.ToAbsolute("~/Administration/CustomField/Delete/")
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Create()
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - new custom field",
                SelectedItem = MainMenuSections.Administration
            };

            InitLists();

            var model = new CreateModel
            {
                SortSequence = 1
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CreateModel model)
        {
            if (!string.IsNullOrEmpty(model.Name))
            {
                if (model.Name.ToLower() == "url")
                    ModelState.AddModelError("Name", "Field name of \"URL\" causes problems with ASP.NET.");
                else if (model.Name.Contains("'")
                         || model.Name.Contains("\\")
                         || model.Name.Contains("/")
                         || model.Name.Contains("\"")
                         || model.Name.Contains("<")
                         || model.Name.Contains(">"))
                    ModelState.AddModelError("Name", "Some special characters like quotes, slashes are not allowed.");
            }

            if (model.Length == 0)
            {
                if (model.DataType != "int" && model.DataType != "datetime")
                    ModelState.AddModelError("Length", "Length or Precision is required for this datatype.");
            }
            else
            {
                if (model.DataType == "int" || model.DataType == "datetime")
                    ModelState.AddModelError("Length", "Length or Precision not allowed for this datatype.");
            }

            if (model.Required)
            {
                if (string.IsNullOrEmpty(model.Default))
                    ModelState.AddModelError("Default", "If \"Required\" is checked, then Default is required.");

                if (!string.IsNullOrEmpty(model.DropdownType))
                    ModelState.AddModelError("Required",
                        "Checking \"Required\" is not compatible with a normal or users dropdown");
            }

            if (model.DropdownType == "normal")
            {
                if (string.IsNullOrEmpty(model.DropdownValues))
                {
                    ModelState.AddModelError("DropdownValues",
                        "Dropdown values are required for dropdown type of \"normal\".");
                }
                else
                {
                    var valsErrorString = Util.ValidateDropdownValues(model.DropdownValues);

                    if (!string.IsNullOrEmpty(valsErrorString))
                    {
                        ModelState.AddModelError("DropdownValues", valsErrorString);
                    }
                    else
                    {
                        if (model.DataType == "int"
                            || model.DataType == "decimal"
                            || model.DataType == "datetime")
                            ModelState.AddModelError("DataType",
                                "For a normal dropdown datatype must be char, varchar, nchar, or nvarchar.");
                    }
                }
            }
            else if (model.DropdownType == "users")
            {
                if (model.DataType != "int")
                    ModelState.AddModelError("DataType", "For a users dropdown datatype must be int.");
            }

            if (model.DropdownType != "normal")
                if (!string.IsNullOrEmpty(model.DropdownValues))
                    ModelState.AddModelError("DropdownValues",
                        "Dropdown values are only used for dropdown of type \"normal\".");

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Custom field was not created.");

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - new custom field",
                    SelectedItem = MainMenuSections.Administration
                };

                InitLists();

                return View(model);
            }

            var sql = @"
                alter table orgs add [og_$nm_field_permission_level] int null
                alter table bugs add [$nm] $dt $ln $null $df"
                .Replace("$nm", model.Name)
                .Replace("$dt", model.DataType);

            if (model.Length != 0)
                //if (this.length.Value.StartsWith("("))
                //    this.Sql = this.Sql.Replace("$ln", this.length.Value);
                //else
                //    this.Sql = this.Sql.Replace("$ln", "(" + this.length.Value + ")");

                sql = sql.Replace("$ln", "(" + model.Length + ")");
            else
                sql = sql.Replace("$ln", string.Empty);

            if (!string.IsNullOrEmpty(model.Default))
            {
                if (model.Default.StartsWith("("))
                    sql = sql.Replace("$df", "DEFAULT " + model.Default);
                else
                    sql = sql.Replace("$df", "DEFAULT (" + model.Default + ")");
            }
            else
            {
                sql = sql.Replace("$df", "");
            }

            if (model.Required)
                sql = sql.Replace("$null", "NOT NULL");
            else
                sql = sql.Replace("$null", "NULL");

            var alterTableWorked = false;

            try
            {
                DbUtil.ExecuteNonQuery(sql);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "The generated SQL was invalid:<br><br>SQL:&nbsp;" + sql +
                                                       "<br><br>Error:&nbsp;" + ex.Message);

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - custom field",
                    SelectedItem = MainMenuSections.Administration
                };

                InitLists();

                return View(model);
            }

            sql = @"declare @colorder int

                select @colorder = sc.colorder
                from syscolumns sc
                inner join sysobjects so on sc.id = so.id
                where so.name = 'bugs'
                and sc.name = '$nm'

                insert into custom_col_metadata
                (ccm_colorder, ccm_dropdown_vals, ccm_sort_seq, ccm_dropdown_type)
                values(@colorder, N'$v', $ss, '$dt')";

            sql = sql.Replace("$nm", model.Name);
            sql = sql.Replace("$v", model.DropdownValues);
            sql = sql.Replace("$ss", model.SortSequence.ToString());
            sql = sql.Replace("$dt", model.DropdownType);

            DbUtil.ExecuteNonQuery(sql);
            System.Web.HttpContext.Current.Application["custom_columns_dataset"] = null;

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Update(int id)
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit custom field",
                SelectedItem = MainMenuSections.Administration
            };

            // Get this entry's data from the db and fill in the form
            var sql = @"
                select sc.name,
                isnull(ccm_dropdown_vals,'') [vals],
                isnull(ccm_dropdown_type,'') [dropdown_type],
                isnull(ccm_sort_seq, sc.colorder) [column order],
                mm.text [default value], dflts.name [default name]
                from syscolumns sc
                inner join sysobjects so on sc.id = so.id
                left outer join custom_col_metadata ccm on ccm_colorder = sc.colorder
                left outer join syscomments mm on sc.cdefault = mm.id
                left outer join sysobjects dflts on dflts.id = mm.id
                where so.name = 'bugs'
                and sc.colorder = $co";

            sql = sql.Replace("$co", Convert.ToString(id));

            var dr = DbUtil.GetDataRow(sql);

            var model = new UpdateModel
            {
                Id = id,
                Name = (string) dr["name"],
                DropdownType = Convert.ToString(dr["dropdown_type"]),

                DropdownValues = (string) dr["vals"],

                SortSequence = (int) dr["column order"],
                Default = Convert.ToString(dr["default value"]),

                DefaultName = Convert.ToString(dr["default name"]),
                DefaultValue = Convert.ToString(dr["default value"]) // to test if it changed
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(UpdateModel model)
        {
            if (model.DropdownType == "normal")
            {
                if (string.IsNullOrEmpty(model.DropdownValues))
                {
                    ModelState.AddModelError("DropdownValues",
                        "Dropdown values are required for dropdown type of \"normal\".");
                }
                else
                {
                    var valsErrorString = Util.ValidateDropdownValues(model.DropdownValues);

                    if (!string.IsNullOrEmpty(valsErrorString))
                        ModelState.AddModelError("DropdownValues", valsErrorString);
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - edit custom field",
                    SelectedItem = MainMenuSections.Administration
                };

                return View(model);
            }

            var sql = @"declare @count int
                select @count = count(1) from custom_col_metadata
                where ccm_colorder = $co

                if @count = 0
                    insert into custom_col_metadata
                    (ccm_colorder, ccm_dropdown_vals, ccm_sort_seq, ccm_dropdown_type)
                    values($co, N'$v', $ss, '$dt')
                else
                    update custom_col_metadata
                    set ccm_dropdown_vals = N'$v',
                    ccm_sort_seq = $ss
                    where ccm_colorder = $co";

            sql = sql.Replace("$co", Convert.ToString(model.Id));
            sql = sql.Replace("$v", model.DropdownValues);
            sql = sql.Replace("$ss", model.SortSequence.ToString());

            DbUtil.ExecuteNonQuery(sql);
            System.Web.HttpContext.Current.Application["custom_columns_dataset"] = null;

            if (model.Default != model.DefaultValue)
            {
                if (!string.IsNullOrEmpty(model.Default) && !string.IsNullOrEmpty(model.DefaultValue))
                {
                    sql = "alter table bugs drop constraint [" +
                          model.DefaultValue + "]";

                    DbUtil.ExecuteNonQuery(sql);
                    System.Web.HttpContext.Current.Application["custom_columns_dataset"] = null;
                }

                if (!string.IsNullOrEmpty(model.Default))
                {
                    sql = "alter table bugs add constraint [" + Guid.NewGuid() + "] default " +
                          model.Default + " for [" + model.Name + "]";

                    DbUtil.ExecuteNonQuery(sql);
                    System.Web.HttpContext.Current.Application["custom_columns_dataset"] = null;
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
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
                Name = (string) dr["name"]
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(DeleteModel model)
        {
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
                    .Replace("$df", (string) dr["default_constraint_name"]);

                DbUtil.ExecuteNonQuery(sql);
            }

            // delete column itself
            sql = @"
                alter table orgs drop column [og_$nm_field_permission_level]
                alter table bugs drop column [$nm]"
                .Replace("$nm", (string) dr["column_name"]);

            DbUtil.ExecuteNonQuery(sql);

            //delete row from custom column table
            sql = @"delete from custom_col_metadata
                    where ccm_colorder = $num"
                .Replace("$num", model.Id.ToString());

            System.Web.HttpContext.Current.Application["custom_columns_dataset"] = null;

            DbUtil.ExecuteNonQuery(sql);

            return RedirectToAction(nameof(Index));
        }

        private void InitLists()
        {
            ViewBag.DropdownTypes = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = string.Empty,
                    Text = "not a dropdown"
                },
                new SelectListItem
                {
                    Value = "normal",
                    Text = "normal"
                },
                new SelectListItem
                {
                    Value = string.Empty,
                    Text = "users"
                }
            };

            ViewBag.DataTypes = new List<SelectListItem>
            {
                new SelectListItem
                {
                    Value = "char",
                    Text = "char"
                },
                new SelectListItem
                {
                    Value = "datetime",
                    Text = "datetime"
                },
                new SelectListItem
                {
                    Value = "decimal",
                    Text = "decimal"
                },
                new SelectListItem
                {
                    Value = "int",
                    Text = "int"
                },
                new SelectListItem
                {
                    Value = "nchar",
                    Text = "nchar"
                },
                new SelectListItem
                {
                    Value = "nvarchar",
                    Text = "nvarchar"
                },
                new SelectListItem
                {
                    Value = "varchar",
                    Text = "varchar"
                }
            };
        }
    }
}