/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Controllers
{
    using anmar.SharpMimeTools;
    using BugTracker.Changing.Results;
    using Core;
    using Core.Controls;
    using Models;
    using Models.Bug;
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Linq;
    using System.Net.Mail;
    using System.Runtime.Caching;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.Mvc;
    using System.Web.UI;

    [Authorize]
    [OutputCache(Location = OutputCacheLocation.None, NoStore = true)]
    public class BugController : Controller
    {
        private readonly IApplicationFacade applicationFacade;
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;
        private readonly IAuthenticate authenticate;

        public BugController(
            IApplicationFacade applicationFacade,
            IApplicationSettings applicationSettings,
            ISecurity security,
            IAuthenticate authenticate)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
            this.authenticate = authenticate;
        }

        [HttpGet]
        public ActionResult Index()
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - {this.applicationSettings.PluralBugLabel}",
                SelectedItem = this.applicationSettings.PluralBugLabel
            };

            LoadQueryDropdown();

            var model = (IndexModel)TempData["IndexModel"] ?? new IndexModel
            {
                QueryId = 0,
                Action = string.Empty,
                NewPage = 0,
                Filter = string.Empty,
                Sort = -1,
                PrevSort = -1,
                PrevDir = "ASC"
            };

            ViewBag.PostBack = false;

            if (!string.IsNullOrEmpty(model.Action) && model.Action != "query")
            {
                // sorting, paging, filtering, so don't go back to the database
                ViewBag.DataView = (DataView)Session["bugs"];

                if (ViewBag.DataView == null)
                {
                    DoQuery(model);
                }
                else if (model.Action == "sort")
                {
                    model.NewPage = 0;
                }

                ViewBag.PostBack = true;
            }
            else
            {
                if (Session["just_did_text_search"] == null)
                {
                    var message = DoQuery(model);

                    if (!string.IsNullOrEmpty(message))
                    {
                        return Content(message);
                    }
                }
                // from search page
                else
                {
                    Session["just_did_text_search"] = null;

                    ViewBag.DataView = (DataView)Session["bugs"];
                }
            }

            CallSortAndFilterBuglistDataview(model, ViewBag.PostBack);

            model.Action = string.Empty;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(IndexModel model)
        {
            // posting back a query change?
            // posting back a filter change?
            // posting back a sort change?

            if (model.Action == "query")
            {
                TempData["IndexModel"] = new IndexModel
                {
                    QueryId = model.QueryId,
                    Action = model.Action,
                    NewPage = 0,
                    Filter = string.Empty,
                    Sort = -1,
                    PrevSort = -1,
                    PrevDir = "ASC"
                };
            }
            else
            {
                TempData["IndexModel"] = model;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public ActionResult Create()
        {
            if (this.security.User.AddsNotAllowed)
            {
                // TODO No access
                throw new InvalidOperationException();
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - new bug",
                SelectedItem = ApplicationSettings.PluralBugLabelDefault
            };

            LoadDropdowns();
            LoadUserDropdown();

            ViewBag.CustomColumns = Util.GetCustomColumns();
            ViewBag.PermissionLevel = SecurityPermissionLevel.PermissionNone;

            // Prepare for custom columns
            var hashCustomColumns = new SortedDictionary<string, string>();

            foreach (DataRow drcc in ViewBag.CustomColumns.Tables[0].Rows)
            {
                var columnName = (string)drcc["name"];

                if (this.security.User.DictCustomFieldPermissionLevel[columnName] != SecurityPermissionLevel.PermissionNone)
                {
                    var defaultval = GetCustomColDefaultValue(drcc["default value"]);

                    hashCustomColumns.Add(columnName, defaultval);
                }
            }

            // We don't know the project yet, so all permissions
            //set_controls_field_permission(SecurityPermissionLevel.PermissionAll, security);

            if (TempData["Errors"] is IReadOnlyCollection<IFailError> failErrors)
                foreach (var failError in failErrors)
                    ModelState.AddModelError(failError.Property, failError.Message);

            if (TempData["Errors2"] is Dictionary<string, ModelErrorCollection> failErrors2)
                foreach (var failError in failErrors2)
                    foreach (var err in failError.Value)
                        ModelState.AddModelError(failError.Key, err.ErrorMessage);

            if (TempData["Model"] is EditModel model) return View("Edit", model);

            var defaultProjectId = GetDefaultProject();

            model = new EditModel
            {
                ProjectId = defaultProjectId,
                OrganizationId = GetDefaultOrganization(),
                CategoryId = GetDefaultCategory(),
                PriorityId = GetDefaultPriority(),
                StatusId = GetDefaultStatus(),
                UserDefinedAttributeId = GetDefaultUserDefinedAttribute(),
                UserId = Util.GetDefaultUser(defaultProjectId/*Convert.ToInt32(this.project.SelectedItem.Value)*/)
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EditModel model)
        {
            GetCookieValuesForShowHideToggles();

            //var drBug = Bug.GetBugDataRow(this.Id, Security, this.DsCustomCols);

            //load_incoming_custom_col_vals_into_hash(Security);

            // Fetch the values of the custom columns from the Request and stash them in a hash table.

            var dsCustomColumns = Util.GetCustomColumns();
            var hashCustomColumns = new SortedDictionary<string, string>();

            foreach (DataRow drcc in dsCustomColumns.Tables[0].Rows)
            {
                var columnName = (string)drcc["name"];

                if (this.security.User.DictCustomFieldPermissionLevel[columnName] != SecurityPermissionLevel.PermissionNone)
                {
                    hashCustomColumns.Add(columnName, Request[columnName]);
                }
            }

            //if (did_user_hit_submit_button()) // or is this a project dropdown autopostback?
            //{
            //    //this.Good = validate(Security);

            //    if (this.Good)
            //    {
            //        // Actually do the update
            //        if (this.Id == 0)
            //            do_insert(Security);
            //        else
            //            do_update(Security);
            //    }
            //    else // bad, invalid
            //    {
            //        // Say we didn't do anything.
            //        if (this.Id == 0)
            //            set_msg(Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel) + " was not created.");
            //        else
            //            set_msg(Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel) + " was not updated.");
            //        load_user_dropdown(Security);
            //    }
            //}

            //Validate

            //TODO
            //if (!did_something_change()) return false;

            // validate custom columns
            foreach (DataRow drcc in dsCustomColumns.Tables[0].Rows)
            {
                var name = (string)drcc["name"];

                if (this.security.User.DictCustomFieldPermissionLevel[name] != SecurityPermissionLevel.PermissionAll) continue;

                var val = Request[name];

                if (val == null) continue;

                val = val.Replace("'", "''");

                // if a date was entered, convert to db format
                if (val.Length > 0)
                {
                    var datatype = drcc["datatype"].ToString();

                    if (datatype == "datetime")
                    {
                        try
                        {
                            DateTime.Parse(val, Util.GetCultureInfo());
                        }
                        catch (FormatException)
                        {
                            ModelState.AddModelError(name, "\"" + name + "\" not in a valid date format.");
                        }
                    }
                    else if (datatype == "int")
                    {
                        if (!Util.IsInt(val))
                        {
                            ModelState.AddModelError(name, "\"" + name + "\" must be an integer.<br>");
                        }
                    }
                    else if (datatype == "decimal")
                    {
                        var xprec = Convert.ToInt32(drcc["xprec"]);
                        var xscale = Convert.ToInt32(drcc["xscale"]);
                        var decimalError = Util.IsValidDecimal(name, val, xprec - xscale, xscale);

                        if (!string.IsNullOrEmpty(decimalError))
                        {
                            ModelState.AddModelError(name, decimalError);
                        }
                    }
                }
                else
                {
                    var nullable = (int)drcc["isnullable"];

                    if (nullable == 0)
                    {
                        ModelState.AddModelError(name, "\"" + name + "\" is required.");
                    }
                }
            }

            // validate assigned to user versus 
            if (!DoesAssignedToHavePermissionForOrg(model.UserId, model.OrganizationId))
            {
                ModelState.AddModelError(nameof(EditModel.UserId), "User does not have permission for the Organization");
            }

            // custom validations go here
            //if (!Workflow.CustomValidations(this.DrBug, security.User,
            //    this, this.custom_validation_err_msg))
            //good = false;

            //Validate

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, Util.CapitalizeFirstLetter(this.applicationSettings.SingularBugLabel) + " was not created.");

                TempData["Model"] = model;
                TempData["Errors2"] = ModelState.Where(x => x.Value.Errors.Any())
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToDictionary(x => x.Key, x => x.Errors);

                return RedirectToAction(nameof(Create));
            }

            // Do insert
            //get_comment_text_from_control(security);
            var commentFormated = string.Empty;
            var commentSearch = string.Empty;
            var commentType = string.Empty;

            if (this.security.User.UseFckeditor)
            {
                commentFormated = Util.StripDangerousTags(model.Comment);
                commentSearch = Util.StripHtml(model.Comment);
                commentType = "text/html";
            }
            else
            {
                commentFormated = HttpUtility.HtmlDecode(model.Comment);
                commentSearch = commentFormated;
                commentType = "text/plain";
            }

            // Project specific
            var pcd1 = Request["pcd1"];
            var pcd2 = Request["pcd2"];
            var pcd3 = Request["pcd3"];

            if (pcd1 == null) pcd1 = string.Empty;
            if (pcd2 == null) pcd2 = string.Empty;
            if (pcd3 == null) pcd3 = string.Empty;

            pcd1 = pcd1.Replace("'", "''");
            pcd2 = pcd2.Replace("'", "''");
            pcd3 = pcd3.Replace("'", "''");

            var newIds = Bug.InsertBug(model.Name, this.security, /*this.tags.Value*/string.Empty, // TODO Tags
                model.ProjectId,
                model.OrganizationId,
                model.CategoryId,
                model.PriorityId,
                model.StatusId,
                model.UserId,
                model.UserDefinedAttributeId,
                pcd1,
                pcd2,
                pcd3, commentFormated, commentSearch,
                null, // from
                null, // cc
                commentType, /*this.internal_only.Checked*/false /*TODO*/ , hashCustomColumns,
                true); // send notifications

            // TODO Tags
            //if (!string.IsNullOrEmpty(this.tags.Value) && this.applicationSettings.EnableTags)
            //{
            //    Tags.BuildTagIndex(HttpContext.ApplicationInstance.Application);
            //}

            //this.Id = newIds.Bugid;

            WhatsNew.AddNews(newIds.Bugid, model.Name, "added", security);

            //this.new_id.Value = Convert.ToString(this.Id);
            //TODO
            //set_msg(Util.CapitalizeFirstLetter(thie.applicationSettings.SingularBugLabel) + " was created.");

            // save for next bug
            Session["project"] = model.ProjectId;

            //Response.Redirect($"~/Bugs/Edit.aspx?id={this.Id}");

            return RedirectToAction(nameof(Update), new { id = newIds.Bugid });
        }

        [HttpGet]
        public ActionResult Update(int id)
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - edit bug",
                SelectedItem = ApplicationSettings.PluralBugLabelDefault
            };

            LoadDropdowns();
            LoadUserDropdown();

            ViewBag.CustomColumns = Util.GetCustomColumns();
            //ViewBag.PermissionLevel = SecurityPermissionLevel.PermissionNone;

            if (TempData["Errors"] is IReadOnlyCollection<IFailError> failErrors)
                foreach (var failError in failErrors)
                    ModelState.AddModelError(failError.Property, failError.Message);

            if (TempData["Model"] is EditModel model) return View("Edit", model);

            GetCookieValuesForShowHideToggles();

            // Get this entry's data from the db and fill in the form
            var drBug = Bug.GetBugDataRow(id, this.security, ViewBag.CustomColumns);

            //prepare_for_update(Security);
            //if (this.DrBug == null)
            //{
            //    this.mainBlock.Visible = false;
            //    this.errorBlock.Visible = true;
            //    this.errorBlockPermissions.Visible = false;
            //    this.errorBlockIntegerId.Visible = false;

            //    return;
            //}

            // look at permission level and react accordingly
            ViewBag.PermissionLevel = (SecurityPermissionLevel)(int)drBug["pu_permission_level"];

            //if (PermissionLevel == SecurityPermissionLevel.PermissionNone)
            //{
            //    this.mainBlock.Visible = false;
            //    this.errorBlock.Visible = false;
            //    this.errorBlockPermissions.Visible = true;
            //    this.errorBlockIntegerId.Visible = false;

            //    return;
            //}

            var hashCustomColumns = new SortedDictionary<string, string>();

            foreach (DataRow drcc in ViewBag.CustomColumns.Tables[0].Rows)
            {
                var columnName = (string)drcc["name"];

                if (this.security.User.DictCustomFieldPermissionLevel[columnName] != SecurityPermissionLevel.PermissionNone)
                {
                    var val = Util.FormatDbValue(drBug[columnName]);

                    hashCustomColumns.Add(columnName, val);
                }
            }

            // move stuff to the page

            //this.bugid.InnerText = Convert.ToString((int)this.DrBug["id"]);

            // Fill in this form
            //this.short_desc.Value = (string)this.DrBug["short_desc"];
            //this.tags.Value = (string)this.DrBug["bg_tags"];
            //Page.Title = Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel)
            //             + " ID " + Convert.ToString(this.DrBug["id"]) + " " + (string)this.DrBug["short_desc"];
            ViewBag.Page.Title = Util.CapitalizeFirstLetter(this.applicationSettings.SingularBugLabel)
                         + " ID " + Convert.ToString(drBug["id"]) + " " + (string)drBug["short_desc"];

            // reported by
            string s;
            s = "Created by ";
            s += PrintBug.FormatEmailUserName(
                true,
                Convert.ToInt32(drBug["id"]), ViewBag.PermissionLevel,
                Convert.ToString(drBug["reporter_email"]),
                Convert.ToString(drBug["reporter"]),
                Convert.ToString(drBug["reporter_fullname"]));
            s += " on ";
            s += Util.FormatDbDateTime(drBug["reported_date"]);
            s += ", ";
            s += Util.HowLongAgo((int)drBug["seconds_ago"]);

            ViewBag.ReportedBy = s;

            // save current values in previous, so that later we can write the audit trail when things change
            //this.prev_short_desc.Value = (string)this.DrBug["short_desc"];
            //this.prev_tags.Value = (string)this.DrBug["bg_tags"];
            //this.prev_project.Value = Convert.ToString((int)this.DrBug["project"]);
            //this.prev_project_name.Value = Convert.ToString(this.DrBug["current_project"]);
            //this.prev_org.Value = Convert.ToString((int)this.DrBug["organization"]);
            //this.prev_org_name.Value = Convert.ToString(this.DrBug["og_name"]);
            //this.prev_category.Value = Convert.ToString((int)this.DrBug["category"]);
            //this.prev_priority.Value = Convert.ToString((int)this.DrBug["priority"]);
            //this.prev_assigned_to.Value = Convert.ToString((int)this.DrBug["assigned_to_user"]);
            //this.prev_assigned_to_username.Value = Convert.ToString(this.DrBug["assigned_to_username"]);
            //this.prev_status.Value = Convert.ToString((int)this.DrBug["status"]);
            //this.prev_udf.Value = Convert.ToString((int)this.DrBug["udf"]);
            //this.prev_pcd1.Value = (string)this.DrBug["bg_project_custom_dropdown_value1"];
            //this.prev_pcd2.Value = (string)this.DrBug["bg_project_custom_dropdown_value2"];
            //this.prev_pcd3.Value = (string)this.DrBug["bg_project_custom_dropdown_value3"];

            //TODO for dropdowns
            // special logic for org
            //if (this.Id != 0)
            //{
            //    // Org
            //    if (this.prev_org.Value != "0")
            //    {
            //        var alreadyInDropdown = false;
            //        foreach (ListItem li in this.org.Items)
            //            if (li.Value == this.prev_org.Value)
            //            {
            //                alreadyInDropdown = true;
            //                break;
            //            }

            //        // Add to the list, even if permissions don't allow it now, because, in the past, they did allow it.
            //        if (!alreadyInDropdown)
            //            this.org.Items.Add(
            //                new ListItem(this.prev_org_name.Value, this.prev_org.Value));
            //    }

            //    foreach (ListItem li in this.org.Items)
            //        if (li.Value == this.prev_org.Value)
            //            li.Selected = true;
            //        else
            //            li.Selected = false;
            //}

            //load_project_and_user_dropdown_for_update(security); // must come before set_controls_field_permission, after assigning to prev_ values

            //set_controls_field_permission(PermissionLevel, security);

            //this.snapshot_timestamp.Value = Convert.ToDateTime(this.DrBug["snapshot_timestamp"])
            //    .ToString("yyyyMMdd HH\\:mm\\:ss\\:fff");

            //prepare_a_bunch_of_links_for_update(security);

            FormatPrevNextBug(id);

            // save for next bug
            if (/*this.project.SelectedItem != null*/ (int)drBug["project"] != 0)
            {
                Session["project"] = (int)drBug["project"];/*this.project.SelectedItem.Value*/
            }

            //// Execute code not written by me
            //Workflow.CustomAdjustControls(this.DrBug, security.User, this);

            model = new EditModel
            {
                Id = drBug["id"],
                Name = drBug["short_desc"],
                ProjectId = (int)drBug["project"],
                OrganizationId = (int)drBug["organization"],
                CategoryId = (int)drBug["category"],
                PriorityId = (int)drBug["priority"],
                StatusId = (int)drBug["status"],
                UserDefinedAttributeId = (int)drBug["udf"],
                //UserId = Util.GetDefaultUser(defaultProjectId/*Convert.ToInt32(this.project.SelectedItem.Value)*/)
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Update(EditModel model)
        {
            GetCookieValuesForShowHideToggles();

            //var drBug = Bug.GetBugDataRow(this.Id, Security, this.DsCustomCols);

            //load_incoming_custom_col_vals_into_hash(Security);

            // Fetch the values of the custom columns from the Request and stash them in a hash table.

            var dsCustomColumns = Util.GetCustomColumns();
            var hashCustomColumns = new SortedDictionary<string, string>();

            foreach (DataRow drcc in dsCustomColumns.Tables[0].Rows)
            {
                var columnName = (string)drcc["name"];

                if (this.security.User.DictCustomFieldPermissionLevel[columnName] != SecurityPermissionLevel.PermissionNone)
                {
                    hashCustomColumns.Add(columnName, Request[columnName]);
                }
            }

            //if (did_user_hit_submit_button()) // or is this a project dropdown autopostback?
            //{
            //    //this.Good = validate(Security);

            //    if (this.Good)
            //    {
            //        // Actually do the update
            //        if (this.Id == 0)
            //            do_insert(Security);
            //        else
            //            do_update(Security);
            //    }
            //    else // bad, invalid
            //    {
            //        // Say we didn't do anything.
            //        if (this.Id == 0)
            //            set_msg(Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel) + " was not created.");
            //        else
            //            set_msg(Util.CapitalizeFirstLetter(ApplicationSettings.SingularBugLabel) + " was not updated.");
            //        load_user_dropdown(Security);
            //    }
            //}

            //Validate

            //TODO
            //if (!did_something_change()) return false;

            // validate custom columns
            foreach (DataRow drcc in dsCustomColumns.Tables[0].Rows)
            {
                var name = (string)drcc["name"];

                if (this.security.User.DictCustomFieldPermissionLevel[name] != SecurityPermissionLevel.PermissionAll) continue;

                var val = Request[name];

                if (val == null) continue;

                val = val.Replace("'", "''");

                // if a date was entered, convert to db format
                if (val.Length > 0)
                {
                    var datatype = drcc["datatype"].ToString();

                    if (datatype == "datetime")
                    {
                        try
                        {
                            DateTime.Parse(val, Util.GetCultureInfo());
                        }
                        catch (FormatException)
                        {
                            ModelState.AddModelError(name, "\"" + name + "\" not in a valid date format.");
                        }
                    }
                    else if (datatype == "int")
                    {
                        if (!Util.IsInt(val))
                        {
                            ModelState.AddModelError(name, "\"" + name + "\" must be an integer.<br>");
                        }
                    }
                    else if (datatype == "decimal")
                    {
                        var xprec = Convert.ToInt32(drcc["xprec"]);
                        var xscale = Convert.ToInt32(drcc["xscale"]);
                        var decimalError = Util.IsValidDecimal(name, val, xprec - xscale, xscale);

                        if (!string.IsNullOrEmpty(decimalError))
                        {
                            ModelState.AddModelError(name, decimalError);
                        }
                    }
                }
                else
                {
                    var nullable = (int)drcc["isnullable"];

                    if (nullable == 0)
                    {
                        ModelState.AddModelError(name, "\"" + name + "\" is required.");
                    }
                }
            }

            // validate assigned to user versus 
            if (!DoesAssignedToHavePermissionForOrg(model.UserId, model.OrganizationId))
            {
                ModelState.AddModelError(nameof(EditModel.UserId), "User does not have permission for the Organization");
            }

            // custom validations go here
            //if (!Workflow.CustomValidations(this.DrBug, security.User,
            //    this, this.custom_validation_err_msg))
            //good = false;

            //Validate

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, Util.CapitalizeFirstLetter(this.applicationSettings.SingularBugLabel) + " was not updated.");

                TempData["Model"] = model;
                TempData["Errors2"] = ModelState.Where(x => x.Value.Errors.Any())
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToDictionary(x => x.Key, x => x.Errors);

                return RedirectToAction(nameof(Create));
            }

            // Do update
            //TODO
            //do_update(Security);


            ////get_comment_text_from_control(security);
            //var commentFormated = string.Empty;
            //var commentSearch = string.Empty;
            //var commentType = string.Empty;

            //if (this.security.User.UseFckeditor)
            //{
            //    commentFormated = Util.StripDangerousTags(model.Comment);
            //    commentSearch = Util.StripHtml(model.Comment);
            //    commentType = "text/html";
            //}
            //else
            //{
            //    commentFormated = HttpUtility.HtmlDecode(model.Comment);
            //    commentSearch = commentFormated;
            //    commentType = "text/plain";
            //}

            //// Project specific
            //var pcd1 = Request["pcd1"];
            //var pcd2 = Request["pcd2"];
            //var pcd3 = Request["pcd3"];

            //if (pcd1 == null) pcd1 = string.Empty;
            //if (pcd2 == null) pcd2 = string.Empty;
            //if (pcd3 == null) pcd3 = string.Empty;

            //pcd1 = pcd1.Replace("'", "''");
            //pcd2 = pcd2.Replace("'", "''");
            //pcd3 = pcd3.Replace("'", "''");

            //var newIds = Bug.InsertBug(model.Name, this.security, /*this.tags.Value*/string.Empty, // TODO Tags
            //    model.ProjectId,
            //    model.OrganizationId,
            //    model.CategoryId,
            //    model.PriorityId,
            //    model.StatusId,
            //    model.UserId,
            //    model.UserDefinedAttributeId,
            //    pcd1,
            //    pcd2,
            //    pcd3, commentFormated, commentSearch,
            //    null, // from
            //    null, // cc
            //    commentType, /*this.internal_only.Checked*/false /*TODO*/ , hashCustomColumns,
            //    true); // send notifications

            //// TODO Tags
            ////if (!string.IsNullOrEmpty(this.tags.Value) && this.applicationSettings.EnableTags)
            ////{
            ////    Tags.BuildTagIndex(HttpContext.ApplicationInstance.Application);
            ////}

            ////this.Id = newIds.Bugid;

            //WhatsNew.AddNews(newIds.Bugid, model.Name, "added", security);

            ////this.new_id.Value = Convert.ToString(this.Id);
            ////TODO
            ////set_msg(Util.CapitalizeFirstLetter(thie.applicationSettings.SingularBugLabel) + " was created.");

            //// save for next bug
            //Session["project"] = model.ProjectId;

            ////Response.Redirect($"~/Bugs/Edit.aspx?id={this.Id}");

            return RedirectToAction(nameof(Update), new { id = model.Id });
        }

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Insert()
        {
            var username = Request["username"];
            var password = Request["password"];
            var projectidString = Request["projectid"];
            var comment = Request["comment"];
            var fromAddr = Request["from"];
            var cc = string.Empty;
            var message = Request["message"];
            var attachmentAsBase64 = Request["attachment"];
            var attachmentContentType = Request["attachment_content_type"];
            var attachmentFilename = Request["attachment_filename"];
            var attachmentDesc = Request["attachment_desc"];
            var bugidString = Request["bugid"];
            var shortDesc = Request["short_desc"];

            // this could also be the email subject
            if (shortDesc == null)
            {
                shortDesc = string.Empty;
            }
            else if (shortDesc.Length > 200)
            {
                shortDesc = shortDesc.Substring(0, 200);
            }

            SharpMimeMessage mimeMessage = null;

            if (message != null && message.Length > 0)
            {
                mimeMessage = MyMime.GetSharpMimeMessage(message);

                comment = MyMime.GetComment(mimeMessage);

                var headers = MyMime.GetHeadersForComment(mimeMessage);

                if (!string.IsNullOrEmpty(headers))
                {
                    comment = headers + "\n" + comment;
                }

                fromAddr = MyMime.GetFromAddr(mimeMessage);
            }
            else
            {
                if (comment == null) comment = string.Empty;
            }

            if (string.IsNullOrEmpty(username))
            {
                Response.AddHeader("BTNET", "ERROR: username required");

                return Content("ERROR: username required");
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

            var security = MyMime.GetSynthesizedSecurity(mimeMessage, fromAddr, username);

            var projectid = 0;
            if (Util.IsInt(projectidString)) projectid = Convert.ToInt32(projectidString);

            var bugid = 0;

            if (Util.IsInt(bugidString))
            {
                bugid = Convert.ToInt32(bugidString);
            }

            // Even though btnet_service.exe has already parsed out the bugid,
            // we can do a better job here with SharpMimeTools.dll
            var subject = string.Empty;

            if (mimeMessage != null)
            {
                subject = MyMime.GetSubject(mimeMessage);

                if (subject != "[No Subject]") bugid = MyMime.GetBugidFromSubject(ref subject);

                cc = MyMime.GetCc(mimeMessage);
            }

            var sql = string.Empty;

            if (bugid != 0)
            {
                // Check if the bug is still in the database
                // No comment can be added to merged or deleted bugids
                // In this case a new bug is created, this to prevent possible loss of information

                sql = @"select count(bg_id)
                    from bugs
                    where bg_id = $id";

                sql = sql.Replace("$id", Convert.ToString(bugid));

                if (Convert.ToInt32(DbUtil.ExecuteScalar(sql)) == 0)
                {
                    bugid = 0;
                }
            }

            // Either insert a new bug or append a commment to existing bug
            // based on presence, absence of bugid
            if (bugid == 0)
            {
                // insert a new bug

                if (mimeMessage != null)
                {
                    // in case somebody is replying to a bug that has been deleted or merged
                    subject = subject.Replace(this.applicationSettings.TrackingIdString, "PREVIOUS:");

                    shortDesc = subject;
                    if (shortDesc.Length > 200)
                    {
                        shortDesc = shortDesc.Substring(0, 200);
                    }
                }

                var orgid = 0;
                var categoryid = 0;
                var priorityid = 0;
                var assignedid = 0;
                var statusid = 0;
                var udfid = 0;

                // You can control some more things from the query string
                if (!string.IsNullOrEmpty(Request["$ORGANIZATION$"]))
                {
                    orgid = Convert.ToInt32(Request["$ORGANIZATION$"]);
                }

                if (!string.IsNullOrEmpty(Request["$CATEGORY$"]))
                {
                    categoryid = Convert.ToInt32(Request["$CATEGORY$"]);
                }

                if (!string.IsNullOrEmpty(Request["$PROJECT$"]))
                {
                    projectid = Convert.ToInt32(Request["$PROJECT$"]);
                }

                if (!string.IsNullOrEmpty(Request["$PRIORITY$"]))
                {
                    priorityid = Convert.ToInt32(Request["$PRIORITY$"]);
                }

                if (!string.IsNullOrEmpty(Request["$ASSIGNEDTO$"]))
                {
                    assignedid = Convert.ToInt32(Request["$ASSIGNEDTO$"]);
                }

                if (!string.IsNullOrEmpty(Request["$STATUS$"]))
                {
                    statusid = Convert.ToInt32(Request["$STATUS$"]);
                }

                if (!string.IsNullOrEmpty(Request["$UDF$"]))
                {
                    udfid = Convert.ToInt32(Request["$UDF$"]);
                }

                var defaults = Bug.GetBugDefaults();

                // If you didn't set these from the query string, we'll give them default values
                if (projectid == 0) projectid = (int)defaults["pj"];
                if (orgid == 0) orgid = security.User.Org;
                if (categoryid == 0) categoryid = (int)defaults["ct"];
                if (priorityid == 0) priorityid = (int)defaults["pr"];
                if (statusid == 0) statusid = (int)defaults["st"];
                if (udfid == 0) udfid = (int)defaults["udf"];

                // but forced project always wins
                if (security.User.ForcedProject != 0) projectid = security.User.ForcedProject;

                var newIds = Bug.InsertBug(
                    shortDesc,
                    security,
                    string.Empty, // tags
                    projectid,
                    orgid,
                    categoryid,
                    priorityid,
                    statusid,
                    assignedid,
                    udfid,
                    string.Empty, string.Empty, string.Empty, // project specific dropdown values
                    comment,
                    comment,
                    fromAddr,
                    cc,
                    "text/plain",
                    false, // internal only
                    null, // custom columns
                    false); // suppress notifications for now - wait till after the attachments

                if (mimeMessage != null)
                {
                    MyMime.AddAttachments(mimeMessage, newIds.Bugid, newIds.Postid, security);

                    MyPop3.AutoReply(newIds.Bugid, fromAddr, shortDesc, projectid);
                }
                else if (attachmentAsBase64 != null && attachmentAsBase64.Length > 0)
                {
                    if (attachmentDesc == null) attachmentDesc = string.Empty;
                    if (attachmentContentType == null) attachmentContentType = string.Empty;
                    if (attachmentFilename == null) attachmentFilename = string.Empty;

                    var byteArray = Convert.FromBase64String(attachmentAsBase64);

                    using (var stream = new MemoryStream(byteArray))
                    {
                        Bug.InsertPostAttachment(
                                security,
                                newIds.Bugid,
                                stream,
                                byteArray.Length,
                                attachmentFilename,
                                attachmentDesc,
                                attachmentContentType,
                                -1, // parent
                                false, // internal_only
                                false); // don't send notification yet
                    }
                }

                // your customizations
                Bug.ApplyPostInsertRules(newIds.Bugid);

                Bug.SendNotifications(Bug.Insert, newIds.Bugid, security);
                WhatsNew.AddNews(newIds.Bugid, shortDesc, "added", security);

                Response.AddHeader("BTNET", "OK:" + Convert.ToString(newIds.Bugid));

                return Content("OK:" + Convert.ToString(newIds.Bugid));
            }
            else // update existing bug
            {
                var statusResultingFromIncomingEmail = this.applicationSettings.StatusResultingFromIncomingEmail;

                sql = string.Empty;

                if (statusResultingFromIncomingEmail != 0)
                {
                    sql = @"update bugs
                        set bg_status = $st
                        where bg_id = $bg";

                    sql = sql.Replace("$st", statusResultingFromIncomingEmail.ToString());
                }

                sql += "select bg_short_desc from bugs where bg_id = $bg";

                sql = sql.Replace("$bg", Convert.ToString(bugid));
                var dr2 = DbUtil.GetDataRow(sql);

                // Add a comment to existing bug.
                var postid = Bug.InsertComment(
                    bugid,
                    security.User.Usid, // (int) dr["us_id"],
                    comment,
                    comment,
                    fromAddr,
                    cc,
                    "text/plain",
                    false); // internal only

                if (mimeMessage != null)
                {
                    MyMime.AddAttachments(mimeMessage, bugid, postid, security);
                }
                else if (attachmentAsBase64 != null && attachmentAsBase64.Length > 0)
                {
                    if (attachmentDesc == null) attachmentDesc = string.Empty;
                    if (attachmentContentType == null) attachmentContentType = string.Empty;
                    if (attachmentFilename == null) attachmentFilename = string.Empty;

                    var byteArray = Convert.FromBase64String(attachmentAsBase64);

                    using (var stream = new MemoryStream(byteArray))
                    {
                        Bug.InsertPostAttachment(
                            security,
                            bugid,
                            stream,
                            byteArray.Length,
                            attachmentFilename,
                            attachmentDesc,
                            attachmentContentType,
                            -1, // parent
                            false, // internal_only
                            false); // don't send notification yet
                    }
                }

                Bug.SendNotifications(Bug.Update, bugid, security);
                WhatsNew.AddNews(bugid, (string)dr2["bg_short_desc"], "updated", security);

                Response.AddHeader("BTNET", "OK:" + Convert.ToString(bugid));

                return Content("OK:" + Convert.ToString(bugid));
            }
        }

        [HttpGet]
        [Authorize(Roles = ApplicationRoles.Member)]
        public ActionResult MassEdit()
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanMassEditBugs;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - massedit",
                SelectedItem = MainMenuSections.Administration
            };

            var list = string.Empty;
            var sql = string.Empty;
            var model = new MassEditModel
            {
                Sql = sql
            };

            if (Request["mass_delete"] != null)
            {
                model.Action = "delete";
            }
            else
            {
                model.Action = "update";
            }

            // create list of bugs affected
            foreach (string var in Request.QueryString)
            {
                if (Util.IsInt(var))
                {
                    if (!string.IsNullOrEmpty(list))
                    {
                        list += ",";
                    }

                    list += var;
                }
            }

            model.BugList = list;

            if (model.Action == "delete")
            {
                sql = "delete bug_post_attachments from bug_post_attachments inner join bug_posts on bug_post_attachments.bpa_post = bug_posts.bp_id where bug_posts.bp_bug in (" + list + ")";

                sql += "\ndelete from bug_posts where bp_bug in (" + list + ")";
                sql += "\ndelete from bug_subscriptions where bs_bug in (" + list + ")";
                sql += "\ndelete from bug_relationships where re_bug1 in (" + list + ")";
                sql += "\ndelete from bug_relationships where re_bug2 in (" + list + ")";
                sql += "\ndelete from bug_user where bu_bug in (" + list + ")";
                sql += "\ndelete from bug_tasks where tsk_bug in (" + list + ")";
                sql += "\ndelete from bugs where bg_id in (" + list + ")";

                model.ButtonText = "Confirm Delete";
            }
            else
            {
                sql = "update bugs \nset ";

                var updates = string.Empty;

                string val;

                val = Request["mass_project"];

                if (val != "-1" && Util.IsInt(val))
                {
                    if (!string.IsNullOrEmpty(updates))
                    {
                        updates += ",\n";
                    }

                    updates += "bg_project = " + val;
                }

                val = Request["mass_org"];

                if (val != "-1" && Util.IsInt(val))
                {
                    if (!string.IsNullOrEmpty(updates))
                    {
                        updates += ",\n";
                    }

                    updates += "bg_org = " + val;
                }

                val = Request["mass_category"];

                if (val != "-1" && Util.IsInt(val))
                {
                    if (!string.IsNullOrEmpty(updates))
                    {
                        updates += ",\n";
                    }

                    updates += "bg_category = " + val;
                }

                val = Request["mass_priority"];

                if (val != "-1" && Util.IsInt(val))
                {
                    if (!string.IsNullOrEmpty(updates))
                    {
                        updates += ",\n";
                    }

                    updates += "bg_priority = " + val;
                }

                val = Request["mass_assigned_to"];

                if (val != "-1" && Util.IsInt(val))
                {
                    if (!string.IsNullOrEmpty(updates))
                    {
                        updates += ",\n";
                    }

                    updates += "bg_assigned_to_user = " + val;
                }

                val = Request["mass_reported_by"];

                if (val != "-1" && Util.IsInt(val))
                {
                    if (!string.IsNullOrEmpty(updates))
                    {
                        updates += ",\n";
                    }

                    updates += "bg_reported_user = " + val;
                }

                val = Request["mass_status"];

                if (val != "-1" && Util.IsInt(val))
                {
                    if (!string.IsNullOrEmpty(updates))
                    {
                        updates += ",\n";
                    }

                    updates += "bg_status = " + val;
                }

                sql += updates + "\nwhere bg_id in (" + list + ")";

                model.ButtonText = "Confirm Update";
            }

            model.Sql = sql;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ApplicationRoles.Member)]
        public ActionResult MassEdit(MassEditModel model)
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanMassEditBugs;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            if (model.Action == "delete")
            {
                var uploadFolder = Util.GetUploadFolder();

                if (uploadFolder != null)
                {
                    // double check the bug_list
                    var ints = model.BugList.Split(',');

                    for (var i = 0; i < ints.Length; i++)
                    {
                        if (!Util.IsInt(ints[i]))
                        {
                            return Content(string.Empty);
                        }
                    }

                    var sql2 = $@"select bp_bug, bp_id, bp_file from bug_posts where bp_type = 'file' and bp_bug in ({model.BugList})";
                    var ds = DbUtil.GetDataSet(sql2);

                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        // create path
                        var path = new StringBuilder(uploadFolder);

                        path.Append("\\");
                        path.Append(Convert.ToString(dr["bp_bug"]));
                        path.Append("_");
                        path.Append(Convert.ToString(dr["bp_id"]));
                        path.Append("_");
                        path.Append(Convert.ToString(dr["bp_file"]));

                        if (System.IO.File.Exists(path.ToString()))
                        {
                            System.IO.File.Delete(path.ToString());
                        }
                    }
                }
            }

            DbUtil.ExecuteNonQuery(model.Sql);

            return RedirectToAction("Index", "Search");
        }

        [HttpGet]
        [Authorize(Roles = ApplicationRoles.Member)]
        public ActionResult Delete(int id)
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanDeleteBug;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var permissionLevel = Bug.GetBugPermissionLevel(Convert.ToInt32(id), this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                return Content("You are not allowed to edit this item");
            }

            var sql = @"select bg_short_desc from bugs where bg_id = $1"
                .Replace("$1", id.ToString());

            var dr = DbUtil.GetDataRow(sql);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete {this.applicationSettings.SingularBugLabel}",
                SelectedItem = ApplicationSettings.PluralBugLabelDefault
            };

            var model = new DeleteModel
            {
                Id = id,
                Name = (string)dr["bg_short_desc"]
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ApplicationRoles.Member)]
        public ActionResult Delete(DeleteModel model)
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanDeleteBug;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            var permissionLevel = Bug.GetBugPermissionLevel(model.Id, this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                return Content("You are not allowed to edit this item");
            }

            Bug.DeleteBug(model.Id);

            return RedirectToAction(nameof(Index));
        }

        [HttpGet]
        public void Print(string format, int? queryId)
        {
            // TODO
            //if (Request["format"] != "excel")
            //{
            //    Util.DoNotCache(System.Web.HttpContext.Current.Response);
            //};

            DataView dataView;

            if (queryId.HasValue)
            {
                // use sql specified in query string
                var sql = @"select qu_sql from queries where qu_id = $1"
                    .Replace("$1", queryId.Value.ToString());

                var bugSql = (string)DbUtil.ExecuteScalar(sql);

                // replace magic variables
                bugSql = bugSql.Replace("$ME", Convert.ToString(this.security.User.Usid));

                bugSql = Util.AlterSqlPerProjectPermissions(bugSql, this.security);

                DataSet dataSet = DbUtil.GetDataSet(bugSql);
                dataView = new DataView(dataSet.Tables[0]);
            }
            else
            {
                dataView = (DataView)Session["bugs"];
            }

            if (dataView == null)
            {
                Response.Write("Please recreate the list before trying to print...");
                return;
            }

            if (format != null && format == "excel")
            {
                Util.PrintAsExcel(System.Web.HttpContext.Current.Response, dataView);
            }
            else
            {
                PrintAsHtml(dataView);
            }
        }

        [HttpGet]
        public ActionResult PrintDetail(int? id, int? queryId)
        {
            if (id.HasValue)
            {
                ViewBag.DataRow = Bug.GetBugDataRow(id.Value, this.security);

                if (ViewBag.DataRow == null)
                {
                    return Content($"{Util.CapitalizeFirstLetter(this.applicationSettings.SingularBugLabel)}not found:&nbsp;{id}");
                }

                if (ViewBag.DataRow["pu_permission_level"] == 0)
                {
                    return Content($"You are not allowed to view this {this.applicationSettings.SingularBugLabel} not found:&nbsp;{id}");
                }

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - {Util.CapitalizeFirstLetter(this.applicationSettings.SingularBugLabel)} ID {id} {(string)ViewBag.DataRow["short_desc"]}"
                };
            }
            else
            {
                if (queryId.HasValue)
                {
                    var sql = @"select qu_sql from queries where qu_id = $1"
                        .Replace("$1", queryId.Value.ToString());

                    var bugSql = (string)DbUtil.ExecuteScalar(sql);

                    // replace magic variables
                    bugSql = bugSql.Replace("$ME", Convert.ToString(this.security.User.Usid));
                    bugSql = Util.AlterSqlPerProjectPermissions(bugSql, this.security);

                    // all we really need is the bugid, but let's do the same query as Bug/Print
                    ViewBag.DataSet = DbUtil.GetDataSet(bugSql);
                }
                else
                {
                    ViewBag.DataView = (DataView)Session["bugs"];
                }

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - print {this.applicationSettings.SingularBugLabel}"
                };
            }

            var cookie = Request.Cookies["images_inline"];

            if (cookie == null || cookie.Value == "0")
            {
                ViewBag.ImagesInline = false;
            }
            else
            {
                ViewBag.ImagesInline = true;
            }

            cookie = Request.Cookies["history_inline"];

            if (cookie == null || cookie.Value == "0")
            {
                ViewBag.HistoryInline = false;
            }
            else
            {
                ViewBag.HistoryInline = true;
            }

            return View();
        }

        [HttpPost]
        [Authorize(Roles = ApplicationRoles.Member)]
        public ActionResult Subscribe(int id, string actn)
        {
            var permissionLevel = Bug.GetBugPermissionLevel(id, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content(string.Empty);
            }

            string sql;

            if (actn == "1")
            {
                sql = @"insert into bug_subscriptions (bs_bug, bs_user) values($bg, $us)";
            }
            else
            {
                sql = @"delete from bug_subscriptions where bs_bug = $bg and bs_user = $us";
            }

            sql = sql.Replace("$bg", id.ToString());
            sql = sql.Replace("$us", Convert.ToString(this.security.User.Usid));

            DbUtil.ExecuteNonQuery(sql);

            return Content("Ok");
        }

        [HttpGet]
        public ActionResult WritePosts(int id, bool imagesInline, bool historyInline)
        {
            var permissionLevel = Bug.GetBugPermissionLevel(id, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                //TODO: research
                return Content("You are not allowed to view this item");
            }

            var dsPosts = PrintBug.GetBugPosts(id, this.security.User.ExternalUser, historyInline);

            ViewBag.Posts = dsPosts;
            ViewBag.BugId = id;
            ViewBag.PermissionLevel = permissionLevel;
            ViewBag.WriteLinks = true; //TODO: research
            ViewBag.ImagesInline = imagesInline;
            ViewBag.InternalPosts = true; //TODO: research
            ViewBag.User = this.security.User;
            ViewBag.ApplicationSettings = this.applicationSettings;

            return PartialView("Bug/_Posts");
        }

        [HttpGet]
        [Authorize(Roles = ApplicationRoles.Member)]
        public ActionResult Merge(int id)
        {
            var isAutorized = this.security.User.IsAdmin
                || this.security.User.CanMergeBugs;

            if (!isAutorized)
            {
                return Content("You are not allowed to use this page.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - merge {this.applicationSettings.SingularBugLabel}",
                SelectedItem = ApplicationSettings.PluralBugLabelDefault
            };

            ViewBag.Confirm = false;

            var model = new MergeModel
            {
                Id = id,
                FromBugId = id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ApplicationRoles.Member)]
        public ActionResult Merge(MergeModel model)
        {
            var isAutorized = this.security.User.IsAdmin
                || this.security.User.CanMergeBugs;

            if (!isAutorized)
            {
                return Content("You are not allowed to use this page.");
            }

            if (model.FromBugId == model.IntoBugId)
            {
                ModelState.AddModelError("IntoBugId", "\"Into\" bug cannot be the same as \"From\" bug.");
            }

            // Continue and see if from and to exist in db

            var sql = @"
                declare @from_desc nvarchar(200)
                declare @into_desc nvarchar(200)
                declare @from_id int
                declare @into_id int
                set @from_id = -1
                set @into_id = -1
                select @from_desc = bg_short_desc, @from_id = bg_id from bugs where bg_id = $from
                select @into_desc = bg_short_desc, @into_id = bg_id from bugs where bg_id = $into
                select @from_desc, @into_desc, @from_id, @into_id"
                .Replace("$from", model.FromBugId.ToString())
                .Replace("$into", model.IntoBugId.ToString());

            var dataRow = DbUtil.GetDataRow(sql);

            if ((int)dataRow[2] == -1)
            {
                ModelState.AddModelError("FromBugId", "\"From\" bug not found.");
            }

            if ((int)dataRow[3] == -1)
            {
                ModelState.AddModelError("IntoBugId", "\"Into\" bug not found.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - merge {this.applicationSettings.SingularBugLabel}",
                    SelectedItem = ApplicationSettings.PluralBugLabelDefault
                };

                return View(model);
            }

            if (model.Confirm)
            {
                // rename the attachments
                var uploadFolder = Util.GetUploadFolder();

                if (uploadFolder != null)
                {
                    sql = @"select bp_id, bp_file from bug_posts
                        where bp_type = 'file' and bp_bug = $from"
                        .Replace("$from", model.FromBugId.ToString());

                    var ds = DbUtil.GetDataSet(sql);

                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        // create path
                        var path = new StringBuilder(uploadFolder);

                        path.Append("\\");
                        path.Append(model.FromBugId);
                        path.Append("_");
                        path.Append(Convert.ToString(dr["bp_id"]));
                        path.Append("_");
                        path.Append(Convert.ToString(dr["bp_file"]));

                        if (System.IO.File.Exists(path.ToString()))
                        {
                            var path2 = new StringBuilder(uploadFolder);

                            path2.Append("\\");
                            path2.Append(model.IntoBugId);
                            path2.Append("_");
                            path2.Append(Convert.ToString(dr["bp_id"]));
                            path2.Append("_");
                            path2.Append(Convert.ToString(dr["bp_file"]));

                            System.IO.File.Move(path.ToString(), path2.ToString());
                        }
                    }
                }

                // copy the from db entries to the to
                sql = @"
                    insert into bug_subscriptions
                    (bs_bug, bs_user)
                    select $into, bs_user
                    from bug_subscriptions
                    where bs_bug = $from
                    and bs_user not in (select bs_user from bug_subscriptions where bs_bug = $into)

                    insert into bug_user
                    (bu_bug, bu_user, bu_flag, bu_flag_datetime, bu_seen, bu_seen_datetime, bu_vote, bu_vote_datetime)
                    select $into, bu_user, bu_flag, bu_flag_datetime, bu_seen, bu_seen_datetime, bu_vote, bu_vote_datetime
                    from bug_user
                    where bu_bug = $from
                    and bu_user not in (select bu_user from bug_user where bu_bug = $into)

                    update bug_posts     set bp_bug     = $into	where bp_bug = $from
                    update bug_tasks     set tsk_bug    = $into where tsk_bug = $from
                    update svn_revisions set svnrev_bug = $into where svnrev_bug = $from
                    update hg_revisions  set hgrev_bug  = $into where hgrev_bug = $from
                    update git_commits   set gitcom_bug = $into where gitcom_bug = $from"
                    .Replace("$from", model.FromBugId.ToString())
                    .Replace("$into", model.IntoBugId.ToString());

                DbUtil.ExecuteNonQuery(sql);

                // record the merge itself

                sql = @"insert into bug_posts
                    (bp_bug, bp_user, bp_date, bp_type, bp_comment, bp_comment_search)
                    values($into,$us,getdate(), 'comment', 'merged bug $from into this bug:', 'merged bug $from into this bug:')
                    select scope_identity()"
                    .Replace("$from", model.FromBugId.ToString())
                    .Replace("$into", model.IntoBugId.ToString())
                    .Replace("$us", Convert.ToString(this.security.User.Usid));

                var commentId = Convert.ToInt32(DbUtil.ExecuteScalar(sql));

                // update bug comments with info from old bug
                sql = @"update bug_posts
                    set bp_comment = convert(nvarchar,bp_comment) + char(10) + bg_short_desc
                    from bugs where bg_id = $from
                    and bp_id = $bc"
                    .Replace("$from", model.FromBugId.ToString())
                    .Replace("$bc", Convert.ToString(commentId));

                DbUtil.ExecuteNonQuery(sql);

                // delete the from bug
                Bug.DeleteBug(model.FromBugId);

                // delete the from bug from the list, if there is a list
                var dvBugs = (DataView)Session["bugs"];

                if (dvBugs != null)
                {
                    // read through the list of bugs looking for the one that matches the from
                    var index = 0;
                    foreach (DataRowView drv in dvBugs)
                    {
                        if (model.FromBugId == (int)drv[1])
                        {
                            dvBugs.Delete(index);
                            break;
                        }

                        index++;
                    }
                }

                Bug.SendNotifications(Bug.Update, model.FromBugId, this.security);

                return Redirect($"~/Bugs/Edit.aspx?id={model.IntoBugId}");
            }

            ModelState.Clear();

            ModelState.AddModelError("StaticFromBug", model.FromBugId.ToString());
            ModelState.AddModelError("StaticIntoBug", model.IntoBugId.ToString());
            ModelState.AddModelError("StaticFromBugDescription", (string)dataRow[0]);
            ModelState.AddModelError("StaticIntoBugDescription", (string)dataRow[1]);

            model.Confirm = true;

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - merge {this.applicationSettings.SingularBugLabel}",
                SelectedItem = ApplicationSettings.PluralBugLabelDefault
            };

            return View(model);
        }

        [HttpGet]
        public ActionResult ViewSubscriber(int id)
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
                Title = $"{this.applicationSettings.AppTitle} - view subscribers",
                SelectedItem = ApplicationSettings.PluralBugLabelDefault
            };

            // clean up bug subscriptions that no longer fit the security restrictions

            Bug.AutoSubscribe(id);

            // show who is subscribed

            var sql = string.Empty;
            var token = new HtmlHelper(new ViewContext(), new ViewPage()).AntiForgeryToken().ToString();

            if (this.security.User.IsAdmin)
            {
                sql = @"
                    select
                        '<form action=/Bug/DeleteSubscriber method=post id=f_$bg_' + convert(varchar, us_id) + '>
                            $token
                            <input type=hidden name=id value=$bg>
                            <input type=hidden name=userId value=' + convert(varchar, us_id) + '>

                            <a href=# onclick=document.getElementById(''f_$bg_' + convert(varchar, us_id) + ''').submit();>unsubscribe</a>
                        </form>' [$no_sort_unsubscriber],
                    us_username [user],
                    us_lastname + ', ' + us_firstname [name],
                    us_email [email],
                    case when us_reported_notifications < 4 or us_assigned_notifications < 4 or us_subscribed_notifications < 4 then 'Y' else 'N' end [user is<br>filtering<br>notifications]
                    from bug_subscriptions
                    inner join users on bs_user = us_id
                    where bs_bug = $bg
                    and us_enable_notifications = 1
                    and us_active = 1
                    order by 1";
            }
            else
            {
                sql = @"
                    select
                    us_username [user],
                    us_lastname + ', ' + us_firstname [name],
                    us_email [email],
                    case when us_reported_notifications < 4 or us_assigned_notifications < 4 or us_subscribed_notifications < 4 then 'Y' else 'N' end [user is<br>filtering<br>notifications]
                    from bug_subscriptions
                    inner join users on bs_user = us_id
                    where bs_bug = $bg
                    and us_enable_notifications = 1
                    and us_active = 1
                    order by 1";
            }

            sql = sql.Replace("$token", token);
            sql = sql.Replace("$bg", Convert.ToString(id));

            ViewBag.Table = new SortableTableModel
            {
                DataTable = DbUtil.GetDataSet(sql).Tables[0],
                HtmlEncode = false
            };

            // Get list of users who could be subscribed to this bug.

            sql = @"
                declare @project int;
                declare @org int;
                select @project = bg_project, @org = bg_org from bugs where bg_id = $bg;";

            // Only users explicitly allowed will be listed
            if (this.applicationSettings.DefaultPermissionLevel == 0)
            {
                sql +=
                    @"select us_id, case when $fullnames then us_lastname + ', ' + us_firstname else us_username end us_username
                    from users
                    where us_active = 1
                    and us_enable_notifications = 1
                    and us_id in
                        (select pu_user from project_user_xref
                        where pu_project = @project
                        and pu_permission_level <> 0)
                    and us_id not in (
                        select us_id
                        from bug_subscriptions
                        inner join users on bs_user = us_id
                        where bs_bug = $bg
                        and us_enable_notifications = 1
                        and us_active = 1)
                    and us_id not in (
                        select us_id from users
                        inner join orgs on us_org = og_id
                        where us_org <> @org
                        and og_other_orgs_permission_level = 0)
                    order by us_username; ";
                // Only users explictly DISallowed will be omitted
            }
            else
            {
                sql +=
                    @"select us_id, case when $fullnames then us_lastname + ', ' + us_firstname else us_username end us_username
                    from users
                    where us_active = 1
                    and us_enable_notifications = 1
                    and us_id not in
                        (select pu_user from project_user_xref
                        where pu_project = @project
                        and pu_permission_level = 0)
                    and us_id not in (
                        select us_id
                        from bug_subscriptions
                        inner join users on bs_user = us_id
                        where bs_bug = $bg
                        and us_enable_notifications = 1
                        and us_active = 1)
                    and us_id not in (
                        select us_id from users
                        inner join orgs on us_org = og_id
                        where us_org <> @org
                        and og_other_orgs_permission_level = 0)
                    order by us_username; ";
            }

            if (!this.applicationSettings.UseFullNames)
            {
                // false condition
                sql = sql.Replace("$fullnames", "0 = 1");
            }
            else
            {
                // true condition
                sql = sql.Replace("$fullnames", "1 = 1");
            }

            sql = sql.Replace("$bg", Convert.ToString(id));

            ViewBag.Users = new List<SelectListItem>();

            var usersDataView = DbUtil.GetDataView(sql);

            foreach (DataRowView row in usersDataView)
            {
                ViewBag.Users.Add(new SelectListItem
                {
                    Value = ((int)row["us_id"]).ToString(),
                    Text = (string)row["us_username"]
                });
            }

            if (ViewBag.Users.Count == 0)
            {
                ViewBag.Users.Insert(0, new SelectListItem
                {
                    Value = "0",
                    Text = "[no users to select]"
                });
            }
            else
            {
                ViewBag.Users.Insert(0, new SelectListItem
                {
                    Value = "0",
                    Text = "[select to add]"
                });
            }

            var model = new CreateSubscriberModel
            {
                Id = id
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CreateSubscriber(CreateSubscriberModel model)
        {
            var permissionLevel = Bug.GetBugPermissionLevel(model.Id, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content("You are not allowed to view this item");
            }

            if (permissionLevel == SecurityPermissionLevel.PermissionReadonly)
            {
                return Content("You are not allowed to edit this item");
            }

            if (model.UserId != 0)
            {
                var newSubscriberUserid = Convert.ToInt32(Request["userid"]);

                var sql = @"delete from bug_subscriptions where bs_bug = $bg and bs_user = $us;
                        insert into bug_subscriptions (bs_bug, bs_user) values($bg, $us)";

                sql = sql.Replace("$bg", Convert.ToString(model.UserId));
                sql = sql.Replace("$us", Convert.ToString(newSubscriberUserid));

                DbUtil.ExecuteNonQuery(sql);

                // send a notification to this user only
                Bug.SendNotifications(Bug.Update, model.Id, this.security, newSubscriberUserid);
            }

            return RedirectToAction(nameof(ViewSubscriber), new { id = model.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ApplicationRoles.Administrator)]
        public ActionResult DeleteSubscriber(DeleteSubscriberModel model)
        {
            var sql = "delete from bug_subscriptions where bs_bug = $bg_id and bs_user = $us_id";

            sql = sql.Replace("$bg_id", model.Id.ToString());
            sql = sql.Replace("$us_id", model.UserId.ToString());

            DbUtil.ExecuteNonQuery(sql);

            return RedirectToAction(nameof(ViewSubscriber), new { id = model.Id });
        }

        [HttpGet]
        public ActionResult Vote(int bugid, int vote)
        {
            var dv = (DataView)Session["bugs"];

            if (dv == null)
            {
                return Content(string.Empty);
            }

            var permissionLevel = Bug.GetBugPermissionLevel(bugid, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content(string.Empty);
            }

            for (var i = 0; i < dv.Count; i++)
            {
                if ((int)dv[i][1] == bugid)
                {
                    // treat it like a delta and update the cached vote count.
                    var objVoteCount = MemoryCache.Default.Get(Convert.ToString(bugid));
                    var voteCount = 0;

                    if (objVoteCount != null)
                    {
                        voteCount = (int)objVoteCount;
                    }

                    voteCount += vote;

                    MemoryCache.Default.Set(Convert.ToString(bugid), voteCount, new CacheItemPolicy { Priority = CacheItemPriority.NotRemovable });

                    // now treat it more like a boolean
                    if (vote == -1)
                    {
                        vote = 0;
                    }

                    dv[i]["$VOTE"] = vote;

                    var sql = @"
                        if not exists (select bu_bug from bug_user where bu_bug = $bg and bu_user = $us)
                        insert into bug_user (bu_bug, bu_user, bu_flag, bu_seen, bu_vote) values($bg, $us, 0, 0, 1) 
                        update bug_user set bu_vote = $vote, bu_vote_datetime = getdate() where bu_bug = $bg and bu_user = $us and bu_vote <> $vote"
                        .Replace("$vote", Convert.ToString(vote))
                        .Replace("$bg", Convert.ToString(bugid))
                        .Replace("$us", Convert.ToString(this.security.User.Usid));

                    DbUtil.ExecuteNonQuery(sql);

                    break;
                }
            }

            return Content("OK");
        }

        [HttpGet]
        public ActionResult Flag(int bugid, int flag)
        {
            var dv = (DataView)Session["bugs"];

            if (dv == null)
            {
                return Content(string.Empty);
            }

            var permissionLevel = Bug.GetBugPermissionLevel(bugid, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content(string.Empty);
            }

            for (var i = 0; i < dv.Count; i++)
            {
                if ((int)dv[i][1] == bugid)
                {
                    dv[i]["$FLAG"] = flag;

                    var sql = @"
                        if not exists (select bu_bug from bug_user where bu_bug = $bg and bu_user = $us)
                        insert into bug_user (bu_bug, bu_user, bu_flag, bu_seen, bu_vote) values($bg, $us, 1, 0, 0) 
                        update bug_user set bu_flag = $fl, bu_flag_datetime = getdate() where bu_bug = $bg and bu_user = $us and bu_flag <> $fl"
                    .Replace("$bg", Convert.ToString(bugid))
                    .Replace("$us", Convert.ToString(this.security.User.Usid))
                    .Replace("$fl", Convert.ToString(flag));

                    DbUtil.ExecuteNonQuery(sql);
                    break;
                }
            }

            return Content("OK");
        }

        [HttpGet]
        public ActionResult Seen(int bugid, int seen)
        {
            var dv = (DataView)Session["bugs"];

            if (dv == null)
            {
                return Content(string.Empty);
            }

            var permissionLevel = Bug.GetBugPermissionLevel(bugid, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content(string.Empty);
            }

            for (var i = 0; i < dv.Count; i++)
            {
                if ((int)dv[i][1] == bugid)
                {
                    dv[i]["$SEEN"] = seen;

                    var sql = @"
                        if not exists (select bu_bug from bug_user where bu_bug = $bg and bu_user = $us)
                        insert into bug_user (bu_bug, bu_user, bu_flag, bu_seen, bu_vote) values($bg, $us, 0, 1, 0) 
                        update bug_user set bu_seen = $seen, bu_seen_datetime = getdate() where bu_bug = $bg and bu_user = $us and bu_seen <> $seen"
                    .Replace("$seen", Convert.ToString(seen))
                    .Replace("$bg", Convert.ToString(bugid))
                    .Replace("$us", Convert.ToString(this.security.User.Usid));

                    DbUtil.ExecuteNonQuery(sql);

                    break;
                }
            }

            return Content("OK");
        }

        [HttpGet]
        public ActionResult Tag()
        {
            if (this.security.User.CategoryFieldPermissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content("Not access");
            }

            var tags = PrintTags();

            return View(tags);
        }

        [HttpGet]
        public ActionResult SendEmail()
        {
            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - send email",
                SelectedItem = this.applicationSettings.PluralBugLabel
            };

            var stringBpId = Request["bp_id"];
            var stringBgId = Request["bg_id"];
            var requestTo = Request["to"];
            var reply = Request["reply"];

            ViewBag.EnableInternalPosts = this.applicationSettings.EnableInternalOnlyPosts;
            ViewBag.Project = -1;

            Session["email_addresses"] = null;

            string sql;
            DataRow dr = null;

            var model = new SendEmailModel();

            if (stringBpId != null)
            {
                stringBpId = Util.SanitizeInteger(stringBpId);

                sql = @"select
                    bp_parent,
                    bp_file,
                    bp_id,
                    bg_id,
                    bg_short_desc,
                    bp_email_from,
                    bp_comment,
                    bp_email_from,
                    bp_date,
                    bp_type,
                    bp_content_type,
                    bg_project,
                    bp_hidden_from_external_users,
                    isnull(us_signature,'') [us_signature],
                    isnull(pj_pop3_email_from,'') [pj_pop3_email_from],
                    isnull(us_email,'') [us_email],
                    isnull(us_firstname,'') [us_firstname],
                    isnull(us_lastname,'') [us_lastname]				
                    from bug_posts
                    inner join bugs on bp_bug = bg_id
                    inner join users on us_id = $us
                    left outer join projects on bg_project = pj_id
                    where bp_id = $id
                    or (bp_parent = $id and bp_type='file')";

                sql = sql.Replace("$id", stringBpId);
                sql = sql.Replace("$us", Convert.ToString(this.security.User.Usid));

                var dv = DbUtil.GetDataView(sql);

                dr = null;

                if (dv.Count > 0)
                {
                    dv.RowFilter = "bp_id = " + stringBpId;
                    if (dv.Count > 0)
                    {
                        dr = dv[0].Row;
                    }
                }

                var intBgId = (int)dr["bg_id"];
                var permissionLevel = Bug.GetBugPermissionLevel(intBgId, this.security);

                if (permissionLevel == SecurityPermissionLevel.PermissionNone)
                {
                    Response.Write("You are not allowed to view this item");
                    Response.End();
                }

                if ((int)dr["bp_hidden_from_external_users"] == 1)
                    if (this.security.User.ExternalUser)
                    {
                        Response.Write("You are not allowed to view this post");
                        Response.End();
                    }

                stringBgId = Convert.ToString(dr["bg_id"]);

                model.BugId = int.Parse(stringBgId);
                model.To = dr["bp_email_from"].ToString();

                // Work around for a mysterious bug:
                // http://sourceforge.net/tracker/?func=detail&aid=2815733&group_id=66812&atid=515837
                if (this.applicationSettings.StripDisplayNameFromEmailAddress)
                {
                    model.To = Email.SimplifyEmailAddress(model.To);
                }

                LoadFromDropdown(dr, true); // list the project's email address first

                if (reply != null && reply == "all")
                {
                    var regex = new Regex("\n");
                    var lines = regex.Split((string)dr["bp_comment"]);
                    var ccAddrs = string.Empty;

                    var max = lines.Length < 5 ? lines.Length : 5;

                    // gather cc addresses, which might include the current user
                    for (var i = 0; i < max; i++)
                        if (lines[i].StartsWith("To:") || lines[i].StartsWith("Cc:"))
                        {
                            var ccAddr = lines[i].Substring(3, lines[i].Length - 3).Trim();

                            // don't cc yourself

                            if (ccAddr.IndexOf(ViewBag.Froms[0]) == -1)
                            {
                                if (!string.IsNullOrEmpty(ccAddrs)) ccAddrs += ",";

                                ccAddrs += ccAddr;
                            }
                        }

                    model.CC = ccAddrs;
                }

                if (!string.IsNullOrEmpty(dr["us_signature"].ToString()))
                {
                    if (this.security.User.UseFckeditor)
                    {
                        model.Body += "<br><br><br>";
                        model.Body += dr["us_signature"].ToString().Replace("\r\n", "<br>");
                        model.Body += "<br><br><br>";
                    }
                    else
                    {
                        model.Body += "\n\n\n";
                        model.Body += dr["us_signature"].ToString();
                        model.Body += "\n\n\n";
                    }
                }

                if (Request["quote"] != null)
                {
                    var regex = new Regex("\n");
                    var lines = regex.Split((string)dr["bp_comment"]);

                    if (dr["bp_type"].ToString() == "received")
                    {
                        if (this.security.User.UseFckeditor)
                        {
                            model.Body += "<br><br><br>";
                            model.Body += "&#62;From: " +
                                               dr["bp_email_from"].ToString().Replace("<", "&#60;")
                                                   .Replace(">", "&#62;") + "<br>";
                        }
                        else
                        {
                            model.Body += "\n\n\n";
                            model.Body += ">From: " + dr["bp_email_from"] + "\n";
                        }
                    }

                    var nextLineIsDate = false;
                    for (var i = 0; i < lines.Length; i++)
                        if (i < 4 && (lines[i].IndexOf("To:") == 0 || lines[i].IndexOf("Cc:") == 0))
                        {
                            nextLineIsDate = true;
                            if (this.security.User.UseFckeditor)
                                model.Body +=
                                    "&#62;" + lines[i].Replace("<", "&#60;").Replace(">", "&#62;") + "<br>";
                            else
                                model.Body += ">" + lines[i] + "\n";
                        }
                        else if (nextLineIsDate)
                        {
                            nextLineIsDate = false;
                            if (this.security.User.UseFckeditor)
                                model.Body +=
                                    "&#62;Date: " + Convert.ToString(dr["bp_date"]) + "<br>&#62;<br>";
                            else
                                model.Body += ">Date: " + Convert.ToString(dr["bp_date"]) + "\n>\n";
                        }
                        else
                        {
                            if (this.security.User.UseFckeditor)
                            {
                                if (Convert.ToString(dr["bp_content_type"]) != "text/html")
                                {
                                    model.Body +=
                                        "&#62;" + lines[i].Replace("<", "&#60;").Replace(">", "&#62;") +
                                        "<br>";
                                }
                                else
                                {
                                    if (i == 0) model.Body += "<hr>";

                                    model.Body += lines[i];
                                }
                            }
                            else
                            {
                                model.Body += ">" + lines[i] + "\n";
                            }
                        }
                }

                ViewBag.Attachments = new List<SelectListItem>();

                if (reply == "forward")
                {
                    model.To = string.Empty;
                    //original attachments
                    //dv.RowFilter = "bp_parent = " + string_bp_id;
                    dv.RowFilter = "bp_type = 'file'";
                    foreach (DataRowView drv in dv)
                    {
                        ViewBag.Attachments.Add(new SelectListItem
                        {
                            Value = drv["bp_id"].ToString(),
                            Text = drv["bp_file"].ToString()
                        });
                    }
                }
            }
            else if (stringBgId != null)
            {
                stringBgId = Util.SanitizeInteger(stringBgId);

                var permissionLevel = Bug.GetBugPermissionLevel(Convert.ToInt32(stringBgId), this.security);
                if (permissionLevel == SecurityPermissionLevel.PermissionNone
                    || permissionLevel == SecurityPermissionLevel.PermissionReadonly)
                {
                    Response.Write("You are not allowed to edit this item");
                    Response.End();
                }

                sql = @"select
                    bg_short_desc,
                    bg_project,
                    isnull(us_signature,'') [us_signature],
                    isnull(us_email,'') [us_email],
                    isnull(us_firstname,'') [us_firstname],
                    isnull(us_lastname,'') [us_lastname],
                    isnull(pj_pop3_email_from,'') [pj_pop3_email_from]
                    from bugs
                    inner join users on us_id = $us
                    left outer join projects on bg_project = pj_id
                    where bg_id = $bg";

                sql = sql.Replace("$us", Convert.ToString(this.security.User.Usid));
                sql = sql.Replace("$bg", stringBgId);

                dr = DbUtil.GetDataRow(sql);

                LoadFromDropdown(dr, false); // list the user's email first, then the project

                model.BugId = int.Parse(stringBgId);

                if (requestTo != null) model.To = requestTo;

                // Work around for a mysterious bug:
                // http://sourceforge.net/tracker/?func=detail&aid=2815733&group_id=66812&atid=515837
                if (this.applicationSettings.StripDisplayNameFromEmailAddress) model.To = Email.SimplifyEmailAddress(model.To);

                if (!string.IsNullOrEmpty(dr["us_signature"].ToString()))
                {
                    if (this.security.User.UseFckeditor)
                    {
                        model.Body += "<br><br><br>";
                        model.Body += dr["us_signature"].ToString().Replace("\r\n", "<br>");
                    }
                    else
                    {
                        model.Body += "\n\n\n";
                        model.Body += dr["us_signature"].ToString();
                    }
                }
            }

            model.BugDescription = (string)dr["bg_short_desc"];

            if (stringBpId != null || stringBgId != null)
            {
                model.Subject = (string)dr["bg_short_desc"]
                                     + "  (" + this.applicationSettings.TrackingIdString
                                     + model.BugId.ToString()
                                     + ")";

                // for determining which users to show in "address book"
                ViewBag.Project = (int)dr["bg_project"];
            }

            ViewBag.Priorities = new List<SelectListItem>();

            ViewBag.Priorities.Add(new SelectListItem
            {
                Value = "High",
                Text = "High"
            });

            ViewBag.Priorities.Add(new SelectListItem
            {
                Value = "Normal",
                Text = "Normal",
                Selected = true
            });

            ViewBag.Priorities.Add(new SelectListItem
            {
                Value = "Low",
                Text = "Low"
            });

            PutAddresses();

            if (TempData["Errors"] is IReadOnlyCollection<IFailError> failErrors)
                foreach (var failError in failErrors)
                    ModelState.AddModelError(failError.Property, failError.Message);

            if (TempData["Errors2"] is Dictionary<string, ModelErrorCollection> failErrors2)
                foreach (var failError in failErrors2)
                    foreach (var err in failError.Value)
                        ModelState.AddModelError(failError.Key, err.ErrorMessage);

            if (TempData["Model"] is SendEmailModel model2)
                return View(model2);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult SendEmail(SendEmailModel model)
        {
            ValidateSendEmail(model);

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Email was not sent.");

                TempData["Model"] = model;
                TempData["Errors2"] = ModelState.Where(x => x.Value.Errors.Any())
                    .Select(x => new { x.Key, x.Value.Errors })
                    .ToDictionary(x => x.Key, x => x.Errors);

                return RedirectToAction(nameof(SendEmail), new { bg_id = model.BugId });
            }

            var sql = @"
                insert into bug_posts
                    (bp_bug, bp_user, bp_date, bp_comment, bp_comment_search, bp_email_from, bp_email_to, bp_type, bp_content_type, bp_email_cc)
                    values($id, $us, getdate(), N'$cm', N'$cs', N'$fr',  N'$to', 'sent', N'$ct', N'$cc');
                select scope_identity()
                update bugs set
                    bg_last_updated_user = $us,
                    bg_last_updated_date = getdate()
                    where bg_id = $id";

            sql = sql.Replace("$id", model.BugId.ToString());
            sql = sql.Replace("$us", Convert.ToString(this.security.User.Usid));

            if (this.security.User.UseFckeditor)
            {
                var adjustedBody = "Subject: " + model.Subject + "<br><br>";
                adjustedBody += Util.StripDangerousTags(model.Body);

                sql = sql.Replace("$cm", adjustedBody.Replace("'", "&#39;"));
                sql = sql.Replace("$cs", adjustedBody.Replace("'", "''"));
                sql = sql.Replace("$ct", "text/html");
            }
            else
            {
                var adjustedBody = "Subject: " + model.Subject + "\n\n";
                adjustedBody += HttpUtility.HtmlDecode(model.Body);
                adjustedBody = adjustedBody.Replace("'", "''");

                sql = sql.Replace("$cm", adjustedBody);
                sql = sql.Replace("$cs", adjustedBody);
                sql = sql.Replace("$ct", "text/plain");
            }

            sql = sql.Replace("$fr", model.From?.Replace("'", "''") ?? string.Empty);
            sql = sql.Replace("$to", model.To?.Replace("'", "''") ?? string.Empty);
            sql = sql.Replace("$cc", model.CC?.Replace("'", "''") ?? string.Empty);

            var commentId = Convert.ToInt32(DbUtil.ExecuteScalar(sql));
            var attachments = HandleAttachments(commentId, security, model);

            string bodyText;
            BtnetMailFormat format;
            BtnetMailPriority priority;

            switch (model.Priority)
            {
                case "High":
                    priority = BtnetMailPriority.High;
                    break;
                case "Low":
                    priority = BtnetMailPriority.Low;
                    break;
                default:
                    priority = BtnetMailPriority.Normal;
                    break;
            }

            if (model.IncludePrintOfBug)
            {
                // white space isn't handled well, I guess.
                if (this.security.User.UseFckeditor)
                {
                    bodyText = model.Body;
                    bodyText += "<br><br>";
                }
                else
                {
                    bodyText = model.Body.Replace("\n", "<br>");
                    bodyText = bodyText.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");
                    bodyText = bodyText.Replace("  ", "&nbsp; ");
                }

                // Get bug html
                var bugDr = Bug.GetBugDataRow(model.BugId, security);

                // Create a fake response and let the code
                // write the html to that response
                var writer = new StringWriter();
                var myResponse = new HttpResponse(writer);
                var html = PrintBug.PrintBugNew(bugDr, security,
                    true, // include style
                    false, // images_inline
                    true, // history_inline
                    model.IncludeCommentsVisibleToInternalUsersOnly); // internal_posts

                myResponse.Write(html);

                bodyText += "<hr>" + writer.ToString();

                format = BtnetMailFormat.Html;
            }
            else
            {
                if (this.security.User.UseFckeditor)
                {
                    bodyText = model.Body;
                    format = BtnetMailFormat.Html;
                }
                else
                {
                    bodyText = HttpUtility.HtmlDecode(model.Body);
                    //body_text = body_text.Replace("\n","\r\n");
                    format = BtnetMailFormat.Text;
                }
            }

            var result = Email.SendEmail( // 9 args
                model.To, model.From, model.CC ?? string.Empty, model.Subject,
                bodyText,
                format,
                priority,
                attachments, model.ReturnReceipt);

            Bug.SendNotifications(Bug.Update, model.BugId, security);
            WhatsNew.AddNews(model.BugId, model.BugDescription, "email sent", security);

            if (string.IsNullOrEmpty(result))
            {
                return RedirectToAction("Update", new { id = model.BugId });
            }

            ModelState.AddModelError(string.Empty, result);

            TempData["Model"] = model;
            TempData["Errors2"] = ModelState.Where(x => x.Value.Errors.Any())
                .Select(x => new { x.Key, x.Value.Errors })
                .ToDictionary(x => x.Key, x => x.Errors);

            return RedirectToAction(nameof(SendEmail), new { bg_id = model.BugId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Relationship(RelationshipModel model)
        {
            var permissionLevel = Bug.GetBugPermissionLevel(model.BugId, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content("You are not allowed to view this item");
            }

            if (permissionLevel == SecurityPermissionLevel.PermissionReadonly)
            {
                return Content("You are not allowed to edit this item");
            }

            var sql = string.Empty;

            if (model.Action == "remove") // remove
            {
                if (this.security.User.IsGuest)
                {
                    return Content("You are not allowed to delete a relationship");
                }

                sql = @"
                    delete from bug_relationships where re_bug2 = $bg2 and re_bug1 = $bg;
                    delete from bug_relationships where re_bug1 = $bg2 and re_bug2 = $bg;
                    insert into bug_posts
                            (bp_bug, bp_user, bp_date, bp_comment, bp_type)
                            values($bg, $us, getdate(), N'deleted relationship to $bg2', 'update')";

                sql = sql.Replace("$bg2", Convert.ToString(model.RelatedBugId));
                sql = sql.Replace("$bg", Convert.ToString(model.BugId));
                sql = sql.Replace("$us", Convert.ToString(this.security.User.Usid));

                DbUtil.ExecuteNonQuery(sql);

                return RedirectToAction(nameof(Relationship), new { bugId = model.BugId });
            }

            // adding
            if (model.BugId == model.RelatedBugId)
            {
                ModelState.AddModelError(string.Empty, "Cannot create a relationship to self.");
            }

            var rows = 0;

            // check if bug exists
            sql = @"select count(1) from bugs where bg_id = $bg2";

            sql = sql.Replace("$bg2", Convert.ToString(model.RelatedBugId));

            rows = (int)DbUtil.ExecuteScalar(sql);

            if (rows == 0)
            {
                ModelState.AddModelError(string.Empty, "Not found.");
            }

            // check if relationship exists
            sql = @"select count(1) from bug_relationships where re_bug1 = $bg and re_bug2 = $bg2";
            sql = sql.Replace("$bg2", Convert.ToString(model.RelatedBugId));
            sql = sql.Replace("$bg", Convert.ToString(model.BugId));

            rows = (int)DbUtil.ExecuteScalar(sql);

            if (rows > 0)
            {
                ModelState.AddModelError(string.Empty, "Relationship already exists.");
            }
            else
            {
                // check permission of related bug
                var permissionLevel2 = Bug.GetBugPermissionLevel(model.RelatedBugId, this.security);

                if (permissionLevel2 == SecurityPermissionLevel.PermissionNone)
                {
                    ModelState.AddModelError(string.Empty, "You are not allowed to view the related item.");
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - relationships",
                    SelectedItem = ApplicationSettings.PluralBugLabelDefault
                };

                sql = @"
                    select bg_id [id],
                        bg_short_desc [desc],
                        re_type [comment],
                        st_name [status],
                        case
                            when re_direction = 0 then ''
                            when re_direction = 2 then 'child of $bg'
                            else                       'parent of $bg' 
                        end as [parent or child],
                        '<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Bugs/Edit.aspx?id=") + @"' + convert(varchar,bg_id) + '>view</a>' [$no_sort_view]";

                if (!this.security.User.IsGuest && permissionLevel == SecurityPermissionLevel.PermissionAll)
                {
                    sql += @"
                    ,'<a href=''javascript:remove(' + convert(varchar,re_bug2) + ')''>detach</a>' [$no_sort_detach]";

                    sql += @"
                    from bugs
                    inner join bug_relationships on bg_id = re_bug2
                    left outer join statuses on st_id = bg_status
                    where re_bug1 = $bg
                    order by bg_id desc";
                }

                sql = sql.Replace("$bg", Convert.ToString(model.BugId));
                sql = Util.AlterSqlPerProjectPermissions(sql, this.security);

                ViewBag.SortableTable = new SortableTableModel
                {
                    DataTable = DbUtil.GetDataSet(sql).Tables[0],
                    HtmlEncode = false
                };

                return View(model);
            }

            // insert the relationship both ways
            sql = @"
                insert into bug_relationships (re_bug1, re_bug2, re_type, re_direction) values($bg, $bg2, N'$ty', $dir1);
                insert into bug_relationships (re_bug2, re_bug1, re_type, re_direction) values($bg, $bg2, N'$ty', $dir2);
                insert into bug_posts
                    (bp_bug, bp_user, bp_date, bp_comment, bp_type)
                    values($bg, $us, getdate(), N'added relationship to $bg2', 'update');";

            sql = sql.Replace("$bg2", Convert.ToString(model.RelatedBugId));
            sql = sql.Replace("$bg", Convert.ToString(model.BugId));
            sql = sql.Replace("$us", Convert.ToString(this.security.User.Usid));
            sql = sql.Replace("$ty", model.Comment);

            if (model.Relation == 0)
            {
                sql = sql.Replace("$dir2", "0");
                sql = sql.Replace("$dir1", "0");
            }
            else if (model.Relation == 1)
            {
                sql = sql.Replace("$dir2", "1");
                sql = sql.Replace("$dir1", "2");
            }
            else
            {
                sql = sql.Replace("$dir2", "2");
                sql = sql.Replace("$dir1", "1");
            }

            DbUtil.ExecuteNonQuery(sql);

            return RedirectToAction(nameof(Relationship), new { bugId = model.BugId });
        }

        [HttpGet]
        public ActionResult Relationship(int bugId)
        {
            var permissionLevel = Bug.GetBugPermissionLevel(bugId, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content("You are not allowed to view this item");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - relationships",
                SelectedItem = ApplicationSettings.PluralBugLabelDefault
            };

            var model = new RelationshipModel
            {
                BugId = bugId,
                Action = "add"
            };

            var sql = @"
                select bg_id [id],
                    bg_short_desc [desc],
                    re_type [comment],
                    st_name [status],
                    case
                        when re_direction = 0 then ''
                        when re_direction = 2 then 'child of $bg'
                        else                       'parent of $bg' 
                    end as [parent or child],
                    '<a target=_blank href=" + VirtualPathUtility.ToAbsolute("~/Bugs/Edit.aspx?id=") + @"' + convert(varchar,bg_id) + '>view</a>' [$no_sort_view]";

            if (!this.security.User.IsGuest && permissionLevel == SecurityPermissionLevel.PermissionAll)
            {
                sql += @"
                    ,'<a href=''javascript:remove(' + convert(varchar,re_bug2) + ')''>detach</a>' [$no_sort_detach]";

                sql += @"
                    from bugs
                    inner join bug_relationships on bg_id = re_bug2
                    left outer join statuses on st_id = bg_status
                    where re_bug1 = $bg
                    order by bg_id desc";
            }

            sql = sql.Replace("$bg", Convert.ToString(bugId));
            sql = Util.AlterSqlPerProjectPermissions(sql, this.security);

            ViewBag.SortableTable = new SortableTableModel
            {
                DataTable = DbUtil.GetDataSet(sql).Tables[0],
                HtmlEncode = false
            };

            return View(model);
        }

        private void LoadQueryDropdown()
        {
            // populate query drop down
            var sql = @"/* query dropdown */
                select qu_id, qu_desc
                from queries
                where (isnull(qu_user,0) = 0 and isnull(qu_org,0) = 0)
                or isnull(qu_user,0) = $us
                or isnull(qu_org,0) = $org
                order by qu_desc";

            sql = sql.Replace("$us", Convert.ToString(this.security.User.Usid));
            sql = sql.Replace("$org", Convert.ToString(this.security.User.Org));

            ViewBag.Queries = new List<SelectListItem>();

            foreach (DataRowView row in DbUtil.GetDataView(sql))
            {
                ViewBag.Queries.Add(new SelectListItem
                {
                    Value = ((int)row["qu_id"]).ToString(),
                    Text = (string)row["qu_desc"],
                });
            }
        }

        private string DoQuery(IndexModel model)
        {
            // figure out what SQL to run and run it.
            string bugSql = null;

            // From the URL
            if (model.QueryId == 0)
            {
                // specified in URL?
                model.QueryId = Convert.ToInt32(Util.SanitizeInteger(Request["qu_id"]));
            }

            // From a previous viewing of this page
            if (model.QueryId == 0 && Session["SelectedBugQuery"] != null)
            {
                // Is there a previously selected query, from a use of this page
                // earlier in this session?
                model.QueryId = (int)Session["SelectedBugQuery"];
            }

            if (model.QueryId != 0)
            {
                // Use sql specified in query string.
                // This is the normal path from the queries page.
                var sql = @"select qu_sql from queries where qu_id = $quid";

                sql = sql.Replace("$quid", Convert.ToString(model.QueryId));

                bugSql = (string)DbUtil.ExecuteScalar(sql);
            }

            if (bugSql == null)
            {
                // This is the normal path after logging in.
                // Use sql associated with user
                var sql = @"select qu_id, qu_sql from queries where qu_id in
                    (select us_default_query from users where us_id = $us)";

                sql = sql.Replace("$us", Convert.ToString(this.security.User.Usid));

                var dr = DbUtil.GetDataRow(sql);

                if (dr != null)
                {
                    model.QueryId = (int)dr["qu_id"];

                    bugSql = (string)dr["qu_sql"];
                }
            }

            // As a last resort, grab some query.
            if (bugSql == null)
            {
                var sql =
                    @"select top 1 qu_id, qu_sql from queries order by case when qu_default = 1 then 1 else 0 end desc";

                var dr = DbUtil.GetDataRow(sql);

                bugSql = (string)dr["qu_sql"];

                if (dr != null)
                {
                    model.QueryId = (int)dr["qu_id"];

                    bugSql = (string)dr["qu_sql"];
                }
            }

            if (bugSql == null)
            {
                return "Error!. No queries available for you to use!<p>Please contact your BugTracker.NET administrator.";
            }

            // replace magic variables
            bugSql = bugSql.Replace("$ME", Convert.ToString(this.security.User.Usid));

            bugSql = Util.AlterSqlPerProjectPermissions(bugSql, this.security);

            if (!this.applicationSettings.UseFullNames)
            {
                // false condition
                bugSql = bugSql.Replace("$fullnames", "0 = 1");
            }
            else
            {
                // true condition
                bugSql = bugSql.Replace("$fullnames", "1 = 1");
            }

            // run the query
            DataSet ds = null;

            try
            {
                ds = DbUtil.GetDataSet(bugSql);

                ViewBag.DataView = new DataView(ds.Tables[0]);
            }
            catch (SqlException e)
            {
                ViewBag.SqlError = e.Message;
                ViewBag.DataView = null;
            }

            // Save it.
            Session["bugs"] = ViewBag.DataView;
            Session["SelectedBugQuery"] = model.QueryId;

            // Save it again.  We use the unfiltered query to determine the
            // values that go in the filter dropdowns.
            if (ds != null)
            {
                Session["bugs_unfiltered"] = ds.Tables[0];
            }
            else
            {
                Session["bugs_unfiltered"] = null;
            }

            return string.Empty;
        }

        private void CallSortAndFilterBuglistDataview(IndexModel model, bool postBack)
        {
            var filterVal = model.Filter;
            var sortVal = model.Sort.ToString();
            var prevSortVal = model.PrevSort.ToString();
            var prevDirVal = model.PrevDir;

            BugList.SortAndFilterBugListDataView((DataView)ViewBag.DataView, postBack, model.Action,
                ref filterVal,
                ref sortVal,
                ref prevSortVal,
                ref prevDirVal);

            model.Filter = filterVal;
            model.Sort = Convert.ToInt32(sortVal);
            model.PrevSort = Convert.ToInt32(prevSortVal);
            model.PrevDir = prevDirVal;
        }

        private void PrintAsHtml(DataView dataView)
        {
            Response.Write("<html><head><link rel='StyleSheet' href='Content/btnet.css' type='text/css'></head><body>");
            Response.Write("<table class=bugt border=1>");
            int col;

            for (col = 1; col < dataView.Table.Columns.Count; col++)
            {
                Response.Write("<td class=bugh>\n");

                if (dataView.Table.Columns[col].ColumnName == "$FLAG")
                {
                    Response.Write("flag");
                }
                else if (dataView.Table.Columns[col].ColumnName == "$SEEN")
                {
                    Response.Write("new");
                }
                else
                {
                    Response.Write(dataView.Table.Columns[col].ColumnName);
                }

                Response.Write("</td>");
            }

            foreach (DataRowView drv in dataView)
            {
                Response.Write("<tr>");
                for (col = 1; col < dataView.Table.Columns.Count; col++)
                {
                    if (dataView.Table.Columns[col].ColumnName == "$FLAG")
                    {
                        var flag = (int)drv[col];
                        var cls = "wht";

                        if (flag == 1)
                        {
                            cls = "red";
                        }
                        else if (flag == 2)
                        {
                            cls = "grn";
                        }

                        Response.Write("<td class=datad><span class=" + cls + ">&nbsp;</span>");
                    }
                    else if (dataView.Table.Columns[col].ColumnName == "$SEEN")
                    {
                        var seen = (int)drv[col];
                        var cls = "old";

                        if (seen == 0)
                        {
                            cls = "new";
                        }
                        else
                        {
                            cls = "old";
                        }

                        Response.Write("<td class=datad><span class=" + cls + ">&nbsp;</span>");
                    }
                    else
                    {
                        var datatype = dataView.Table.Columns[col].DataType;

                        if (Util.IsNumericDataType(datatype))
                        {
                            Response.Write("<td class=bugd align=right>");
                        }
                        else
                        {
                            Response.Write("<td class=bugd>");
                        }

                        // write the data
                        if (string.IsNullOrEmpty(drv[col].ToString()))
                        {
                            Response.Write("&nbsp;");
                        }
                        else
                        {
                            Response.Write(Server.HtmlEncode(drv[col].ToString()).Replace("\n", "<br>"));
                        }
                    }

                    Response.Write("</td>");
                }

                Response.Write("</tr>");
            }

            Response.Write("</table></body></html>");
        }

        private static string PrintTags()
        {
            var stringBuilder = new StringBuilder();
            var tags = Util.MemoryTags ?? new SortedDictionary<string, List<int>>();
            var tagsByCount = new List<TagLabel>();
            var fonts = new Dictionary<string, string>();

            foreach (var s in tags.Keys)
            {
                var tl = new TagLabel
                {
                    Count = tags[s].Count,
                    Label = s
                };

                tagsByCount.Add(tl);
            }

            tagsByCount.Sort(); // sort in descending count order

            float total = tags.Count;
            var soFar = 0.0F;
            var previousCount = -1;
            var previousFont = string.Empty;

            foreach (var tl in tagsByCount)
            {
                soFar++;

                if (tl.Count == previousCount)
                    fonts[tl.Label] = previousFont; // if same count, then same font
                else if (soFar / total < .1)
                    fonts[tl.Label] = "24pt";
                else if (soFar / total < .2)
                    fonts[tl.Label] = "22pt";
                else if (soFar / total < .3)
                    fonts[tl.Label] = "20pt";
                else if (soFar / total < .4)
                    fonts[tl.Label] = "18pt";
                else if (soFar / total < .5)
                    fonts[tl.Label] = "16pt";
                else if (soFar / total < .6)
                    fonts[tl.Label] = "14pt";
                else if (soFar / total < .7)
                    fonts[tl.Label] = "12pt";
                else if (soFar / total < .8)
                    fonts[tl.Label] = "10pt";
                else
                    fonts[tl.Label] = "8pt";

                previousFont = fonts[tl.Label];
                previousCount = tl.Count;
            }

            foreach (var s in tags.Keys)
            {
                stringBuilder.Append("\n<a style='font-size:");
                stringBuilder.Append(fonts[s]);
                stringBuilder.Append(";' href='javascript:opener.append_tag(\"");
                stringBuilder.Append(s.Replace("'", "%27"));
                stringBuilder.Append("\")'>");
                stringBuilder.Append(s);
                stringBuilder.Append("(");
                stringBuilder.Append(tags[s].Count);
                stringBuilder.Append(")</a>&nbsp;&nbsp; ");
            }

            return stringBuilder.ToString();
        }

        private sealed class TagLabel : IComparable<TagLabel>
        {
            public int Count { get; set; }

            public string Label { get; set; }

            public int CompareTo(TagLabel other)
            {
                if (Count > other.Count)
                {
                    return -1;
                }

                if (Count < other.Count)
                {
                    return 1;
                }
                return 0;
            }
        }

        //public void LoadDropdownsForInsert()
        //{
        //    //LoadDropdowns();

        //    // Get the defaults
        //    var sql = "\nselect top 1 pj_id from projects where pj_default = 1 order by pj_name;"; // 0
        //    sql += "\nselect top 1 ct_id from categories where ct_default = 1 order by ct_name;"; // 1
        //    sql += "\nselect top 1 pr_id from priorities where pr_default = 1 order by pr_name;"; // 2
        //    sql += "\nselect top 1 st_id from statuses where st_default = 1 order by st_name;"; // 3
        //    sql += "\nselect top 1 udf_id from user_defined_attribute where udf_default = 1 order by udf_name;"; // 4

        //    var dsDefaults = DbUtil.GetDataSet(sql);

        //    //LoadProjectForInsert(dsDefaults.Tables[0]); /*AndUserDropdown*/
        //    //LoadUserDropdown();

        //    //LoadOtherDropdownsAndSelectDefaults(dsDefaults);
        //}

        public void LoadDropdowns()
        {
            // only show projects where user has permissions
            // 0
            var sql = @"/* drop downs */ select pj_id, pj_name
                from projects
                left outer join project_user_xref on pj_id = pu_project
                and pu_user = $us
                where pj_active = 1
                and isnull(pu_permission_level,$dpl) not in (0, 1)
                order by pj_name;";

            sql = sql.Replace("$us", Convert.ToString(this.security.User.Usid));
            sql = sql.Replace("$dpl", this.applicationSettings.DefaultPermissionLevel.ToString());

            // 1
            sql += "\nselect og_id, og_name from orgs where og_active = 1 order by og_name;";

            // 2
            sql += "\nselect ct_id, ct_name from categories order by ct_sort_seq, ct_name;";

            // 3
            sql += "\nselect pr_id, pr_name from priorities order by pr_sort_seq, pr_name;";

            // 4
            sql += "\nselect st_id, st_name from statuses order by st_sort_seq, st_name;";

            // 5
            sql += "\nselect udf_id, udf_name from user_defined_attribute order by udf_sort_seq, udf_name;";

            // do a batch of sql statements
            var dsDropdowns = DbUtil.GetDataSet(sql);

            ViewBag.Projects = new List<SelectListItem>();

            if (this.applicationSettings.DefaultPermissionLevel == 2)
            {
                ViewBag.Projects.Add(new SelectListItem
                {
                    Value = "0",
                    Text = "[no project]"
                });
            }

            foreach (DataRow row in dsDropdowns.Tables[0].Rows)
            {
                ViewBag.Projects.Add(new SelectListItem
                {
                    Value = row["pj_id"].ToString(),
                    Text = row["pj_name"].ToString()
                });
            }

            ViewBag.Organizations = new List<SelectListItem>();
            ViewBag.Organizations.Add(new SelectListItem
            {
                Value = "0",
                Text = "[no organization]"
            });

            foreach (DataRow row in dsDropdowns.Tables[1].Rows)
            {
                ViewBag.Organizations.Add(new SelectListItem
                {
                    Value = row["og_id"].ToString(),
                    Text = row["og_name"].ToString()
                });
            }

            ViewBag.Categories = new List<SelectListItem>();
            ViewBag.Categories.Add(new SelectListItem
            {
                Value = "0",
                Text = "[no category]"
            });

            foreach (DataRow row in dsDropdowns.Tables[2].Rows)
            {
                ViewBag.Categories.Add(new SelectListItem
                {
                    Value = row["ct_id"].ToString(),
                    Text = row["ct_name"].ToString()
                });
            }

            ViewBag.Priorities = new List<SelectListItem>();
            ViewBag.Priorities.Add(new SelectListItem
            {
                Value = "0",
                Text = "[no priority]"
            });

            foreach (DataRow row in dsDropdowns.Tables[3].Rows)
            {
                ViewBag.Priorities.Add(new SelectListItem
                {
                    Value = row["pr_id"].ToString(),
                    Text = row["pr_name"].ToString()
                });
            }

            ViewBag.Statuses = new List<SelectListItem>();
            ViewBag.Statuses.Add(new SelectListItem
            {
                Value = "0",
                Text = "[no status]"
            });

            foreach (DataRow row in dsDropdowns.Tables[4].Rows)
            {
                ViewBag.Statuses.Add(new SelectListItem
                {
                    Value = row["st_id"].ToString(),
                    Text = row["st_name"].ToString()
                });
            }

            ViewBag.UserDefinedAttributes = new List<SelectListItem>();
            ViewBag.UserDefinedAttributes.Add(new SelectListItem
            {
                Value = "0",
                Text = "[none]"
            });

            foreach (DataRow row in dsDropdowns.Tables[5].Rows)
            {
                ViewBag.UserDefinedAttributes.Add(new SelectListItem
                {
                    Value = row["udf_id"].ToString(),
                    Text = row["udf_name"].ToString()
                });
            }
        }

        public int GetDefaultProject()
        {
            var sql = "\nselect top 1 pj_id from projects where pj_default = 1 order by pj_name;"; // 0
            var projectDefault = DbUtil.GetDataSet(sql);

            // get default values
            var initialProject = (int?)Session["project"];

            // project
            if (this.security.User.ForcedProject != 0)
            {
                initialProject = this.security.User.ForcedProject;
            }

            if (initialProject != null && initialProject != 0)
            {
                return initialProject.Value;
            }
            else
            {
                int defaultValue;
                if (projectDefault.Tables[0].Rows.Count > 0)
                    defaultValue = (int)projectDefault.Tables[0].Rows[0][0];
                else
                    defaultValue = 0;

                return defaultValue;
            }
        }

        public int GetDefaultOrganization()
        {
            return this.security.User.Org;
        }

        public int GetDefaultCategory()
        {
            var sql = "\nselect top 1 ct_id from categories where ct_default = 1 order by ct_name;"; // 1
            var dsDefaults = DbUtil.GetDataSet(sql);

            // category
            int defaultValue;
            if (dsDefaults.Tables[0].Rows.Count > 0)
                defaultValue = (int)dsDefaults.Tables[0].Rows[0][0];
            else
                defaultValue = 0;

            return defaultValue;
        }

        public int GetDefaultPriority()
        {
            var sql = "\nselect top 1 pr_id from priorities where pr_default = 1 order by pr_name;"; // 2
            var dsDefaults = DbUtil.GetDataSet(sql);

            // priority
            int defaultValue;
            if (dsDefaults.Tables[0].Rows.Count > 0)
                defaultValue = (int)dsDefaults.Tables[0].Rows[0][0];
            else
                defaultValue = 0;

            return defaultValue;
        }

        public int GetDefaultStatus()
        {
            var sql = "\nselect top 1 st_id from statuses where st_default = 1 order by st_name;"; // 3
            var dsDefaults = DbUtil.GetDataSet(sql);

            // priority
            int defaultValue;
            if (dsDefaults.Tables[0].Rows.Count > 0)
                defaultValue = (int)dsDefaults.Tables[0].Rows[0][0];
            else
                defaultValue = 0;

            return defaultValue;
        }

        public int GetDefaultUserDefinedAttribute()
        {
            var sql = "\nselect top 1 udf_id from user_defined_attribute where udf_default = 1 order by udf_name;"; // 4
            var dsDefaults = DbUtil.GetDataSet(sql);

            // priority
            int defaultValue;
            if (dsDefaults.Tables[0].Rows.Count > 0)
                defaultValue = (int)dsDefaults.Tables[0].Rows[0][0];
            else
                defaultValue = 0;

            return defaultValue;
        }

        public void LoadUserDropdown()
        {
            // What's selected now?   Save it before we refresh the dropdown.
            var currentValue = string.Empty;

            //if (IsPostBack)
            //{
            //    currentValue = this.assigned_to.SelectedItem.Value;
            //}

            var sql = string.Empty;
            // Load the user dropdown, which changes per project
            // Only users explicitly allowed will be listed
            if (this.applicationSettings.DefaultPermissionLevel == 0)
                sql = @"
            /* users this project */ select us_id, case when $fullnames then us_lastname + ', ' + us_firstname else us_username end us_username
            from users
            inner join orgs on us_org = og_id
            where us_active = 1
            and og_can_be_assigned_to = 1
            and ($og_other_orgs_permission_level <> 0 or $og_id = og_id or (og_external_user = 0 and $og_can_assign_to_internal_users))
            and us_id in
                (select pu_user from project_user_xref
                    where pu_project = $pj
                    and pu_permission_level <> 0)
            order by us_username; ";
            // Only users explictly DISallowed will be omitted
            else
                sql = @"
            /* users this project */ select us_id, case when $fullnames then us_lastname + ', ' + us_firstname else us_username end us_username
            from users
            inner join orgs on us_org = og_id
            where us_active = 1
            and og_can_be_assigned_to = 1
            and ($og_other_orgs_permission_level <> 0 or $og_id = og_id or (og_external_user = 0 and $og_can_assign_to_internal_users))
            and us_id not in
                (select pu_user from project_user_xref
                    where pu_project = $pj
                    and pu_permission_level = 0)
            order by us_username; ";

            if (!this.applicationSettings.UseFullNames)
                // false condition
                sql = sql.Replace("$fullnames", "0 = 1");
            else
                // true condition
                sql = sql.Replace("$fullnames", "1 = 1");

            var defaultProjectId = GetDefaultProject();

            //if (this.project.SelectedItem != null)
            sql = sql.Replace("$pj", defaultProjectId.ToString()/*this.project.SelectedItem.Value*/);
            //else
            //    sql = sql.Replace("$pj", "0");

            sql = sql.Replace("$og_id", Convert.ToString(this.security.User.Org));
            sql = sql.Replace("$og_other_orgs_permission_level",
                Convert.ToString((int)this.security.User.OtherOrgsPermissionLevel));

            if (this.security.User.CanAssignToInternalUsers)
                sql = sql.Replace("$og_can_assign_to_internal_users", "1 = 1");
            else
                sql = sql.Replace("$og_can_assign_to_internal_users", "0 = 1");

            var dtUsers = DbUtil.GetDataSet(sql).Tables[0];

            ViewBag.Users = new List<SelectListItem>();
            ViewBag.Users.Add(new SelectListItem
            {
                Value = "0",
                Text = "[not assigned]"
            });

            foreach (DataRow row in dtUsers.Rows)
            {
                ViewBag.Users.Add(new SelectListItem
                {
                    Value = row["us_id"].ToString(),
                    Text = row["us_username"].ToString()
                });
            }

            //this.assigned_to.DataSource = new DataView(DtUsers);
            //this.assigned_to.DataTextField = "us_username";
            //this.assigned_to.DataValueField = "us_id";
            //this.assigned_to.DataBind();
            //this.assigned_to.Items.Insert(0, new ListItem("[not assigned]", "0"));

            // It can happen that the user in the db is not listed in the dropdown, because of a subsequent change in permissions.
            // Since that user IS the user associated with the bug, let's force it into the dropdown.
            //if (this.Id != 0) // if existing bug
            //    if (this.prev_assigned_to.Value != "0")
            //    {
            //        // see if already in the dropdown.
            //        var userInDropdown = false;
            //        foreach (ListItem li in this.assigned_to.Items)
            //            if (li.Value == this.prev_assigned_to.Value)
            //            {
            //                userInDropdown = true;
            //                break;
            //            }

            //        // Add to the list, even if permissions don't allow it now, because, in the past, they did allow it.
            //        if (!userInDropdown)
            //            this.assigned_to.Items.Insert(1,
            //                new ListItem(this.prev_assigned_to_username.Value, this.prev_assigned_to.Value));
            //    }

            //// At this point, all the users we need are in the dropdown.
            //// Now selected the selected.
            //if (string.IsNullOrEmpty(currentValue)) currentValue = this.prev_assigned_to.Value;

            //// Select the user.  We are either restoring the previous selection
            //// or selecting what was in the database.
            //if (currentValue != "0")
            //    foreach (ListItem li in this.assigned_to.Items)
            //        if (li.Value == currentValue)
            //            li.Selected = true;
            //        else
            //            li.Selected = false;

            //// if nothing else is selected. select the default user for the project
            //if (this.assigned_to.SelectedItem.Value == "0")
            //{
            //    var projectDefaultUser = 0;
            //    if (this.project.SelectedItem != null)
            //    {
            //        // get the default user of the project
            //        projectDefaultUser = Util.GetDefaultUser(Convert.ToInt32(this.project.SelectedItem.Value));

            //        if (projectDefaultUser != 0)
            //            foreach (ListItem li in this.assigned_to.Items)
            //                if (Convert.ToInt32(li.Value) == projectDefaultUser)
            //                    li.Selected = true;
            //                else
            //                    li.Selected = false;
            //    }
            //}
        }

        public static string GetCustomColDefaultValue(object o)
        {
            var defaultval = Convert.ToString(o);

            // populate the sql default value of a custom field
            if (defaultval.Length > 2)
                if (defaultval[0] == '('
                    && defaultval[defaultval.Length - 1] == ')')
                {
                    var defaultvalSql = "select " + defaultval.Substring(1, defaultval.Length - 2);
                    defaultval = Convert.ToString(DbUtil.ExecuteScalar(defaultvalSql));
                }

            return defaultval;
        }

        public static bool DoesAssignedToHavePermissionForOrg(int assignedTo, int org)
        {
            if (assignedTo < 1) return true;

            var sql = @"
                /* validate org versus assigned_to */
                select case when og_other_orgs_permission_level <> 0
                or $bg_org = og_id then 1
                else 0 end as [answer]
                from users
                inner join orgs on us_org = og_id
                where us_id = @us_id";

            sql = sql.Replace("@us_id", Convert.ToString(assignedTo));
            sql = sql.Replace("$bg_org", Convert.ToString(org));

            var allowed = DbUtil.ExecuteScalar(sql);

            if (allowed != null && Convert.ToInt32(allowed) == 1)
                return true;
            return false;
        }

        public void GetCookieValuesForShowHideToggles()
        {
            var cookie = Request.Cookies["images_inline"];
            if (cookie == null || cookie.Value == "0")
                ViewBag.ImagesInline = false;
            else
                ViewBag.ImagesInline = true;

            cookie = Request.Cookies["history_inline"];
            if (cookie == null || cookie.Value == "0")
                ViewBag.HistoryInline = false;
            else
                ViewBag.HistoryInline = true;
        }

        public void FormatPrevNextBug(int id)
        {
            // for next/prev bug links
            var dvBugs = (DataView)Session["bugs"];

            if (dvBugs != null)
            {
                var prevBug = 0;
                var nextBug = 0;
                var thisBugFound = false;

                // read through the list of bugs looking for the one that matches this one
                var positionInList = 0;
                var savePositionInList = 0;
                foreach (DataRowView drv in dvBugs)
                {
                    positionInList++;
                    if (thisBugFound)
                    {
                        // step 3 - get the next bug - we're done
                        nextBug = (int)drv[1];
                        break;
                    }

                    if (id == (int)drv[1])
                    {
                        // step 2 - we found this - set switch
                        savePositionInList = positionInList;
                        thisBugFound = true;
                    }
                    else
                    {
                        // step 1 - save the previous just in case the next one IS this bug
                        prevBug = (int)drv[1];
                    }
                }

                var prevNextLink = string.Empty;

                if (thisBugFound)
                {
                    if (prevBug != 0)
                        prevNextLink =
                            "&nbsp;&nbsp;&nbsp;&nbsp;<a class=warn href=" + VirtualPathUtility.ToAbsolute("~/Bug/Update/")
                            + Convert.ToString(prevBug)
                            + "><img src=" + VirtualPathUtility.ToAbsolute("~/Content/images/arrow_up.png") + " border=0 align=top>prev</a>";
                    else
                        prevNextLink = "&nbsp;&nbsp;&nbsp;&nbsp;<span class=gray_link>prev</span>";

                    if (nextBug != 0)
                        prevNextLink +=
                            "&nbsp;&nbsp;&nbsp;&nbsp;<a class=warn href=" + VirtualPathUtility.ToAbsolute("~/Bug/Update/")
                            + Convert.ToString(nextBug)
                            + ">next<img src=" + VirtualPathUtility.ToAbsolute("~/Content/images/arrow_down.png") + " border=0 align=top></a>";
                    else
                        prevNextLink += "&nbsp;&nbsp;&nbsp;&nbsp;<span class=gray_link>next</span>";

                    prevNextLink += "&nbsp;&nbsp;&nbsp;<span class=smallnote>"
                                      + Convert.ToString(savePositionInList)
                                      + " of "
                                      + Convert.ToString(dvBugs.Count)
                                      + "</span>";

                    ViewBag.PrevNext = prevNextLink;
                }
            }
        }

        public void LoadFromDropdown(DataRow dr, bool projectFirst)
        {
            // format from dropdown
            var projectEmail = dr["pj_pop3_email_from"].ToString();
            var usEmail = dr["us_email"].ToString();
            var usFirstname = dr["us_firstname"].ToString();
            var usLastname = dr["us_lastname"].ToString();

            ViewBag.Froms = new List<SelectListItem>();
            ViewBag.Froms.Add(new SelectListItem
            {
                Value = "0",
                Text = "[not assigned]"
            });

            if (projectFirst)
            {
                if (!string.IsNullOrEmpty(projectEmail))
                {
                    ViewBag.Froms.Add(new SelectListItem
                    {
                        Value = projectEmail,
                        Text = projectEmail
                    });

                    if (!string.IsNullOrEmpty(usFirstname) && !string.IsNullOrEmpty(usLastname))
                        ViewBag.Froms.Add(new SelectListItem
                        {
                            Value = "\"" + usFirstname + " " + usLastname + "\" <" + projectEmail + ">",
                            Text = "\"" + usFirstname + " " + usLastname + "\" <" + projectEmail + ">"
                        });
                }

                if (!string.IsNullOrEmpty(usEmail))
                {
                    ViewBag.Froms.Add(new SelectListItem
                    {
                        Value = usEmail,
                        Text = usEmail
                    });

                    if (!string.IsNullOrEmpty(usFirstname) && !string.IsNullOrEmpty(usLastname))
                        ViewBag.Froms.Add(new SelectListItem
                        {
                            Value = "\"" + usFirstname + " " + usLastname + "\" <" + usEmail + ">",
                            Text = "\"" + usFirstname + " " + usLastname + "\" <" + usEmail + ">"
                        });
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(usEmail))
                {
                    ViewBag.Froms.Add(new SelectListItem
                    {
                        Value = usEmail,
                        Text = usEmail
                    });

                    if (!string.IsNullOrEmpty(usFirstname) && !string.IsNullOrEmpty(usLastname))
                        ViewBag.Froms.Add(new SelectListItem
                        {
                            Value = "\"" + usFirstname + " " + usLastname + "\" <" + usEmail + ">",
                            Text = "\"" + usFirstname + " " + usLastname + "\" <" + usEmail + ">"
                        });
                }

                if (!string.IsNullOrEmpty(projectEmail))
                {
                    ViewBag.Froms.Add(new SelectListItem
                    {
                        Value = projectEmail,
                        Text = projectEmail
                    });

                    if (!string.IsNullOrEmpty(usFirstname) && !string.IsNullOrEmpty(usLastname))
                        ViewBag.Froms.Add(new SelectListItem
                        {
                            Value = "\"" + usFirstname + " " + usLastname + "\" <" + projectEmail + ">",
                            Text = "\"" + usFirstname + " " + usLastname + "\" <" + projectEmail + ">"
                        });
                }
            }

            if (ViewBag.Froms.Count == 0)
            {
                ViewBag.Froms.Add(new SelectListItem
                {
                    Value = "[none]",
                    Text = "[none]"
                });
            }
        }

        public void ValidateSendEmail(SendEmailModel model)
        {
            if (string.IsNullOrEmpty(model.To))
            {
                ModelState.AddModelError(nameof(SendEmailModel.To), "\"To\" is required.");
            }
            else
            {
                try
                {
                    var dummyMsg = new MailMessage();
                    Email.AddAddressesToEmail(dummyMsg, model.To, Email.AddrType.To);
                }
                catch
                {
                    ModelState.AddModelError(nameof(SendEmailModel.To), "\"To\" is not in a valid format. Separate multiple addresses with commas.");
                }
            }

            if (!string.IsNullOrEmpty(model.CC))
                try
                {
                    var dummyMsg = new MailMessage();
                    Email.AddAddressesToEmail(dummyMsg, model.CC, Email.AddrType.Cc);
                }
                catch
                {
                    ModelState.AddModelError(nameof(SendEmailModel.CC), "\"CC\" is not in a valid format. Separate multiple addresses with commas.");
                }

            if (model.From == "[none]")
            {
                ModelState.AddModelError(nameof(SendEmailModel.From), "\"From\" is required.  Use \"settings\" to fix.");
            }

            if (string.IsNullOrEmpty(model.Subject))
            {
                ModelState.AddModelError(nameof(SendEmailModel.Subject), "\"Subject\" is required.");
            }
        }

        public int[] HandleAttachments(int commentId, ISecurity security, SendEmailModel model)
        {
            var attachments = new ArrayList();

            var filename = Path.GetFileName(model.Attachment.FileName);
            if (!string.IsNullOrEmpty(filename))
            {
                //add attachment
                var maxUploadSize = this.applicationSettings.MaxUploadSize;
                var contentLength = model.Attachment.ContentLength;
                if (contentLength > maxUploadSize)
                {
                    ModelState.AddModelError(nameof(SendEmailModel.Attachment), $"File exceeds maximum allowed length of {maxUploadSize}.");
                    return null;
                }

                if (contentLength == 0)
                {
                    ModelState.AddModelError(nameof(SendEmailModel.Attachment), "No data was uploaded.");
                    return null;
                }

                var bpId = Bug.InsertPostAttachment(security,
                    Convert.ToInt32(model.BugId), model.Attachment.InputStream,
                    contentLength,
                    filename,
                    "email attachment", model.Attachment.ContentType,
                    commentId,
                    false, false);

                attachments.Add(bpId);
            }

            //attachments to forward

            foreach (var attachment in model.Attachments)
            //if (itemAttachment.Selected)
            {
                var bpId = Convert.ToInt32(attachment);

                Bug.InsertPostAttachmentCopy(security, model.BugId, bpId, "email attachment", commentId, false, false);

                attachments.Add(bpId);
            }

            return (int[])attachments.ToArray(typeof(int));
        }

        public void PutAddresses()
        {
            var dictUsersForThisProject = new Dictionary<int, int>();
            string sql;

            // list of email addresses to use.
            if (Session["email_addresses"] == null)
            {
                if (ViewBag.Project > -1)
                {
                    if (ViewBag.Project == 0)
                    {
                        sql = @"select us_id
                            from users
                            where us_active = 1
                            and len(us_email) > 0
                            order by us_email";
                    }
                    else
                    {
                        // Only users explicitly allowed will be listed
                        if (this.applicationSettings.DefaultPermissionLevel == 0)
                            sql = @"select us_id
                                from users
                                where us_active = 1
                                and len(us_email) > 0
                                and us_id in
                                    (select pu_user from project_user_xref
                                    where pu_project = $pr
                                    and pu_permission_level <> 0)
                                order by us_email";
                        // Only users explictly DISallowed will be omitted
                        else
                            sql = @"select us_id
                                from users
                                where us_active = 1
                                and len(us_email) > 0
                                and us_id not in
                                    (select pu_user from project_user_xref
                                    where pu_project = $pr
                                    and pu_permission_level = 0)
                                order by us_email";
                    }

                    sql = sql.Replace("$pr", Convert.ToString(ViewBag.Project));
                    var dsUsersForThisProject = DbUtil.GetDataSet(sql);

                    // remember the users for this this project
                    foreach (DataRow dr in dsUsersForThisProject.Tables[0].Rows)
                        dictUsersForThisProject[(int)dr[0]] = 1;
                }

                var dtRelatedUsers = Util.GetRelatedUsers(this.security, true); // force full names
                                                                                // let's sort by email
                var dvRelatedUsers = new DataView(dtRelatedUsers);
                dvRelatedUsers.Sort = "us_email";

                var sb = new StringBuilder();

                foreach (DataRowView drvEmail in dvRelatedUsers)
                    if (dictUsersForThisProject.ContainsKey((int)drvEmail["us_id"]))
                    {
                        var email = (string)drvEmail["us_email"];
                        var username = (string)drvEmail["us_username"];

                        sb.Append("<option style='padding: 3px;'>");

                        if (username != "" && username != email)
                        {
                            sb.Append("\"");
                            sb.Append(username);
                            sb.Append("\"&lt;");
                            sb.Append(email);
                            sb.Append("&gt;");
                        }
                        else
                        {
                            sb.Append(email);
                        }
                        sb.Append("</option>");
                    }

                Session["email_addresses"] = sb.ToString();
            }

            ViewBag.EmailAddresses = Session["email_addresses"];
        }
    }
}