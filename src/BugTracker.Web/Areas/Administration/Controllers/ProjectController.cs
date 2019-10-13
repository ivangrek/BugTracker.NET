/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Controllers
{
    using BugTracker.Web.Areas.Administration.Models.Project;
    using BugTracker.Web.Core;
    using BugTracker.Web.Core.Controls;
    using BugTracker.Web.Models;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Web;
    using System.Web.Mvc;

    public class ProjectController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;

        public ProjectController(
            IApplicationSettings applicationSettings,
            ISecurity security)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
        }

        [HttpGet]
        public ActionResult Index()
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - projects",
                SelectedItem = MainMenuSections.Administration
            };

            var dataSet = DbUtil.GetDataSet(
                @"select
                    pj_id [id],
                    '<a href=" + VirtualPathUtility.ToAbsolute("~/Administration/Project/Update/") + @"' + convert(varchar,pj_id) + '>edit</a>' [$no_sort_edit],
                    '<a href=" + VirtualPathUtility.ToAbsolute("~/Administration/Project/UpdateUserPermission/") + @"' + convert(varchar,pj_id) + '?projects=true>permissions</a>' [$no_sort_per user<br>permissions],
                    '<a href=" + VirtualPathUtility.ToAbsolute("~/Administration/Project/Delete/") + @"' + convert(varchar,pj_id) + '>delete</a>' [$no_sort_delete],
                    pj_name [project],
                    case when pj_active = 1 then 'Y' else 'N' end [active],
                    us_username [default user],
                    case when isnull(pj_auto_assign_default_user,0) = 1 then 'Y' else 'N' end [auto assign<br>default user],
                    case when isnull(pj_auto_subscribe_default_user,0) = 1 then 'Y' else 'N' end [auto subscribe<br>default user],
                    case when isnull(pj_enable_pop3,0) = 1 then 'Y' else 'N' end [receive items<br>via pop3],
                    pj_pop3_username [pop3 username],
                    pj_pop3_email_from [from email addressl],
                    case when pj_default = 1 then 'Y' else 'N' end [default]
                    from projects
                    left outer join users on us_id = pj_default_user
                    order by pj_name");

            var model = new SortableTableModel
            {
                DataSet = dataSet,
                HtmlEncode = false
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Create()
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - create project",
                SelectedItem = MainMenuSections.Administration
            };

            var users = DbUtil.GetDataView("select us_id, us_username from users order by us_username");

            ViewBag.DefaultUsers = new List<SelectListItem>();

            foreach (DataRowView user in users)
            {
                ViewBag.DefaultUsers.Add(new SelectListItem
                {
                    Value = ((int)user["us_id"]).ToString(),
                    Text = (string)user["us_username"],
                });
            }

            var model = new EditModel
            {
                Active = true
            };


            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EditModel model)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            var valsErrorString = Util.ValidateDropdownValues(model.CustomDropdown1Values);

            if (!string.IsNullOrEmpty(valsErrorString))
            {
                ModelState.AddModelError("CustomDropdown1Values", valsErrorString);
            }

            valsErrorString = Util.ValidateDropdownValues(model.CustomDropdown2Values);

            if (!string.IsNullOrEmpty(valsErrorString))
            {
                ModelState.AddModelError("CustomDropdown2Values", valsErrorString);
            }

            valsErrorString = Util.ValidateDropdownValues(model.CustomDropdown3Values);

            if (!string.IsNullOrEmpty(valsErrorString))
            {
                ModelState.AddModelError("CustomDropdown3Values", valsErrorString);
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Message", "Custom fields have errors.");

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - create project",
                    SelectedItem = MainMenuSections.Administration
                };

                var users = DbUtil.GetDataView("select us_id, us_username from users order by us_username");

                ViewBag.DefaultUsers = new List<SelectListItem>();

                foreach (DataRowView user in users)
                {
                    ViewBag.DefaultUsers.Add(new SelectListItem
                    {
                        Value = ((int)user["us_id"]).ToString(),
                        Text = (string)user["us_username"],
                    });
                }

                return View("Edit", model);
            }

            var sql = @"insert into projects
                (pj_name, pj_active, pj_default_user, pj_default, pj_auto_assign_default_user, pj_auto_subscribe_default_user,
                pj_enable_pop3,
                pj_pop3_username,
                pj_pop3_password,
                pj_pop3_email_from,
                pj_description,
                pj_enable_custom_dropdown1,
                pj_enable_custom_dropdown2,
                pj_enable_custom_dropdown3,
                pj_custom_dropdown_label1,
                pj_custom_dropdown_label2,
                pj_custom_dropdown_label3,
                pj_custom_dropdown_values1,
                pj_custom_dropdown_values2,
                pj_custom_dropdown_values3)
                values (N'$name', $active, $defaultuser, $defaultsel, $autoasg, $autosub,
                $enablepop, N'$popuser',N'$poppass',N'$popfrom',
                N'$desc', 
                $ecd1,$ecd2,$ecd3,
                N'$cdl1',N'$cdl2',N'$cdl3',
                N'$cdv1',N'$cdv2',N'$cdv3')"
                .Replace("$poppass", model.Pop3Password);

            sql = sql.Replace("$name", model.Name);
            sql = sql.Replace("$active", Util.BoolToString(model.Active));
            sql = sql.Replace("$defaultuser", model.DefaultUser.ToString());
            sql = sql.Replace("$autoasg", Util.BoolToString(model.AutoAssign));
            sql = sql.Replace("$autosub", Util.BoolToString(model.AutoSubscribe));
            sql = sql.Replace("$defaultsel", Util.BoolToString(model.Default));
            sql = sql.Replace("$enablepop", Util.BoolToString(model.EnablePop3));
            sql = sql.Replace("$popuser", model.Pop3Login);
            sql = sql.Replace("$popfrom", model.Pop3Email);

            sql = sql.Replace("$desc", model.Description);

            sql = sql.Replace("$ecd1", Util.BoolToString(model.EnableCustomDropdown1));
            sql = sql.Replace("$ecd2", Util.BoolToString(model.EnableCustomDropdown2));
            sql = sql.Replace("$ecd3", Util.BoolToString(model.EnableCustomDropdown3));

            sql = sql.Replace("$cdl1", model.CustomDropdown1Label);
            sql = sql.Replace("$cdl2", model.CustomDropdown2Label);
            sql = sql.Replace("$cdl3", model.CustomDropdown3Label);

            sql = sql.Replace("$cdv1", model.CustomDropdown1Values);
            sql = sql.Replace("$cdv2", model.CustomDropdown2Values);
            sql = sql.Replace("$cdv3", model.CustomDropdown3Values);

            DbUtil.ExecuteNonQuery(sql);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Update(int id)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            // Get this entry's data from the db and fill in the form
            var sql = @"select
                pj_name,
                pj_active,
                isnull(pj_default_user,0) [pj_default_user],
                pj_default,
                isnull(pj_auto_assign_default_user,0) [pj_auto_assign_default_user],
                isnull(pj_auto_subscribe_default_user,0) [pj_auto_subscribe_default_user],
                isnull(pj_enable_pop3,0) [pj_enable_pop3],
                isnull(pj_pop3_username,'') [pj_pop3_username],
                isnull(pj_pop3_email_from,'') [pj_pop3_email_from],
                isnull(pj_description,'') [pj_description],
                isnull(pj_enable_custom_dropdown1,0) [pj_enable_custom_dropdown1],
                isnull(pj_enable_custom_dropdown2,0) [pj_enable_custom_dropdown2],
                isnull(pj_enable_custom_dropdown3,0) [pj_enable_custom_dropdown3],
                isnull(pj_custom_dropdown_label1,'') [pj_custom_dropdown_label1],
                isnull(pj_custom_dropdown_label2,'') [pj_custom_dropdown_label2],
                isnull(pj_custom_dropdown_label3,'') [pj_custom_dropdown_label3],
                isnull(pj_custom_dropdown_values1,'') [pj_custom_dropdown_values1],
                isnull(pj_custom_dropdown_values2,'') [pj_custom_dropdown_values2],
                isnull(pj_custom_dropdown_values3,'') [pj_custom_dropdown_values3]
                from projects
                where pj_id = $1"
                .Replace("$1", id.ToString());

            var dr = DbUtil.GetDataRow(sql);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - update project",
                SelectedItem = MainMenuSections.Administration
            };

            var users = DbUtil.GetDataView("select us_id, us_username from users order by us_username");

            ViewBag.DefaultUsers = new List<SelectListItem>();

            foreach (DataRowView user in users)
            {
                ViewBag.DefaultUsers.Add(new SelectListItem
                {
                    Value = ((int)user["us_id"]).ToString(),
                    Text = (string)user["us_username"],
                });
            }

            var model = new EditModel
            {
                Id = id,
                Name = (string)dr["pj_name"],
                Active = Convert.ToBoolean((int)dr["pj_active"]),
                Default = Convert.ToBoolean((int)dr["pj_default"]),
                DefaultUser = (int)dr["pj_default_user"],

                AutoAssign = Convert.ToBoolean((int)dr["pj_auto_assign_default_user"]),
                AutoSubscribe = Convert.ToBoolean((int)dr["pj_auto_subscribe_default_user"]),

                EnablePop3 = Convert.ToBoolean((int)dr["pj_enable_pop3"]),
                Pop3Login = (string)dr["pj_pop3_username"],
                Pop3Email = (string)dr["pj_pop3_email_from"],

                Description = (string)dr["pj_description"],

                EnableCustomDropdown1 = Convert.ToBoolean((int)dr["pj_enable_custom_dropdown1"]),
                CustomDropdown1Label = (string)dr["pj_custom_dropdown_label1"],
                CustomDropdown1Values = (string)dr["pj_custom_dropdown_values1"],

                EnableCustomDropdown2 = Convert.ToBoolean((int)dr["pj_enable_custom_dropdown2"]),
                CustomDropdown2Label = (string)dr["pj_custom_dropdown_label2"],
                CustomDropdown2Values = (string)dr["pj_custom_dropdown_values2"],

                EnableCustomDropdown3 = Convert.ToBoolean((int)dr["pj_enable_custom_dropdown3"]),
                CustomDropdown3Label = (string)dr["pj_custom_dropdown_label3"],
                CustomDropdown3Values = (string)dr["pj_custom_dropdown_values3"],
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(EditModel model)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            var valsErrorString = Util.ValidateDropdownValues(model.CustomDropdown1Values);

            if (!string.IsNullOrEmpty(valsErrorString))
            {
                ModelState.AddModelError("CustomDropdown1Values", valsErrorString);
            }

            valsErrorString = Util.ValidateDropdownValues(model.CustomDropdown2Values);

            if (!string.IsNullOrEmpty(valsErrorString))
            {
                ModelState.AddModelError("CustomDropdown2Values", valsErrorString);
            }

            valsErrorString = Util.ValidateDropdownValues(model.CustomDropdown3Values);

            if (!string.IsNullOrEmpty(valsErrorString))
            {
                ModelState.AddModelError("CustomDropdown3Values", valsErrorString);
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("Message", "Custom fields have errors.");

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - create project",
                    SelectedItem = MainMenuSections.Administration
                };

                var users = DbUtil.GetDataView("select us_id, us_username from users order by us_username");

                ViewBag.DefaultUsers = new List<SelectListItem>();

                foreach (DataRowView user in users)
                {
                    ViewBag.DefaultUsers.Add(new SelectListItem
                    {
                        Value = ((int)user["us_id"]).ToString(),
                        Text = (string)user["us_username"],
                    });
                }

                return View("Edit", model);
            }

            var sql = @"update projects set
                pj_name = N'$name',
                $POP3_PASSWORD
                pj_active = $active,
                pj_default_user = $defaultuser,
                pj_default = $defaultsel,
                pj_auto_assign_default_user = $autoasg,
                pj_auto_subscribe_default_user = $autosub,
                pj_enable_pop3 = $enablepop,
                pj_pop3_username = N'$popuser',
                pj_pop3_email_from = N'$popfrom',
                pj_description = N'$desc',
                pj_enable_custom_dropdown1 = $ecd1,
                pj_enable_custom_dropdown2 = $ecd2,
                pj_enable_custom_dropdown3 = $ecd3,
                pj_custom_dropdown_label1 = N'$cdl1',
                pj_custom_dropdown_label2 = N'$cdl2',
                pj_custom_dropdown_label3 = N'$cdl3',
                pj_custom_dropdown_values1 = N'$cdv1',
                pj_custom_dropdown_values2 = N'$cdv2',
                pj_custom_dropdown_values3 = N'$cdv3'
                where pj_id = $id"
                .Replace("$id", Convert.ToString(model.Id));

            if (model.Pop3Password != string.Empty)
            {
                sql = sql.Replace("$POP3_PASSWORD", "pj_pop3_password = N'" + model.Pop3Password + "',");
            }
            else
            {
                sql = sql.Replace("$POP3_PASSWORD", string.Empty);
            }

            sql = sql.Replace("$name", model.Name);
            sql = sql.Replace("$active", Util.BoolToString(model.Active));
            sql = sql.Replace("$defaultuser", model.DefaultUser.ToString());
            sql = sql.Replace("$autoasg", Util.BoolToString(model.AutoAssign));
            sql = sql.Replace("$autosub", Util.BoolToString(model.AutoSubscribe));
            sql = sql.Replace("$defaultsel", Util.BoolToString(model.Default));
            sql = sql.Replace("$enablepop", Util.BoolToString(model.EnablePop3));
            sql = sql.Replace("$popuser", model.Pop3Login);
            sql = sql.Replace("$popfrom", model.Pop3Email);

            sql = sql.Replace("$desc", model.Description);

            sql = sql.Replace("$ecd1", Util.BoolToString(model.EnableCustomDropdown1));
            sql = sql.Replace("$ecd2", Util.BoolToString(model.EnableCustomDropdown2));
            sql = sql.Replace("$ecd3", Util.BoolToString(model.EnableCustomDropdown3));

            sql = sql.Replace("$cdl1", model.CustomDropdown1Label);
            sql = sql.Replace("$cdl2", model.CustomDropdown2Label);
            sql = sql.Replace("$cdl3", model.CustomDropdown3Label);

            sql = sql.Replace("$cdv1", model.CustomDropdown1Values);
            sql = sql.Replace("$cdv2", model.CustomDropdown2Values);
            sql = sql.Replace("$cdv3", model.CustomDropdown3Values);

            DbUtil.ExecuteNonQuery(sql);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult UpdateUserPermission(int id, bool projects = false)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            var sql = @"Select us_username, us_id, isnull(pu_permission_level,$dpl) [pu_permission_level]
                from users
                left outer join project_user_xref on pu_user = us_id
                and pu_project = $pj
                order by us_username;
                select pj_name from projects where pj_id = $pj;"
                .Replace("$pj", id.ToString())
                .Replace("$dpl", this.applicationSettings.DefaultPermissionLevel.ToString());

            ViewBag.DataSet = DbUtil.GetDataSet(sql);

            ViewBag.Caption = "Permissions for " + (string)ViewBag.DataSet.Tables[1].Rows[0][0];

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit project per-user permissions",
                SelectedItem = MainMenuSections.Administration
            };

            var modle = new UpdateUserPermissionModel
            {
                Id = id,
                ToProjects = projects
            };

            return View(modle);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateUserPermission(UpdateUserPermissionModel model)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            // now update all the recs
            var sqlBatch = string.Empty;
            //RadioButton rb;
            //string permissionLevel;

            foreach (var permission in model.Permission)
            {
                var sq = @" if exists (select * from project_user_xref where pu_user = $us and pu_project = $pj)
                    update project_user_xref set pu_permission_level = $pu
                    where pu_user = $us and pu_project = $pj
                 else
                    insert into project_user_xref (pu_user, pu_project, pu_permission_level)
                    values ($us, $pj, $pu); ";

                sq = sq.Replace("$pj", model.Id.ToString());
                sq = sq.Replace("$us", Util.SanitizeInteger(permission.Key)/*Convert.ToString(dgi.Cells[1].Text)*/);

                //rb = (RadioButton)dgi.FindControl("none");
                //if (rb.Checked)
                //{
                //    permissionLevel = "0";
                //}
                //else
                //{
                //    rb = (RadioButton)dgi.FindControl("readonly");
                //    if (rb.Checked)
                //    {
                //        permissionLevel = "1";
                //    }
                //    else
                //    {
                //        rb = (RadioButton)dgi.FindControl("reporter");
                //        if (rb.Checked)
                //            permissionLevel = "3";
                //        else
                //            permissionLevel = "2";
                //    }
                //}

                sq = sq.Replace("$pu", permission.Value[0]);

                // add to the batch
                sqlBatch += sq;
            }

            DbUtil.ExecuteNonQuery(sqlBatch);

            ModelState.AddModelError("Message", "Permissions have been updated.");

            var sql = @"Select us_username, us_id, isnull(pu_permission_level,$dpl) [pu_permission_level]
                from users
                left outer join project_user_xref on pu_user = us_id
                and pu_project = $pj
                order by us_username;
                select pj_name from projects where pj_id = $pj;"
                .Replace("$pj", model.Id.ToString())
                .Replace("$dpl", this.applicationSettings.DefaultPermissionLevel.ToString());

            ViewBag.DataSet = DbUtil.GetDataSet(sql);

            ViewBag.Caption = "Permissions for " + (string)ViewBag.DataSet.Tables[1].Rows[0][0];

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit project per-user permissions",
                SelectedItem = MainMenuSections.Administration
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult Delete(int id)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            var sql = @"declare @cnt int
                select @cnt = count(1) from bugs where bg_project = $1
                select pj_name, @cnt [cnt] from projects where pj_id = $1"
                .Replace("$1", id.ToString());

            var dr = DbUtil.GetDataRow(sql);

            if ((int)dr["cnt"] > 0)
            {
                return Content($"You can't delete project \"{dr["pj_name"]}\" because some bugs still reference it.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete project",
                SelectedItem = MainMenuSections.Administration
            };

            var model = new DeleteModel
            {
                Id = id,
                Name = (string)dr["pj_name"]
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(DeleteModel model)
        {
            Util.DoNotCache(System.Web.HttpContext.Current.Response);

            this.security.CheckSecurity(SecurityLevel.MustBeAdmin);

            var sql = @"delete projects where pj_id = $1"
                .Replace("$1", model.Id.ToString());

            DbUtil.ExecuteNonQuery(sql);

            return RedirectToAction(nameof(Index));
        }
    }
}