/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Controllers
{
    using Core;
    using Models;
    using Models.Task;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Web.Mvc;
    using System.Web.UI;

    [Authorize]
    [OutputCache(Location = OutputCacheLocation.None)]
    public class TaskController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;
        private readonly IAuthenticate authenticate;

        public TaskController(
            IApplicationSettings applicationSettings,
            ISecurity security,
            IAuthenticate authenticate)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
            this.authenticate = authenticate;
        }

        [HttpGet]
        public ActionResult Index(int? bugId)
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanViewTasks;

            if (!isAuthorized)
            {
                return Content("You are not allowed to view tasks.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - tasks",
                SelectedItem = "bugs"
            };

            var model = new SortableTableModel
            {
                HtmlEncode = false
            };

            if (bugId == null)
            {
                ViewBag.ShowToolbar = false;

                model.DataTable = Util.GetAllTasks(this.security, 0).Tables[0];

                return View(model);
            }

            var permissionLevel = Bug.GetBugPermissionLevel(bugId.Value, this.security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                return Content("You are not allowed to view tasks for this item");
            }

            var sql = "select tsk_id [id],";

            if (permissionLevel == SecurityPermissionLevel.PermissionAll
                && !this.security.User.IsGuest
                && (this.security.User.IsAdmin || this.security.User.CanEditTasks))
            {
                sql += @"
                    '<a href=/Task/Update?bugid=$bugid&id=' + convert(varchar,tsk_id) + '>edit</a>'   [$no_sort_edit],
                    '<a href=/Task/Delete?bugId=$bugid&id=' + convert(varchar,tsk_id) + '>delete</a>' [$no_sort_delete],";
            }

            sql += "tsk_description [description]";

            if (this.applicationSettings.ShowTaskAssignedTo)
            {
                sql += ",us_username [assigned to]";
            }

            if (this.applicationSettings.ShowTaskPlannedStartDate)
            {
                sql += ", tsk_planned_start_date [planned start]";
            }

            if (this.applicationSettings.ShowTaskActualStartDate)
            {
                sql += ", tsk_actual_start_date [actual start]";
            }

            if (this.applicationSettings.ShowTaskPlannedEndDate)
            {
                sql += ", tsk_planned_end_date [planned end]";
            }

            if (this.applicationSettings.ShowTaskActualEndDate)
            {
                sql += ", tsk_actual_end_date [actual end]";
            }

            if (this.applicationSettings.ShowTaskPlannedDuration)
            {
                sql += ", tsk_planned_duration [planned<br>duration]";
            }

            if (this.applicationSettings.ShowTaskActualDuration)
            {
                sql += ", tsk_actual_duration  [actual<br>duration]";
            }

            if (this.applicationSettings.ShowTaskDurationUnits)
            {
                sql += ", tsk_duration_units [duration<br>units]";
            }

            if (this.applicationSettings.ShowTaskPercentComplete)
            {
                sql += ", tsk_percent_complete [percent<br>complete]";
            }

            if (this.applicationSettings.ShowTaskStatus)
            {
                sql += ", st_name  [status]";
            }

            if (this.applicationSettings.ShowTaskSortSequence)
            {
                sql += ", tsk_sort_sequence  [seq]";
            }

            sql += @"
                from bug_tasks 
                left outer join statuses on tsk_status = st_id
                left outer join users on tsk_assigned_to_user = us_id
                where tsk_bug = $bugid 
                order by tsk_sort_sequence, tsk_id";

            sql = sql.Replace("$bugid", Convert.ToString(bugId.Value));

            model.DataTable = DbUtil.GetDataSet(sql).Tables[0];

            ViewBag.BugId = bugId;
            ViewBag.ShowToolbar = (permissionLevel == SecurityPermissionLevel.PermissionAll)
                && (this.security.User.IsAdmin || this.security.User.CanEditTasks);

            return View(model);
        }

        [HttpGet]
        [Authorize(Roles = ApplicationRoles.Member)]
        public ActionResult Create(int bugId)
        {
            var permissionLevel = Bug.GetBugPermissionLevel(bugId, this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                return Content("You are not allowed to edit tasks for this item");
            }

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditTasks;

            if (!isAuthorized)
            {
                return Content("You are not allowed to edit tasks.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - create task",
                SelectedItem = "bugs"
            };

            LoadUsersDropdowns(bugId);

            ////add
            var model = new EditModel
            {
                BugId = bugId,
                PlannedStartHour = this.applicationSettings.TaskDefaultHour,
                PlannedEndHour = this.applicationSettings.TaskDefaultHour,
                ActualStartHour = this.applicationSettings.TaskDefaultHour,
                ActualEndHour = this.applicationSettings.TaskDefaultHour,
                DurationUnitId = this.applicationSettings.TaskDefaultDurationUnits,
                //StatusId = this.applicationSettings.TaskDefaultStatus
            };

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ApplicationRoles.Member)]
        public ActionResult Create(EditModel model)
        {
            var permissionLevel = Bug.GetBugPermissionLevel(model.BugId, this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                return Content("You are not allowed to edit tasks for this item");
            }

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditTasks;

            if (!isAuthorized)
            {
                return Content("You are not allowed to edit tasks.");
            }

            if (!string.IsNullOrEmpty(model.PercentComplete))
            {
                if (!Util.IsInt(model.PercentComplete))
                {
                    ModelState.AddModelError(nameof(EditModel.PercentComplete), "Percent Complete must be from 0 to 100.");
                }
                else
                {
                    var percentCompleteInt = Convert.ToInt32(model.PercentComplete);

                    if (!(percentCompleteInt >= 0 && percentCompleteInt <= 100))
                    {
                        ModelState.AddModelError(nameof(EditModel.PercentComplete), "Percent Complete must be from 0 to 100.");
                    }
                }
            }

            if (!string.IsNullOrEmpty(model.PlannedDuration))
            {
                var err = Util.IsValidDecimal("Planned Duration", model.PlannedDuration, 4, 2);

                if (!string.IsNullOrEmpty(err))
                {
                    ModelState.AddModelError(nameof(EditModel.PlannedDuration), err);
                }
            }

            if (!string.IsNullOrEmpty(model.ActualDuration))
            {
                var err = Util.IsValidDecimal("Actual Duration", model.ActualDuration, 4, 2);

                if (!string.IsNullOrEmpty(err))
                {
                    ModelState.AddModelError(nameof(EditModel.ActualDuration), err);
                }
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Task was not created.");

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - create task",
                    SelectedItem = "bugs"
                };

                LoadUsersDropdowns(model.BugId);

                return View("Edit", model);
            }

            var sql = @"
                insert into bug_tasks (
                tsk_bug,
                tsk_created_user,
                tsk_created_date,
                tsk_last_updated_user,
                tsk_last_updated_date,
                tsk_assigned_to_user,
                tsk_planned_start_date,
                tsk_actual_start_date,
                tsk_planned_end_date,
                tsk_actual_end_date,
                tsk_planned_duration,
                tsk_actual_duration,
                tsk_duration_units,
                tsk_percent_complete,
                tsk_status,
                tsk_sort_sequence,
                tsk_description
                )
                values (
                $tsk_bug,
                $tsk_created_user,
                getdate(),
                $tsk_last_updated_user,
                getdate(),
                $tsk_assigned_to_user,
                '$tsk_planned_start_date',
                '$tsk_actual_start_date',
                '$tsk_planned_end_date',
                '$tsk_actual_end_date',
                $tsk_planned_duration,
                $tsk_actual_duration,
                N'$tsk_duration_units',
                $tsk_percent_complete,
                $tsk_status,
                $tsk_sort_sequence,
                N'$tsk_description'
                )

                declare @tsk_id int
                select @tsk_id = scope_identity()

                insert into bug_posts
                (bp_bug, bp_user, bp_date, bp_comment, bp_type)
                values($tsk_bug, $tsk_last_updated_user, getdate(), N'added task ' + convert(varchar, @tsk_id), 'update')";

            sql = sql.Replace("$tsk_created_user", Convert.ToString(this.security.User.Usid));

            sql = sql.Replace("$tsk_bug", Convert.ToString(model.BugId));
            sql = sql.Replace("$tsk_last_updated_user", Convert.ToString(this.security.User.Usid));

            sql = sql.Replace("$tsk_planned_start_date", FormatDateHourMin(model.PlannedStartDate, model.PlannedStartHour, model.PlannedStartMinute));
            sql = sql.Replace("$tsk_actual_start_date", FormatDateHourMin(model.ActualStartDate, model.ActualStartHour, model.ActualStartMinute));
            sql = sql.Replace("$tsk_planned_end_date", FormatDateHourMin(model.PlannedEndDate, model.PlannedEndHour, model.PlannedEndMinute));
            sql = sql.Replace("$tsk_actual_end_date", FormatDateHourMin(model.ActualEndDate, model.ActualEndHour, model.ActualEndMinute));

            sql = sql.Replace("$tsk_planned_duration", FormatDecimalForDb(model.PlannedDuration));
            sql = sql.Replace("$tsk_actual_duration", FormatDecimalForDb(model.ActualDuration));
            sql = sql.Replace("$tsk_percent_complete", FormatNumberForDb(model.PercentComplete));
            sql = sql.Replace("$tsk_status", model.StatusId.ToString());
            sql = sql.Replace("$tsk_sort_sequence", model.SortSequence.ToString());
            sql = sql.Replace("$tsk_assigned_to_user", model.UserId.ToString());
            sql = sql.Replace("$tsk_description", model.Name);
            sql = sql.Replace("$tsk_duration_units", model.DurationUnitId);

            DbUtil.ExecuteNonQuery(sql);

            Bug.SendNotifications(Bug.Update, model.BugId, this.security);

            return RedirectToAction(nameof(Index), new { bugId = model.BugId });
        }

        [HttpGet]
        [Authorize(Roles = ApplicationRoles.Member)]
        public ActionResult Update(int id, int bugId)
        {
            var permissionLevel = Bug.GetBugPermissionLevel(bugId, this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                return Content("You are not allowed to edit tasks for this item");
            }

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditTasks;

            if (!isAuthorized)
            {
                return Content("You are not allowed to edit tasks.");
            }

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - update task",
                SelectedItem = "bugs"
            };

            LoadUsersDropdowns(bugId);

            // Get this entry's data from the db and fill in the form
            var sql = @"select * from bug_tasks where tsk_id = $tsk_id and tsk_bug = $bugid";

            sql = sql.Replace("$tsk_id", Convert.ToString(id));
            sql = sql.Replace("$bugid", Convert.ToString(bugId));

            var dr = DbUtil.GetDataRow(sql);
            var model = new EditModel
            {
                Id = id,
                BugId = bugId,
                Name = Convert.ToString(dr["tsk_description"]),
                UserId = (int)dr["tsk_assigned_to_user"],

                PlannedDuration = Util.FormatDbValue(dr["tsk_planned_duration"]),
                ActualDuration = Util.FormatDbValue(dr["tsk_actual_duration"]),

                DurationUnitId = (string)dr["tsk_duration_units"],
                PercentComplete = Convert.ToString(dr["tsk_percent_complete"]),
                StatusId = (int)dr["tsk_status"],
                SortSequence = (int)dr["tsk_sort_sequence"]
            };


            //load_date_hour_min(this.planned_start_date, this.planned_start_hour, this.planned_start_min, dr["tsk_planned_start_date"]);
            var result = LoadDateHourMin(dr["tsk_planned_start_date"]);

            model.PlannedStartDate = result.date;
            model.PlannedStartHour = result.hour;
            model.PlannedStartMinute = result.minute;

            result = LoadDateHourMin(dr["tsk_actual_start_date"]);

            model.ActualStartDate = result.date;
            model.ActualStartHour = result.hour;
            model.ActualStartMinute = result.minute;

            result = LoadDateHourMin(dr["tsk_planned_end_date"]);

            model.PlannedEndDate = result.date;
            model.PlannedEndHour = result.hour;
            model.PlannedEndMinute = result.minute;

            result = LoadDateHourMin(dr["tsk_actual_end_date"]);

            model.ActualEndDate = result.date;
            model.ActualEndHour = result.hour;
            model.ActualEndMinute = result.minute;

            return View("Edit", model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ApplicationRoles.Member)]
        public ActionResult Update(EditModel model)
        {
            var permissionLevel = Bug.GetBugPermissionLevel(model.BugId, this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                return Content("You are not allowed to edit tasks for this item");
            }

            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanEditTasks;

            if (!isAuthorized)
            {
                return Content("You are not allowed to edit tasks.");
            }

            if (!string.IsNullOrEmpty(model.PercentComplete))
            {
                if (!Util.IsInt(model.PercentComplete))
                {
                    ModelState.AddModelError(nameof(EditModel.PercentComplete), "Percent Complete must be from 0 to 100.");
                }
                else
                {
                    var percentCompleteInt = Convert.ToInt32(model.PercentComplete);

                    if (!(percentCompleteInt >= 0 && percentCompleteInt <= 100))
                    {
                        ModelState.AddModelError(nameof(EditModel.PercentComplete), "Percent Complete must be from 0 to 100.");
                    }
                }
            }

            if (!string.IsNullOrEmpty(model.PlannedDuration))
            {
                var err = Util.IsValidDecimal("Planned Duration", model.PlannedDuration, 4, 2);

                if (!string.IsNullOrEmpty(err))
                {
                    ModelState.AddModelError(nameof(EditModel.PlannedDuration), err);
                }
            }

            if (!string.IsNullOrEmpty(model.ActualDuration))
            {
                var err = Util.IsValidDecimal("Actual Duration", model.ActualDuration, 4, 2);

                if (!string.IsNullOrEmpty(err))
                {
                    ModelState.AddModelError(nameof(EditModel.ActualDuration), err);
                }
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError(string.Empty, "Task was not created.");

                ViewBag.Page = new PageModel
                {
                    ApplicationSettings = this.applicationSettings,
                    Security = this.security,
                    Title = $"{this.applicationSettings.AppTitle} - create task",
                    SelectedItem = "bugs"
                };

                LoadUsersDropdowns(model.BugId);

                return View("Edit", model);
            }

            var sql = @"
                update bug_tasks set
                tsk_last_updated_user = $tsk_last_updated_user,
                tsk_last_updated_date = getdate(),
                tsk_assigned_to_user = $tsk_assigned_to_user,
                tsk_planned_start_date = '$tsk_planned_start_date',
                tsk_actual_start_date = '$tsk_actual_start_date',
                tsk_planned_end_date = '$tsk_planned_end_date',
                tsk_actual_end_date = '$tsk_actual_end_date',
                tsk_planned_duration = $tsk_planned_duration,
                tsk_actual_duration = $tsk_actual_duration,
                tsk_duration_units = N'$tsk_duration_units',
                tsk_percent_complete = $tsk_percent_complete,
                tsk_status = $tsk_status,
                tsk_sort_sequence = $tsk_sort_sequence,
                tsk_description = N'$tsk_description'
                where tsk_id = $tsk_id
                
                insert into bug_posts
                (bp_bug, bp_user, bp_date, bp_comment, bp_type)
                values($tsk_bug, $tsk_last_updated_user, getdate(), N'updated task $tsk_id', 'update')";

            sql = sql.Replace("$tsk_id", Convert.ToString(model.Id));

            sql = sql.Replace("$tsk_bug", Convert.ToString(model.BugId));
            sql = sql.Replace("$tsk_last_updated_user", Convert.ToString(this.security.User.Usid));

            sql = sql.Replace("$tsk_planned_start_date", FormatDateHourMin(model.PlannedStartDate, model.PlannedStartHour, model.PlannedStartMinute));
            sql = sql.Replace("$tsk_actual_start_date", FormatDateHourMin(model.ActualStartDate, model.ActualStartHour, model.ActualStartMinute));
            sql = sql.Replace("$tsk_planned_end_date", FormatDateHourMin(model.PlannedEndDate, model.PlannedEndHour, model.PlannedEndMinute));
            sql = sql.Replace("$tsk_actual_end_date", FormatDateHourMin(model.ActualEndDate, model.ActualEndHour, model.ActualEndMinute));

            sql = sql.Replace("$tsk_planned_duration", FormatDecimalForDb(model.PlannedDuration));
            sql = sql.Replace("$tsk_actual_duration", FormatDecimalForDb(model.ActualDuration));
            sql = sql.Replace("$tsk_percent_complete", FormatNumberForDb(model.PercentComplete));
            sql = sql.Replace("$tsk_status", model.StatusId.ToString());
            sql = sql.Replace("$tsk_sort_sequence", model.SortSequence.ToString());
            sql = sql.Replace("$tsk_assigned_to_user", model.UserId.ToString());
            sql = sql.Replace("$tsk_description", model.Name);
            sql = sql.Replace("$tsk_duration_units", model.DurationUnitId);

            DbUtil.ExecuteNonQuery(sql);

            Bug.SendNotifications(Bug.Update, model.BugId, this.security);

            return RedirectToAction(nameof(Index), new { bugId = model.BugId });
        }

        [HttpGet]
        [Authorize(Roles = ApplicationRoles.Administrator)]
        public ActionResult Delete(int id, int bugId)
        {
            var permissionLevel = Bug.GetBugPermissionLevel(bugId, this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                return Content("You are not allowed to edit this item");
            }

            var sql = @"select tsk_description from bug_tasks where tsk_id = $tsk_id and tsk_bug = $bugid";

            sql = sql.Replace("$tsk_id", id.ToString());
            sql = sql.Replace("$bugid", bugId.ToString());

            var dr = DbUtil.GetDataRow(sql);

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - delete task",
                SelectedItem = ApplicationSettings.PluralBugLabelDefault
            };

            var model = new DeleteModel
            {
                Id = id,
                BugId = bugId,
                Name = (string)dr["tsk_description"]
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = ApplicationRoles.Administrator)]
        public ActionResult Delete(DeleteModel model)
        {
            var permissionLevel = Bug.GetBugPermissionLevel(model.BugId, this.security);

            if (permissionLevel != SecurityPermissionLevel.PermissionAll)
            {
                return Content("You are not allowed to edit this item");
            }

            var sql = @"delete bug_tasks where tsk_id = $tsk_id and tsk_bug = $bugid";

            sql = sql.Replace("$tsk_id", model.Id.ToString());
            sql = sql.Replace("$bugid", model.BugId.ToString());

            DbUtil.ExecuteNonQuery(sql);

            return RedirectToAction(nameof(Index), new { bugId = model.BugId });
        }

        [HttpGet]
        public void Export()
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanViewTasks;

            if (!isAuthorized)
            {
                Response.Write("You are not allowed to view tasks.");
            }
            else
            {
                var tasks = Util.GetAllTasks(this.security, 0);
                var dv = new DataView(tasks.Tables[0]);

                Util.PrintAsExcel(System.Web.HttpContext.Current.Response, dv);
            }
        }

        private void LoadUsersDropdowns(int bugId)
        {
            // What's selected now?   Save it before we refresh the dropdown.
            var currentValue = string.Empty;

            //TODO
            //if (IsPostBack) currentValue = this.assigned_to.SelectedItem.Value;

            var sql = @"
                declare @project int
                declare @assigned_to int
                select @project = bg_project, @assigned_to = bg_assigned_to_user from bugs where bg_id = $bg_id";

            // Load the user dropdown, which changes per project
            // Only users explicitly allowed will be listed
            if (this.applicationSettings.DefaultPermissionLevel == 0)
            {
                sql += @"
                    /* users this project */ select us_id, case when $fullnames then us_lastname + ', ' + us_firstname else us_username end us_username
                    from users
                    inner join orgs on us_org = og_id
                    where us_active = 1
                    and og_can_be_assigned_to = 1
                    and ($og_other_orgs_permission_level <> 0 or $og_id = og_id or og_external_user = 0)
                    and us_id in
                        (select pu_user from project_user_xref
                            where pu_project = @project
                            and pu_permission_level <> 0)
                    order by us_username; ";
            }
            // Only users explictly DISallowed will be omitted
            else
            {
                sql += @"
                    /* users this project */ select us_id, case when $fullnames then us_lastname + ', ' + us_firstname else us_username end us_username
                    from users
                    inner join orgs on us_org = og_id
                    where us_active = 1
                    and og_can_be_assigned_to = 1
                    and ($og_other_orgs_permission_level <> 0 or $og_id = og_id or og_external_user = 0)
                    and us_id not in
                        (select pu_user from project_user_xref
                            where pu_project = @project
                            and pu_permission_level = 0)
                    order by us_username; ";
            }

            sql += "\nselect st_id, st_name from statuses order by st_sort_seq, st_name";

            sql += "\nselect isnull(@assigned_to,0) ";

            sql = sql.Replace("$og_id", Convert.ToString(this.security.User.Org));
            sql = sql.Replace("$og_other_orgs_permission_level", Convert.ToString((int) this.security.User.OtherOrgsPermissionLevel));
            sql = sql.Replace("$bg_id", Convert.ToString(bugId));

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

            ViewBag.Users = new List<SelectListItem>();

            ViewBag.Users.Add(new SelectListItem
            {
                Value = "0",
                Text = "[not assigned]"
            });

            foreach (DataRowView row in new DataView(DbUtil.GetDataSet(sql).Tables[0]))
            {
                ViewBag.Users.Add(new SelectListItem
                {

                    Value = ((int)row["us_id"]).ToString(),
                    Text = (string)row["us_username"]
                });
            }

            ViewBag.Statuses = new List<SelectListItem>();

            ViewBag.Statuses.Add(new SelectListItem
            {
                Value = "0",
                Text = "[no status]"
            });

            foreach (DataRowView row in new DataView(DbUtil.GetDataSet(sql).Tables[1]))
            {
                ViewBag.Statuses.Add(new SelectListItem
                {

                    Value = ((int)row["st_id"]).ToString(),
                    Text = (string)row["st_name"]
                });
            }

            //this.assigned_to.DataSource = new DataView(DbUtil.GetDataSet(sql).Tables[0]);
            //this.assigned_to.DataTextField = ;
            //this.assigned_to.DataValueField = "us_id";
            //this.assigned_to.DataBind();
            //this.assigned_to.Items.Insert(0, new ListItem("[not assigned]", "0"));

            //this.status.DataSource = new DataView(DbUtil.GetDataSet(sql).Tables[1]);
            //this.status.DataTextField = "st_name";
            //this.status.DataValueField = "st_id";
            //this.status.DataBind();
            //this.status.Items.Insert(0, new ListItem("[no status]", "0"));


            //TODO
            // by default, assign the entry to the same user to whom the bug is assigned to?
            // or should it be assigned to the logged in user?
            //if (this.TskId == 0)
            //{
            //    var defaultAssignedToUser = (int)DbUtil.GetDataSet(sql).Tables[2].Rows[0][0];
            //    var li = this.assigned_to.Items.FindByValue(Convert.ToString(defaultAssignedToUser));
            //    if (li != null) li.Selected = true;
            //}

            ViewBag.Hours = new List<SelectListItem>();

            foreach (var hour in Enumerable.Range(0, 24))
            {
                ViewBag.Hours.Add(new SelectListItem
                {

                    Value = $"{hour:00}",
                    Text = $"{hour:00}"
                });
            }

            ViewBag.Minutes = new List<SelectListItem>();

            foreach (var minute in Enumerable.Range(0, 4))
            {
                var value = minute * 15;

                ViewBag.Minutes.Add(new SelectListItem
                {
                    Value = $"{value:00}",
                    Text = $"{value:00}"
                });
            }

            ViewBag.DurationUnits = new List<SelectListItem>();

            ViewBag.DurationUnits.Add(new SelectListItem
            {
                Value = "minutes",
                Text = "minutes"
            });

            ViewBag.DurationUnits.Add(new SelectListItem
            {
                Value = "hours",
                Text = "hours"
            });

            ViewBag.DurationUnits.Add(new SelectListItem
            {
                Value = "days",
                Text = "days"
            });
        }

        // This might not be right.   Maybe use the commented out version, from Sergey Vasiliev
        private static string FormatDateHourMin(string date, string hour, string min)
        {
            if (!string.IsNullOrEmpty(date))
                return Util.FormatLocalDateIntoDbFormat(
                    date
                    + " "
                    + hour
                    + ":"
                    + min
                    + ":00");

            return string.Empty;
        }

        // Version from Sergey Vasiliev
        //private static string FormatDateHourMin(string date, string hour, string min)
        //{
        //    if (!string.IsNullOrEmpty(date))
        //    {
        //        DateTime wDate = DateTime.ParseExact(date,
        //            Util.GetSetting("JustDateFormat", "g"),
        //            new System.Globalization.CultureInfo(System.Threading.Thread.CurrentThread.CurrentCulture.Name, true),
        //            System.Globalization.DateTimeStyles.AllowWhiteSpaces);

        //        return Util.FormatLocalDateIntoDbFormat(
        //            new DateTime(
        //                wDate.Year,
        //                wDate.Month,
        //                wDate.Day,
        //                Convert.ToInt32(hour),
        //                Convert.ToInt32(min), 0));
        //    }
        //    else
        //    {
        //        return string.Empty;
        //    }
        //}

        private static string FormatDecimalForDb(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "null";
            return Util.FormatLocalDecimalIntoDbFormat(s);
        }

        private static string FormatNumberForDb(string s)
        {
            if (string.IsNullOrEmpty(s))
            {
                return "null";
            }

            return s;
        }

        private static (string date, string hour, string minute) LoadDateHourMin(object date)
        {
            if (Convert.IsDBNull(date))
            {
                return (string.Empty, "00", "00");
            }
            else
            {
                var dt = Convert.ToDateTime(date);
                var tempDate = dt.Year.ToString("0000") + "-" + dt.Month.ToString("00") + "-" + dt.Day.ToString("00");

                return (Util.FormatDbDateTime(Convert.ToDateTime(tempDate)), dt.Hour.ToString("00"), dt.Minute.ToString("00"));
            }
        }
    }
}