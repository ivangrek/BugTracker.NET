/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Controllers
{
    using Core;
    using Core.Controls;
    using Models;
    using Models.Search;
    using Lucene.Net.Highlight;
    using Lucene.Net.Search;
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Text;
    using System.Web.Mvc;

    [Authorize]
    public class SearchController : Controller
    {
        private readonly IApplicationSettings applicationSettings;
        private readonly ISecurity security;

        public SearchController(
            IApplicationSettings applicationSettings,
            ISecurity security)
        {
            this.applicationSettings = applicationSettings;
            this.security = security;
        }

        [HttpGet]
        public ActionResult Index()
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanSearch;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            ViewBag.DsCustomCols = Util.GetCustomColumns();

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - search",
                SelectedItem = MainMenuSections.Search
            };

            LoadDropDowns();

            ViewBag.ProjectCustomDropdown1Display = "none";
            ViewBag.ProjectCustomDropdown2Display = "none";
            ViewBag.ProjectCustomDropdown3Display = "none";

            // are there any project dropdowns?
            var sql = @"
                select count(1)
                from projects
                where isnull(pj_enable_custom_dropdown1,0) = 1
                or isnull(pj_enable_custom_dropdown2,0) = 1
                or isnull(pj_enable_custom_dropdown3,0) = 1";

            var projectsWithCustomDropdowns = (int)DbUtil.ExecuteScalar(sql);

            if (projectsWithCustomDropdowns == 0)
            {
                ViewBag.ProjectAutoPostBack = false;
            }
            else
            {
                ViewBag.ProjectAutoPostBack = true;
            }

            var model = new IndexModel
            {
                Action = string.Empty,
                NewPage = 0,
                Filter = string.Empty,
                Sort = -1,
                PrevSort = -1,
                PrevDir = "ASC"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Index(IndexModel model)
        {
            var isAuthorized = this.security.User.IsAdmin
                || this.security.User.CanSearch;

            if (!isAuthorized)
            {
                return Content("You are not allowed to use this page.");
            }

            ViewBag.DsCustomCols = Util.GetCustomColumns();

            ViewBag.Page = new PageModel
            {
                ApplicationSettings = this.applicationSettings,
                Security = this.security,
                Title = $"{this.applicationSettings.AppTitle} - search",
                SelectedItem = MainMenuSections.Search
            };

            LoadDropDowns();

            // are there any project dropdowns?
            var sql = @"
                select count(1)
                from projects
                where isnull(pj_enable_custom_dropdown1,0) = 1
                or isnull(pj_enable_custom_dropdown2,0) = 1
                or isnull(pj_enable_custom_dropdown3,0) = 1";

            var projectsWithCustomDropdowns = (int)DbUtil.ExecuteScalar(sql);

            if (projectsWithCustomDropdowns == 0)
            {
                ViewBag.ProjectAutoPostBack = false;
            }
            else
            {
                ViewBag.ProjectAutoPostBack = true;
            }

            sql = @"
                select
                pj_id,
                isnull(pj_enable_custom_dropdown1,0) pj_enable_custom_dropdown1,
                isnull(pj_enable_custom_dropdown2,0) pj_enable_custom_dropdown2,
                isnull(pj_enable_custom_dropdown3,0) pj_enable_custom_dropdown3,
                isnull(pj_custom_dropdown_label1,'') pj_custom_dropdown_label1,
                isnull(pj_custom_dropdown_label2,'') pj_custom_dropdown_label2,
                isnull(pj_custom_dropdown_label3,'') pj_custom_dropdown_label3,
                isnull(pj_custom_dropdown_values1,'') pj_custom_dropdown_values1,
                isnull(pj_custom_dropdown_values2,'') pj_custom_dropdown_values2,
                isnull(pj_custom_dropdown_values3,'') pj_custom_dropdown_values3
                from projects
                where isnull(pj_enable_custom_dropdown1,0) = 1
                or isnull(pj_enable_custom_dropdown2,0) = 1
                or isnull(pj_enable_custom_dropdown3,0) = 1";

            var dsProjects = DbUtil.GetDataSet(sql);
            var mapProjects = new Dictionary<int, BtnetProject>();

            foreach (DataRow dr in dsProjects.Tables[0].Rows)
            {
                var btnetProject = new BtnetProject();

                ProjectDropdown dropdown;

                dropdown = new ProjectDropdown();
                dropdown.Enabled = Convert.ToBoolean((int)dr["pj_enable_custom_dropdown1"]);
                dropdown.Label = (string)dr["pj_custom_dropdown_label1"];
                dropdown.Values = (string)dr["pj_custom_dropdown_values1"];
                btnetProject.MapDropdowns[1] = dropdown;

                dropdown = new ProjectDropdown();
                dropdown.Enabled = Convert.ToBoolean((int)dr["pj_enable_custom_dropdown2"]);
                dropdown.Label = (string)dr["pj_custom_dropdown_label2"];
                dropdown.Values = (string)dr["pj_custom_dropdown_values2"];
                btnetProject.MapDropdowns[2] = dropdown;

                dropdown = new ProjectDropdown();
                dropdown.Enabled = Convert.ToBoolean((int)dr["pj_enable_custom_dropdown3"]);
                dropdown.Label = (string)dr["pj_custom_dropdown_label3"];
                dropdown.Values = (string)dr["pj_custom_dropdown_values3"];
                btnetProject.MapDropdowns[3] = dropdown;

                mapProjects[(int)dr["pj_id"]] = btnetProject;
            }

            // which button did the user hit?
            if (Request["project_changed"] == "1" /*&& this.project.AutoPostBack*/)
            {
                HandleProjectCustomDropdowns(model, mapProjects);
            }
            else if (Request["hit_submit_button"] == "1")
            {
                HandleProjectCustomDropdowns(model, mapProjects);
                DoQuery(model, mapProjects);
            }
            else
            {
                ViewBag.DataView = (DataView)Session["bugs"];

                if (ViewBag.DataView == null)
                {
                    DoQuery(model, mapProjects);
                }

                CallSortAndFilterBuglistDataview(model);
            }

            model.PrevSort = -1;
            model.PrevDir = "ASC";
            model.NewPage = 0;

            return View(model);
        }

        [HttpGet]
        public ActionResult SearchText(string query)
        {
            Query searchQuery;

            try
            {
                if (string.IsNullOrEmpty(query))
                {
                    throw new Exception("You forgot to enter something to search for...");
                }

                searchQuery = MyLucene.Parser.Parse(query);
            }
            catch (Exception ex)
            {
                var message = DisplayException(ex);

                return Content(message);
            }

            var scorer = new QueryScorer(searchQuery);
            var highlighter = new Highlighter(MyLucene.Formatter, scorer);

            highlighter.SetTextFragmenter(MyLucene.Fragmenter); // new Lucene.Net.Highlight.SimpleFragmenter(400));

            var sb = new StringBuilder();
            var guid = Guid.NewGuid().ToString().Replace("-", string.Empty);
            var dictAlreadySeenIds = new Dictionary<string, int>();

            sb.Append(@"
                create table #$GUID
                (
                temp_bg_id int,
                temp_bp_id int,
                temp_source varchar(30),
                temp_score float,
                temp_text nvarchar(3000)
                )");

            lock (MyLucene.MyLock)
            {
                Hits hits = null;

                try
                {
                    hits = MyLucene.Search(searchQuery);
                }
                catch (Exception ex)
                {
                    var message = DisplayException(ex);

                    return Content(message);
                }

                // insert the search results into a temp table which we will join with what's in the database
                for (var i = 0; i < hits.Length(); i++)
                {
                    if (dictAlreadySeenIds.Count < 100)
                    {
                        var doc = hits.Doc(i);
                        var bgId = doc.Get("bg_id");

                        if (!dictAlreadySeenIds.ContainsKey(bgId))
                        {
                            dictAlreadySeenIds[bgId] = 1;
                            sb.Append("insert into #");
                            sb.Append(guid);
                            sb.Append(" values(");
                            sb.Append(bgId);
                            sb.Append(",");
                            sb.Append(doc.Get("bp_id"));
                            sb.Append(",'");
                            sb.Append(doc.Get("src"));
                            sb.Append("',");
                            sb.Append(Convert.ToString(hits.Score(i))
                                .Replace(",", ".")); // Somebody said this fixes a bug. Localization issue?
                            sb.Append(",N'");

                            var rawText = Server.HtmlEncode(doc.Get("raw_text"));
                            var stream = MyLucene.Anal.TokenStream(string.Empty, new StringReader(rawText));
                            var highlightedText = highlighter.GetBestFragments(stream, rawText, 1, "...").Replace("'", "''");

                            if (string.IsNullOrEmpty(highlightedText)) // someties the highlighter fails to emit text...
                            {
                                highlightedText = rawText.Replace("'", "''");
                            }

                            if (highlightedText.Length > 3000)
                            {
                                highlightedText = highlightedText.Substring(0, 3000);
                            }

                            sb.Append(highlightedText);
                            sb.Append("'");
                            sb.Append(")\n");
                        }
                    }
                    else
                    {
                        break;
                    }
                }

                //searcher.Close();
            }

            sb.Append(@"
                select '#ffffff', bg_id [id], bg_short_desc [desc] ,
                    temp_source [search_source] ,
                    temp_text [search_text],
                    bg_reported_date [date],
                    isnull(st_name,'') [status],
                    temp_score [$SCORE]
                from bugs
                inner join #$GUID t on t.temp_bg_id = bg_id and t.temp_bp_id = 0
                left outer join statuses on st_id = bg_status
                where $ALTER_HERE

                union

                select '#ffffff', bg_id, bg_short_desc,
                    bp_type + ',' + convert(varchar,bp_id) COLLATE DATABASE_DEFAULT,
                    temp_text,
                    bp_date,
                    isnull(st_name,''),
                    temp_score
                from bugs
                inner join #$GUID t on t.temp_bg_id = bg_id
                inner join bug_posts on temp_bp_id = bp_id
                left outer join statuses on st_id = bg_status
                where $ALTER_HERE

                order by t.temp_score desc, bg_id desc

                drop table #$GUID");

            var sql = sb.ToString().Replace("$GUID", guid);

            sql = Util.AlterSqlPerProjectPermissions(sql, this.security);

            var ds = DbUtil.GetDataSet(sql);

            Session["bugs_unfiltered"] = ds.Tables[0];
            Session["bugs"] = new DataView(ds.Tables[0]);

            Session["just_did_text_search"] = "yes"; // switch for /Bug
            Session["query"] = query; // for util.cs, to persist the text in the search <input>

            return RedirectToAction("Index", "Bug");
        }

        private static string DisplayException(Exception e)
        {
            var stringBuilder = new StringBuilder();
            var message = e.Message;

            if (e.InnerException != null)
            {
                message += "<br>";
                message += e.InnerException.Message;
            }

            stringBuilder.Append(@"
                <html>
                <link rel=StyleSheet href=Content/btnet.css type=text/css>
                <p>&nbsp;</p>
                <div class=align>
                <div class=err>");

            stringBuilder.Append(message);

            stringBuilder.Append(@"
                <p>
                <a href='javascript:history.go(-1)'>back</a>
                </div></div>
                </html>");

            return stringBuilder.ToString();
        }

        private void LoadDropDowns()
        {
            ViewBag.DtUsers = Util.GetRelatedUsers(this.security, false);

            ViewBag.ReportedByUsers = new List<SelectListItem>();

            foreach (DataRow row in ViewBag.DtUsers.Rows)
            {
                ViewBag.ReportedByUsers.Add(new SelectListItem
                {
                    Value = Convert.ToString(row["us_id"]),
                    Text = Convert.ToString(row["us_username"]),
                });
            }

            var sql = string.Empty;

            // only show projects where user has permissions
            if (this.security.User.IsAdmin)
            {
                sql = "/* drop downs */ select pj_id, pj_name from projects order by pj_name;";
            }
            else
            {
                sql = @"/* drop downs */ select pj_id, pj_name
                    from projects
                    left outer join project_user_xref on pj_id = pu_project
                    and pu_user = $us
                    where isnull(pu_permission_level,$dpl) <> 0
                    order by pj_name;";

                sql = sql.Replace("$us", Convert.ToString(this.security.User.Usid));
                sql = sql.Replace("$dpl", this.applicationSettings.DefaultPermissionLevel.ToString());
            }

            if (this.security.User.OtherOrgsPermissionLevel != 0)
            {
                sql += " select og_id, og_name from orgs order by og_name;";
            }
            else
            {
                sql += " select og_id, og_name from orgs where og_id = " +
                            Convert.ToInt32(this.security.User.Org) +
                            " order by og_name;";

                //this.org.Visible = false;
                //this.org_label.Visible = false;
            }

            sql += @"
                select ct_id, ct_name from categories order by ct_sort_seq, ct_name;
                select pr_id, pr_name from priorities order by pr_sort_seq, pr_name;
                select st_id, st_name from statuses order by st_sort_seq, st_name;
                select udf_id, udf_name from user_defined_attribute order by udf_sort_seq, udf_name";

            var dsDropdowns = DbUtil.GetDataSet(sql);

            ViewBag.Projects = new List<SelectListItem>();
            ViewBag.Projects.Add(new SelectListItem
            {
                Value = "0",
                Text = "[no project]",
            });

            foreach (DataRow row in dsDropdowns.Tables[0].Rows)
            {
                ViewBag.Projects.Add(new SelectListItem
                {
                    Value = Convert.ToString(row["pj_id"]),
                    Text = Convert.ToString(row["pj_name"]),
                });
            }

            ViewBag.Organizations = new List<SelectListItem>();
            ViewBag.Organizations.Add(new SelectListItem
            {
                Value = "0",
                Text = "[no organization]",
            });

            foreach (DataRow row in dsDropdowns.Tables[1].Rows)
            {
                ViewBag.Organizations.Add(new SelectListItem
                {
                    Value = Convert.ToString(row["og_id"]),
                    Text = Convert.ToString(row["og_name"]),
                });
            }

            ViewBag.Categories = new List<SelectListItem>();
            ViewBag.Categories.Add(new SelectListItem
            {
                Value = "0",
                Text = "[no category]",
            });

            foreach (DataRow row in dsDropdowns.Tables[2].Rows)
            {
                ViewBag.Categories.Add(new SelectListItem
                {
                    Value = Convert.ToString(row["ct_id"]),
                    Text = Convert.ToString(row["ct_name"]),
                });
            }

            ViewBag.Priorities = new List<SelectListItem>();
            ViewBag.Priorities.Add(new SelectListItem
            {
                Value = "0",
                Text = "[no priority]",
            });

            foreach (DataRow row in dsDropdowns.Tables[3].Rows)
            {
                ViewBag.Priorities.Add(new SelectListItem
                {
                    Value = Convert.ToString(row["pr_id"]),
                    Text = Convert.ToString(row["pr_name"]),
                });
            }

            ViewBag.Statuses = new List<SelectListItem>();
            ViewBag.Statuses.Add(new SelectListItem
            {
                Value = "0",
                Text = "[no status]",
            });

            foreach (DataRow row in dsDropdowns.Tables[4].Rows)
            {
                ViewBag.Statuses.Add(new SelectListItem
                {
                    Value = Convert.ToString(row["st_id"]),
                    Text = Convert.ToString(row["st_name"]),
                });
            }

            ViewBag.AssignedToUsers = new List<SelectListItem>(ViewBag.ReportedByUsers);
            ViewBag.AssignedToUsers.Insert(0, new SelectListItem
            {
                Value = "0",
                Text = "[not assigned]",
            });

            if (this.applicationSettings.ShowUserDefinedBugAttribute)
            {
                ViewBag.Udfs = new List<SelectListItem>();
                ViewBag.Udfs.Add(new SelectListItem
                {
                    Value = "0",
                    Text = "[none]",
                });

                foreach (DataRow row in dsDropdowns.Tables[5].Rows)
                {
                    ViewBag.Udfs.Add(new SelectListItem
                    {
                        Value = Convert.ToString(row["udf_id"]),
                        Text = Convert.ToString(row["udf_name"]),
                    });
                }
            }

            ViewBag.ProjectCustomValues1 = new List<SelectListItem>();
            ViewBag.ProjectCustomValues2 = new List<SelectListItem>();
            ViewBag.ProjectCustomValues3 = new List<SelectListItem>();

            if (this.security.User.ProjectFieldPermissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                ViewBag.ProjectDisplay = "none";
            }
            else
            {
                ViewBag.ProjectDisplay = "block";
            }

            if (this.security.User.OrgFieldPermissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                ViewBag.OrganizationDisplay = "none";
            }
            else
            {
                ViewBag.OrganizationDisplay = "block";
            }

            if (this.security.User.CategoryFieldPermissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                ViewBag.CategoryDisplay = "none";
            }
            else
            {
                ViewBag.CategoryDisplay = "block";
            }

            if (this.security.User.PriorityFieldPermissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                ViewBag.PriorityDisplay = "none";
            }
            else
            {
                ViewBag.PriorityDisplay = "block";
            }

            if (this.security.User.StatusFieldPermissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                ViewBag.StatusDisplay = "none";
            }
            else
            {
                ViewBag.StatusDisplay = "block";
            }

            if (this.security.User.AssignedToFieldPermissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                ViewBag.AssignedToDisplay = "none";
            }
            else
            {
                ViewBag.AssignedToDisplay = "block";
            }

            if (this.security.User.UdfFieldPermissionLevel == SecurityPermissionLevel.PermissionNone)
            {
                ViewBag.UdfDisplay = "none";
            }
            else
            {
                ViewBag.UdfDisplay = "block";
            }
        }

        private void DoQuery(IndexModel model, Dictionary<int, BtnetProject> mapProjects)
        {
            // Create "WHERE" clause

            var where = string.Empty;

            var reportedByClause = BuildClauseFromListbox(model.ReportedByUserIds, "bg_reported_user");
            var assignedToClause = BuildClauseFromListbox(model.AssignedToUserIds, "bg_assigned_to_user");
            var projectClause = BuildClauseFromListbox(model.ProjectIds, "bg_project");

            var projectCustomDropdown1Clause = BuildClauseFromListbox(model.ProjectCustomValues1, "bg_project_custom_dropdown_value1");
            var projectCustomDropdown2Clause = BuildClauseFromListbox(model.ProjectCustomValues2, "bg_project_custom_dropdown_value2");
            var projectCustomDropdown3Clause = BuildClauseFromListbox(model.ProjectCustomValues3, "bg_project_custom_dropdown_value3");

            var orgClause = BuildClauseFromListbox(model.OrganizationIds, "bg_org");
            var categoryClause = BuildClauseFromListbox(model.CategoryIds, "bg_category");
            var priorityClause = BuildClauseFromListbox(model.PriorityIds, "bg_priority");
            var statusClause = BuildClauseFromListbox(model.StatusIds, "bg_status");
            var udfClause = string.Empty;

            if (this.applicationSettings.ShowUserDefinedBugAttribute)
            {
                udfClause = BuildClauseFromListbox(model.UdfIds, "bg_user_defined_attribute");
            }

            // SQL "LIKE" uses [, %, and _ in a special way
            var descClause = string.Empty;

            if (!string.IsNullOrEmpty(model.DescriptionContains))
            {
                var likeString = model.DescriptionContains.Replace("'", "''");

                likeString = likeString.Replace("[", "[[]");
                likeString = likeString.Replace("%", "[%]");
                likeString = likeString.Replace("_", "[_]");

                descClause = " bg_short_desc like";
                descClause += " N'%" + likeString + "%'\n";
            }

            var commentsClause = string.Empty;

            if (!string.IsNullOrEmpty(model.CommentContains))
            {
                var like2String = model.CommentContains.Replace("'", "''");

                like2String = like2String.Replace("[", "[[]");
                like2String = like2String.Replace("%", "[%]");
                like2String = like2String.Replace("_", "[_]");

                commentsClause = " bg_id in (select bp_bug from bug_posts where bp_type in ('comment','received','sent') and isnull(bp_comment_search,bp_comment) like";
                commentsClause += " N'%" + like2String + "%'";
                if (this.security.User.ExternalUser) commentsClause += " and bp_hidden_from_external_users = 0";
                commentsClause += ")\n";
            }

            var commentsSinceClause = string.Empty;

            if (!string.IsNullOrEmpty(model.CommentSince))
            {
                commentsSinceClause = " bg_id in (select bp_bug from bug_posts where bp_type in ('comment','received','sent') and bp_date > '";
                commentsSinceClause += FormatToDate(model.CommentSince);
                commentsSinceClause += "')\n";
            }

            var fromClause = string.Empty;

            if (!string.IsNullOrEmpty(model.ReportedOnFrom))
                fromClause = " bg_reported_date >= '" + FormatFromDate(model.ReportedOnFrom) + "'\n";

            var toClause = string.Empty;

            if (!string.IsNullOrEmpty(model.ReportedOnTo))
                toClause = " bg_reported_date <= '" + FormatToDate(model.ReportedOnTo) + "'\n";

            var luFromClause = string.Empty;
            if (!string.IsNullOrEmpty(model.LastupdatedOnFrom))
                luFromClause = " bg_last_updated_date >= '" + FormatFromDate(model.LastupdatedOnFrom) + "'\n";

            var luToClause = string.Empty;

            if (!string.IsNullOrEmpty(model.LastupdatedOnTo))
                luToClause = " bg_last_updated_date <= '" + FormatToDate(model.LastupdatedOnTo) + "'\n";

            where = BuildWhere(!model.UseOrLogic, where, reportedByClause);
            where = BuildWhere(!model.UseOrLogic, where, assignedToClause);
            where = BuildWhere(!model.UseOrLogic, where, projectClause);
            where = BuildWhere(!model.UseOrLogic, where, projectCustomDropdown1Clause);
            where = BuildWhere(!model.UseOrLogic, where, projectCustomDropdown2Clause);
            where = BuildWhere(!model.UseOrLogic, where, projectCustomDropdown3Clause);
            where = BuildWhere(!model.UseOrLogic, where, orgClause);
            where = BuildWhere(!model.UseOrLogic, where, categoryClause);
            where = BuildWhere(!model.UseOrLogic, where, priorityClause);
            where = BuildWhere(!model.UseOrLogic, where, statusClause);
            where = BuildWhere(!model.UseOrLogic, where, descClause);
            where = BuildWhere(!model.UseOrLogic, where, commentsClause);
            where = BuildWhere(!model.UseOrLogic, where, commentsSinceClause);
            where = BuildWhere(!model.UseOrLogic, where, fromClause);
            where = BuildWhere(!model.UseOrLogic, where, toClause);
            where = BuildWhere(!model.UseOrLogic, where, luFromClause);
            where = BuildWhere(!model.UseOrLogic, where, luToClause);

            if (this.applicationSettings.ShowUserDefinedBugAttribute)
            {
                where = BuildWhere(!model.UseOrLogic, where, udfClause);
            }

            var dsCustomCols = Util.GetCustomColumns();

            foreach (DataRow drcc in dsCustomCols.Tables[0].Rows)
            {
                var columnName = (string)drcc["name"];
                if (this.security.User.DictCustomFieldPermissionLevel[columnName] ==
                    SecurityPermissionLevel.PermissionNone) continue;

                var values = Request[columnName];

                if (values != null)
                {
                    values = values.Replace("'", "''");

                    var customClause = string.Empty;

                    var datatype = (string)drcc["datatype"];

                    if ((datatype == "varchar" || datatype == "nvarchar" || datatype == "char" || datatype == "nchar")
                        && string.IsNullOrEmpty((string)drcc["dropdown type"]))
                    {
                        if (!string.IsNullOrEmpty(values))
                        {
                            customClause = " [" + columnName + "] like '%" + values + "%'\n";
                            where = BuildWhere(!model.UseOrLogic, where, customClause);
                        }
                    }
                    else if (datatype == "datetime")
                    {
                        if (!string.IsNullOrEmpty(values))
                        {
                            customClause = " [" + columnName + "] >= '" + FormatFromDate(values) + "'\n";
                            where = BuildWhere(!model.UseOrLogic, where, customClause);

                            // reset, and do the to date
                            customClause = string.Empty;
                            values = Request["to__" + columnName];
                            if (!string.IsNullOrEmpty(values))
                            {
                                customClause = " [" + columnName + "] <= '" + FormatToDate(values) + "'\n";
                                where = BuildWhere(!model.UseOrLogic, where, customClause);
                            }
                        }
                    }
                    else
                    {
                        if (string.IsNullOrEmpty(values) && (datatype == "int" || datatype == "decimal"))
                        {
                            // skip
                        }
                        else
                        {
                            var inNotIn = FormatInNotIn(values);
                            customClause = " [" + columnName + "] in " + inNotIn + "\n";
                            where = BuildWhere(!model.UseOrLogic, where, customClause);
                        }
                    }
                }
            }

            // The rest of the SQL is either built in or comes from Web.config
            var searchSql = this.applicationSettings.SearchSQL;

            string sql;

            if (string.IsNullOrEmpty(searchSql))
            {
                /*
            select isnull(pr_background_color,'#ffffff') [color], bg_id [id],
            bg_short_desc [desc],
            bg_reported_date [reported on],
            isnull(rpt.us_username,'') [reported by],
            isnull(pj_name,'') [project],
            isnull(og_name,'') [organization],
            isnull(ct_name,'') [category],
            isnull(pr_name,'') [priority],
            isnull(asg.us_username,'') [assigned to],
            isnull(st_name,'') [status],
            isnull(udf_name,'') [MyUDF],
            isnull([mycust],'') [mycust],
            isnull([mycust2],'') [mycust2]
            from bugs
            left outer join users rpt on rpt.us_id = bg_reported_user
            left outer join users asg on asg.us_id = bg_assigned_to_user
            left outer join projects on pj_id = bg_project
            left outer join orgs on og_id = bg_org
            left outer join categories on ct_id = bg_category
            left outer join priorities on pr_id = bg_priority
            left outer join statuses on st_id = bg_status
            left outer join user_defined_attribute on udf_id = bg_user_defined_attribute
            order by bg_id desc
            */

                var select = "select isnull(pr_background_color,'#ffffff') [color], bg_id [id],\nbg_short_desc [desc]";

                // reported
                if (this.applicationSettings.UseFullNames)
                    select += "\n,isnull(rpt.us_lastname + ', ' + rpt.us_firstname,'') [reported by]";
                else
                    select += "\n,isnull(rpt.us_username,'') [reported by]";
                select += "\n,bg_reported_date [reported on]";

                // last updated
                if (this.applicationSettings.UseFullNames)
                    select += "\n,isnull(lu.us_lastname + ', ' + lu.us_firstname,'') [last updated by]";
                else
                    select += "\n,isnull(lu.us_username,'') [last updated by]";
                select += "\n,bg_last_updated_date [last updated on]";

                if (this.security.User.TagsFieldPermissionLevel != SecurityPermissionLevel.PermissionNone)
                    select += ",\nisnull(bg_tags,'') [tags]";

                if (this.security.User.ProjectFieldPermissionLevel != SecurityPermissionLevel.PermissionNone)
                    select += ",\nisnull(pj_name,'') [project]";

                if (this.security.User.OrgFieldPermissionLevel != SecurityPermissionLevel.PermissionNone)
                    select += ",\nisnull(og_name,'') [organization]";

                if (this.security.User.CategoryFieldPermissionLevel != SecurityPermissionLevel.PermissionNone)
                    select += ",\nisnull(ct_name,'') [category]";

                if (this.security.User.PriorityFieldPermissionLevel != SecurityPermissionLevel.PermissionNone)
                    select += ",\nisnull(pr_name,'') [priority]";

                if (this.security.User.AssignedToFieldPermissionLevel != SecurityPermissionLevel.PermissionNone)
                {
                    if (this.applicationSettings.UseFullNames)
                        select += ",\nisnull(asg.us_lastname + ', ' + asg.us_firstname,'') [assigned to]";
                    else
                        select += ",\nisnull(asg.us_username,'') [assigned to]";
                }

                if (this.security.User.StatusFieldPermissionLevel != SecurityPermissionLevel.PermissionNone)
                    select += ",\nisnull(st_name,'') [status]";

                if (this.security.User.UdfFieldPermissionLevel != SecurityPermissionLevel.PermissionNone)
                    if (this.applicationSettings.ShowUserDefinedBugAttribute)
                    {
                        var udfName = this.applicationSettings.UserDefinedBugAttributeName;
                        select += ",\nisnull(udf_name,'') [" + udfName + "]";
                    }

                // let results include custom columns
                var customColsSql = string.Empty;
                var userTypeCnt = 1;
                foreach (DataRow drcc in dsCustomCols.Tables[0].Rows)
                {
                    var columnName = (string)drcc["name"];
                    if (this.security.User.DictCustomFieldPermissionLevel[columnName] ==
                        SecurityPermissionLevel.PermissionNone) continue;

                    if (Convert.ToString(drcc["dropdown type"]) == "users")
                    {
                        customColsSql += ",\nisnull(users"
                                           + Convert.ToString(userTypeCnt++)
                                           + ".us_username,'') "
                                           + "["
                                           + columnName + "]";
                    }
                    else
                    {
                        if (Convert.ToString(drcc["datatype"]) == "decimal")
                            customColsSql += ",\nisnull(["
                                               + columnName
                                               + "],0)["
                                               + columnName + "]";
                        else
                            customColsSql += ",\nisnull(["
                                               + columnName
                                               + "],'')["
                                               + columnName + "]";
                    }
                }

                select += customColsSql;

                // Handle project custom dropdowns
                var projectDropdownSelectColsServerSide = string.Empty;

                string alias1 = null;
                string alias2 = null;
                string alias3 = null;

                foreach (var projectId in model.ProjectIds)
                {
                    if (projectId == 0)
                        continue;

                    var pjId = projectId;

                    if (mapProjects.ContainsKey(pjId))
                    {
                        var btnetProject = mapProjects[pjId];

                        if (btnetProject.MapDropdowns[1].Enabled)
                        {
                            if (alias1 == null)
                                alias1 = btnetProject.MapDropdowns[1].Label;
                            else
                                alias1 = "dropdown1";
                        }

                        if (btnetProject.MapDropdowns[2].Enabled)
                        {
                            if (alias2 == null)
                                alias2 = btnetProject.MapDropdowns[2].Label;
                            else
                                alias2 = "dropdown2";
                        }

                        if (btnetProject.MapDropdowns[3].Enabled)
                        {
                            if (alias3 == null)
                                alias3 = btnetProject.MapDropdowns[3].Label;
                            else
                                alias3 = "dropdown3";
                        }
                    }
                }

                if (alias1 != null)
                    projectDropdownSelectColsServerSide += ",\nisnull(bg_project_custom_dropdown_value1,'') [" + alias1 + "]";
                if (alias2 != null)
                    projectDropdownSelectColsServerSide += ",\nisnull(bg_project_custom_dropdown_value2,'') [" + alias2 + "]";
                if (alias3 != null)
                    projectDropdownSelectColsServerSide += ",\nisnull(bg_project_custom_dropdown_value3,'') [" + alias3 + "]";

                select += projectDropdownSelectColsServerSide;

                select += @" from bugs
                left outer join users rpt on rpt.us_id = bg_reported_user
                left outer join users lu on lu.us_id = bg_last_updated_user
                left outer join users asg on asg.us_id = bg_assigned_to_user
                left outer join projects on pj_id = bg_project
                left outer join orgs on og_id = bg_org
                left outer join categories on ct_id = bg_category
                left outer join priorities on pr_id = bg_priority
                left outer join statuses on st_id = bg_status
                ";

                userTypeCnt = 1;
                foreach (DataRow drcc in dsCustomCols.Tables[0].Rows)
                {
                    var columnName = (string)drcc["name"];
                    if (this.security.User.DictCustomFieldPermissionLevel[columnName] ==
                        SecurityPermissionLevel.PermissionNone) continue;

                    if (Convert.ToString(drcc["dropdown type"]) == "users")
                    {
                        select += "left outer join users users"
                                  + Convert.ToString(userTypeCnt)
                                  + " on users"
                                  + Convert.ToString(userTypeCnt)
                                  + ".us_id = bugs."
                                  + "[" + columnName + "]\n";

                        userTypeCnt++;
                    }
                }

                if (this.applicationSettings.ShowUserDefinedBugAttribute)
                    select += "left outer join user_defined_attribute on udf_id = bg_user_defined_attribute";

                sql = select + where + " order by bg_id desc";
            }
            else
            {
                searchSql = searchSql.Replace("[br]", "\n");
                sql = searchSql.Replace("$WHERE$", where);
            }

            sql = Util.AlterSqlPerProjectPermissions(sql, security);

            var ds = DbUtil.GetDataSet(sql);

            ViewBag.DataView = new DataView(ds.Tables[0]);

            Session["bugs"] = ViewBag.DataView;
            Session["bugs_unfiltered"] = ds.Tables[0];
        }

        private string BuildWhere(bool useAndLogic, string where, string clause)
        {
            if (string.IsNullOrEmpty(clause)) return where;

            var sql = string.Empty;

            if (string.IsNullOrEmpty(where))
            {
                sql = " where ";
                sql += clause;
            }
            else
            {
                sql = where;
                var andOr = useAndLogic ? "and " : "or ";
                sql += andOr;
                sql += clause;
            }

            return sql;
        }

        private static string BuildClauseFromListbox(List<int> list, string columnName)
        {
            var clause = string.Empty;

            foreach (var item in list)
            {
                if (string.IsNullOrEmpty(clause))
                    clause += columnName + " in (";
                else
                    clause += ",";

                clause += $"{item}";
            }

            if (!string.IsNullOrEmpty(clause)) clause += ") ";

            return clause;
        }

        private static string BuildClauseFromListbox(List<string> list, string columnName)
        {
            var clause = string.Empty;

            foreach (var item in list)
            {
                if (string.IsNullOrEmpty(clause))
                    clause += columnName + " in (";
                else
                    clause += ",";

                clause += $"'{item}'";
            }

            if (!string.IsNullOrEmpty(clause)) clause += ") ";

            return clause;
        }

        private static string FormatInNotIn(string s)
        {
            var vals = "(";
            var opts = string.Empty;

            var s2 = Util.SplitStringUsingCommas(s);
            for (var i = 0; i < s2.Length; i++)
            {
                if (!string.IsNullOrEmpty(opts)) opts += ",";

                var oneOpt = "N'";
                oneOpt += s2[i].Replace("'", "''");
                oneOpt += "'";

                opts += oneOpt;
            }

            vals += opts;
            vals += ")";

            return vals;
        }

        public static string FormatFromDate(string dt)
        {
            return Util.FormatLocalDateIntoDbFormat(dt).Replace(" 12:00:00", "").Replace(" 00:00:00", "");
        }

        public static string FormatToDate(string dt)
        {
            return Util.FormatLocalDateIntoDbFormat(dt).Replace(" 12:00:00", " 23:59:59")
                .Replace(" 00:00:00", " 23:59:59");
        }

        private void HandleProjectCustomDropdowns(IndexModel model, Dictionary<int, BtnetProject> mapProjects)
        {
            // How many projects selected?
            var dupeDetectionDictionaries = new Dictionary<string, string>[3];
            var previousSelectionDictionaries = new Dictionary<string, string>[3];
            for (var i = 0; i < dupeDetectionDictionaries.Length; i++)
            {
                // Initialize Dictionary to accumulate ListItem values as they are added to the ListBox
                // so that duplicate values from multiple projects are not added to the ListBox twice.
                dupeDetectionDictionaries[i] = new Dictionary<string, string>();

                previousSelectionDictionaries[i] = new Dictionary<string, string>();
            }

            // Preserve user's previous selections (necessary if this is called during a postback).
            foreach (var value in model.ProjectCustomValues1)
                previousSelectionDictionaries[0].Add(value, value);
            foreach (var value in model.ProjectCustomValues2)
                previousSelectionDictionaries[1].Add(value, value);
            foreach (var value in model.ProjectCustomValues3)
                previousSelectionDictionaries[2].Add(value, value);

            ViewBag.ProjectDropdownSelectCols = string.Empty;

            ViewBag.ProjectCustomDropdown1Label = string.Empty;
            ViewBag.ProjectCustomDropdown1Label = string.Empty;
            ViewBag.ProjectCustomDropdown1Label = string.Empty;

            model.ProjectCustomValues1.Clear();
            model.ProjectCustomValues2.Clear();
            model.ProjectCustomValues3.Clear();

            ViewBag.ProjectCustomValues1 = new List<SelectListItem>();
            ViewBag.ProjectCustomValues2 = new List<SelectListItem>();
            ViewBag.ProjectCustomValues3 = new List<SelectListItem>();

            foreach (var projectId in model.ProjectIds)
            {
                // Read the project dropdown info from the db.
                // Load the dropdowns as necessary

                if (projectId == 0)
                    continue;

                var pjId = projectId;

                if (mapProjects.ContainsKey(pjId))
                {
                    var btnetProject = mapProjects[pjId];

                    if (btnetProject.MapDropdowns[1].Enabled)
                    {
                        if (string.IsNullOrEmpty(ViewBag.ProjectCustomDropdown1Label))
                        {
                            ViewBag.ProjectCustomDropdown1Label = btnetProject.MapDropdowns[1].Label;
                            ViewBag.ProjectCustomDropdown1Display = "block";
                        }
                        else if (ViewBag.ProjectCustomDropdown1Label != btnetProject.MapDropdowns[1].Label)
                        {
                            ViewBag.ProjectCustomDropdown1Label = "dropdown1";
                        }

                        LoadProjectCustomDropdown(ViewBag.ProjectCustomValues1, btnetProject.MapDropdowns[1].Values, dupeDetectionDictionaries[0]);
                    }

                    if (btnetProject.MapDropdowns[2].Enabled)
                    {
                        if (string.IsNullOrEmpty(ViewBag.ProjectCustomDropdown2Label))
                        {
                            ViewBag.ProjectCustomDropdown2Label = btnetProject.MapDropdowns[2].Label;
                            ViewBag.ProjectCustomDropdown2Display = "block";
                        }
                        else if (ViewBag.ProjectCustomDropdown2Label != btnetProject.MapDropdowns[2].Label)
                        {
                            ViewBag.ProjectCustomDropdown2Label = "dropdown2";
                        }

                        LoadProjectCustomDropdown(ViewBag.ProjectCustomValues2, btnetProject.MapDropdowns[2].Values, dupeDetectionDictionaries[1]);
                    }

                    if (btnetProject.MapDropdowns[3].Enabled)
                    {
                        if (string.IsNullOrEmpty(ViewBag.ProjectCustomDropdown3Label))
                        {
                            ViewBag.ProjectCustomDropdown3Label = btnetProject.MapDropdowns[3].Label;
                            ViewBag.ProjectCustomDropdown3Display = "block";
                        }
                        else if (ViewBag.ProjectCustomDropdown3Label != btnetProject.MapDropdowns[3].Label)
                        {
                            ViewBag.ProjectCustomDropdown3Label = "dropdown3";
                        }

                        LoadProjectCustomDropdown(ViewBag.ProjectCustomValues3, btnetProject.MapDropdowns[3].Values, dupeDetectionDictionaries[2]);
                    }
                }
            }

            if (string.IsNullOrEmpty(ViewBag.ProjectCustomDropdown1Label))
            {
                model.ProjectCustomValues1.Clear();

                ViewBag.ProjectCustomDropdown1Display = "none";
            }
            else
            {
                ViewBag.ProjectCustomDropdown1Display = "block";
                ViewBag.ProjectDropdownSelectCols += ",\\nisnull(bg_project_custom_dropdown_value1,'') [" + ViewBag.ProjectCustomDropdown1Label + "]";
            }

            if (string.IsNullOrEmpty(ViewBag.ProjectCustomDropdown2Label))
            {
                model.ProjectCustomValues2.Clear();

                ViewBag.ProjectCustomDropdown2Display = "none";
            }
            else
            {
                ViewBag.ProjectCustomDropdown2Display = "block";
                ViewBag.ProjectDropdownSelectCols += ",\\nisnull(bg_project_custom_dropdown_value2,'') [" + ViewBag.ProjectCustomDropdown2Label + "]";
            }

            if (string.IsNullOrEmpty(ViewBag.ProjectCustomDropdown3Label))
            {
                model.ProjectCustomValues3.Clear();

                ViewBag.ProjectCustomDropdown3Display = "none";
            }
            else
            {
                ViewBag.ProjectCustomDropdown3Display = "block";
                ViewBag.ProjectDropdownSelectCols += ",\\nisnull(bg_project_custom_dropdown_value3,'') [" + ViewBag.ProjectCustomDropdown3Label + "]";
            }

            // Restore user's previous selections.
            foreach (var item in (List<SelectListItem>)ViewBag.ProjectCustomValues1)
                if (previousSelectionDictionaries[0].ContainsKey(item.Value))
                {
                    model.ProjectCustomValues1.Add(item.Value);
                }
            foreach (var item in (List<SelectListItem>)ViewBag.ProjectCustomValues2)
                if (previousSelectionDictionaries[0].ContainsKey(item.Value))
                {
                    model.ProjectCustomValues2.Add(item.Value);
                }
            foreach (var item in (List<SelectListItem>)ViewBag.ProjectCustomValues3)
                if (previousSelectionDictionaries[0].ContainsKey(item.Value))
                {
                    model.ProjectCustomValues2.Add(item.Value);
                }
        }

        public void CallSortAndFilterBuglistDataview(IndexModel model)
        {
            var filterVal = model.Filter;
            var sortVal = model.Sort.ToString();
            var prevSortVal = model.PrevSort.ToString();
            var prevDirVal = model.PrevDir;

            BugList.SortAndFilterBugListDataView(ViewBag.DatView, /*IsPostBack*/true, model.Action,
                ref filterVal,
                ref sortVal,
                ref prevSortVal,
                ref prevDirVal);

            model.Filter = filterVal;
            model.Sort = int.Parse(sortVal);
            model.PrevSort = int.Parse(prevSortVal);
            model.PrevDir = prevDirVal;
        }

        public static void LoadProjectCustomDropdown(List<SelectListItem> list, string valsString, Dictionary<string, string> duplicateDetectionDictionary)
        {
            var valsArray = Util.SplitDropdownVals(valsString);

            for (var i = 0; i < valsArray.Length; i++)
                if (!duplicateDetectionDictionary.ContainsKey(valsArray[i]))
                {
                    list.Add(new SelectListItem
                    {
                        Text = valsArray[i],
                        Value = valsArray[i]
                    });

                    duplicateDetectionDictionary.Add(valsArray[i], valsArray[i]);
                }
        }

        private class ProjectDropdown
        {
            public bool Enabled { get; set; }
            public string Label { get; set; } = string.Empty;
            public string Values { get; set; } = string.Empty;
        }

        private class BtnetProject
        {
            public Dictionary<int, ProjectDropdown> MapDropdowns = new Dictionary<int, ProjectDropdown>();
        }
    }
}