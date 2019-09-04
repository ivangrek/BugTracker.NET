/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using Core;

    public partial class Search : Page
    {
        public DataSet DsCustomCols;

        public DataTable DtUsers;
        public DataView Dv;

        public Dictionary<int, BtnetProject> MapProjects = new Dictionary<int, BtnetProject>();

        public string ProjectDropdownSelectCols = "";
        public bool ShowUdf;

        public string Sql;
        public bool UseFullNames;

        public Security Security { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.AnyUserOk);

            Security = security;

            MainMenu.Security = security;
            MainMenu.SelectedItem = "search";

            if (security.User.IsAdmin || security.User.CanSearch)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - search";

            this.ShowUdf = Util.GetSetting("ShowUserDefinedBugAttribute", "1") == "1";
            this.UseFullNames = Util.GetSetting("UseFullNames", "0") == "1";

            this.DsCustomCols = Util.GetCustomColumns();

            this.DtUsers = Util.GetRelatedUsers(security, false);

            if (!IsPostBack)
            {
                load_drop_downs(security);
                this.project_custom_dropdown1_label.Style["display"] = "none";
                this.project_custom_dropdown1.Style["display"] = "none";

                this.project_custom_dropdown2_label.Style["display"] = "none";
                this.project_custom_dropdown2.Style["display"] = "none";

                this.project_custom_dropdown3_label.Style["display"] = "none";
                this.project_custom_dropdown3.Style["display"] = "none";

                // are there any project dropdowns?

                var sql = @"
select count(1)
from projects
where isnull(pj_enable_custom_dropdown1,0) = 1
or isnull(pj_enable_custom_dropdown2,0) = 1
or isnull(pj_enable_custom_dropdown3,0) = 1";

                var projectsWithCustomDropdowns = (int) DbUtil.ExecuteScalar(sql);

                if (projectsWithCustomDropdowns == 0) this.project.AutoPostBack = false;
            }
            else
            {
                // get the project dropdowns

                var sql = @"
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

                foreach (DataRow dr in dsProjects.Tables[0].Rows)
                {
                    var btnetProject = new BtnetProject();

                    ProjectDropdown dropdown;

                    dropdown = new ProjectDropdown();
                    dropdown.Enabled = Convert.ToBoolean((int) dr["pj_enable_custom_dropdown1"]);
                    dropdown.Label = (string) dr["pj_custom_dropdown_label1"];
                    dropdown.Values = (string) dr["pj_custom_dropdown_values1"];
                    btnetProject.MapDropdowns[1] = dropdown;

                    dropdown = new ProjectDropdown();
                    dropdown.Enabled = Convert.ToBoolean((int) dr["pj_enable_custom_dropdown2"]);
                    dropdown.Label = (string) dr["pj_custom_dropdown_label2"];
                    dropdown.Values = (string) dr["pj_custom_dropdown_values2"];
                    btnetProject.MapDropdowns[2] = dropdown;

                    dropdown = new ProjectDropdown();
                    dropdown.Enabled = Convert.ToBoolean((int) dr["pj_enable_custom_dropdown3"]);
                    dropdown.Label = (string) dr["pj_custom_dropdown_label3"];
                    dropdown.Values = (string) dr["pj_custom_dropdown_values3"];
                    btnetProject.MapDropdowns[3] = dropdown;

                    this.MapProjects[(int) dr["pj_id"]] = btnetProject;
                }

                // which button did the user hit?

                if (this.project_changed.Value == "1" && this.project.AutoPostBack)
                {
                    handle_project_custom_dropdowns();
                }
                else if (this.hit_submit_button.Value == "1")
                {
                    handle_project_custom_dropdowns();
                    do_query(security);
                }
                else
                {
                    this.Dv = (DataView) Session["bugs"];
                    if (this.Dv == null) do_query(security);
                    call_sort_and_filter_buglist_dataview();
                }
            }

            this.hit_submit_button.Value = "0";
            this.project_changed.Value = "0";

            if (security.User.IsAdmin || security.User.CanEditSql)
            {
            }
            else
            {
                this.visible_sql_label.Style["display"] = "none";
                this.visible_sql_text.Style["display"] = "none";
            }
        }

        public string build_where(string where, string clause)
        {
            if (clause == "") return where;

            var sql = "";

            if (where == "")
            {
                sql = " where ";
                sql += clause;
            }
            else
            {
                sql = where;
                var andOr = this.and.Checked ? "and " : "or ";
                sql += andOr;
                sql += clause;
            }

            return sql;
        }

        public string build_clause_from_listbox(ListBox lb, string columnName)
        {
            var clause = "";
            foreach (ListItem li in lb.Items)
                if (li.Selected)
                {
                    if (clause == "")
                        clause += columnName + " in (";
                    else
                        clause += ",";

                    clause += li.Value;
                }

            if (clause != "") clause += ") ";

            return clause;
        }

        public string format_in_not_in(string s)
        {
            var vals = "(";
            var opts = "";

            var s2 = Util.SplitStringUsingCommas(s);
            for (var i = 0; i < s2.Length; i++)
            {
                if (opts != "") opts += ",";

                var oneOpt = "N'";
                oneOpt += s2[i].Replace("'", "''");
                oneOpt += "'";

                opts += oneOpt;
            }

            vals += opts;
            vals += ")";

            return vals;
        }

        public List<ListItem> get_selected_projects()
        {
            var selectedProjects = new List<ListItem>();

            foreach (ListItem li in this.project.Items)
                if (li.Selected)
                    selectedProjects.Add(li);

            return selectedProjects;
        }

        public void do_query(Security security)
        {
            this.prev_sort.Value = "-1";
            this.prev_dir.Value = "ASC";
            this.new_page.Value = "0";

            // Create "WHERE" clause

            var where = "";

            var reportedByClause = build_clause_from_listbox(this.reported_by, "bg_reported_user");
            var assignedToClause = build_clause_from_listbox(this.assigned_to, "bg_assigned_to_user");
            var projectClause = build_clause_from_listbox(this.project, "bg_project");

            var projectCustomDropdown1Clause
                = build_clause_from_listbox(this.project_custom_dropdown1, "bg_project_custom_dropdown_value1");
            var projectCustomDropdown2Clause
                = build_clause_from_listbox(this.project_custom_dropdown2, "bg_project_custom_dropdown_value2");
            var projectCustomDropdown3Clause
                = build_clause_from_listbox(this.project_custom_dropdown3, "bg_project_custom_dropdown_value3");

            var orgClause = build_clause_from_listbox(this.org, "bg_org");
            var categoryClause = build_clause_from_listbox(this.category, "bg_category");
            var priorityClause = build_clause_from_listbox(this.priority, "bg_priority");
            var statusClause = build_clause_from_listbox(this.status, "bg_status");
            var udfClause = "";

            if (this.ShowUdf) udfClause = build_clause_from_listbox(this.udf, "bg_user_defined_attribute");

            // SQL "LIKE" uses [, %, and _ in a special way

            var likeString = this.like.Value.Replace("'", "''");
            likeString = likeString.Replace("[", "[[]");
            likeString = likeString.Replace("%", "[%]");
            likeString = likeString.Replace("_", "[_]");

            var like2String = this.like2.Value.Replace("'", "''");
            like2String = like2String.Replace("[", "[[]");
            like2String = like2String.Replace("%", "[%]");
            like2String = like2String.Replace("_", "[_]");

            var descClause = "";
            if (this.like.Value != "")
            {
                descClause = " bg_short_desc like";
                descClause += " N'%" + likeString + "%'\n";
            }

            var commentsClause = "";
            if (this.like2.Value != "")
            {
                commentsClause =
                    " bg_id in (select bp_bug from bug_posts where bp_type in ('comment','received','sent') and isnull(bp_comment_search,bp_comment) like";
                commentsClause += " N'%" + like2String + "%'";
                if (security.User.ExternalUser) commentsClause += " and bp_hidden_from_external_users = 0";
                commentsClause += ")\n";
            }

            var commentsSinceClause = "";
            if (this.comments_since.Value != "")
            {
                commentsSinceClause =
                    " bg_id in (select bp_bug from bug_posts where bp_type in ('comment','received','sent') and bp_date > '";
                commentsSinceClause += format_to_date(this.comments_since.Value);
                commentsSinceClause += "')\n";
            }

            var fromClause = "";
            if (this.from_date.Value != "")
                fromClause = " bg_reported_date >= '" + format_from_date(this.from_date.Value) + "'\n";

            var toClause = "";
            if (this.to_date.Value != "")
                toClause = " bg_reported_date <= '" + format_to_date(this.to_date.Value) + "'\n";

            var luFromClause = "";
            if (this.lu_from_date.Value != "")
                luFromClause = " bg_last_updated_date >= '" + format_from_date(this.lu_from_date.Value) + "'\n";

            var luToClause = "";
            if (this.lu_to_date.Value != "")
                luToClause = " bg_last_updated_date <= '" + format_to_date(this.lu_to_date.Value) + "'\n";

            where = build_where(where, reportedByClause);
            where = build_where(where, assignedToClause);
            where = build_where(where, projectClause);
            where = build_where(where, projectCustomDropdown1Clause);
            where = build_where(where, projectCustomDropdown2Clause);
            where = build_where(where, projectCustomDropdown3Clause);
            where = build_where(where, orgClause);
            where = build_where(where, categoryClause);
            where = build_where(where, priorityClause);
            where = build_where(where, statusClause);
            where = build_where(where, descClause);
            where = build_where(where, commentsClause);
            where = build_where(where, commentsSinceClause);
            where = build_where(where, fromClause);
            where = build_where(where, toClause);
            where = build_where(where, luFromClause);
            where = build_where(where, luToClause);

            if (this.ShowUdf) where = build_where(where, udfClause);

            foreach (DataRow drcc in this.DsCustomCols.Tables[0].Rows)
            {
                var columnName = (string) drcc["name"];
                if (security.User.DictCustomFieldPermissionLevel[columnName] ==
                    Security.PermissionNone) continue;

                var values = Request[columnName];

                if (values != null)
                {
                    values = values.Replace("'", "''");

                    var customClause = "";

                    var datatype = (string) drcc["datatype"];

                    if ((datatype == "varchar" || datatype == "nvarchar" || datatype == "char" || datatype == "nchar")
                        && (string) drcc["dropdown type"] == "")
                    {
                        if (values != "")
                        {
                            customClause = " [" + columnName + "] like '%" + values + "%'\n";
                            where = build_where(where, customClause);
                        }
                    }
                    else if (datatype == "datetime")
                    {
                        if (values != "")
                        {
                            customClause = " [" + columnName + "] >= '" + format_from_date(values) + "'\n";
                            where = build_where(where, customClause);

                            // reset, and do the to date
                            customClause = "";
                            values = Request["to__" + columnName];
                            if (values != "")
                            {
                                customClause = " [" + columnName + "] <= '" + format_to_date(values) + "'\n";
                                where = build_where(where, customClause);
                            }
                        }
                    }
                    else
                    {
                        if (values == "" && (datatype == "int" || datatype == "decimal"))
                        {
                            // skip
                        }
                        else
                        {
                            var inNotIn = format_in_not_in(values);
                            customClause = " [" + columnName + "] in " + inNotIn + "\n";
                            where = build_where(where, customClause);
                        }
                    }
                }
            }

            // The rest of the SQL is either built in or comes from Web.config

            var searchSql = Util.GetSetting("SearchSQL", "");

            if (searchSql == "")
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
                if (this.UseFullNames)
                    select += "\n,isnull(rpt.us_lastname + ', ' + rpt.us_firstname,'') [reported by]";
                else
                    select += "\n,isnull(rpt.us_username,'') [reported by]";
                select += "\n,bg_reported_date [reported on]";

                // last updated
                if (this.UseFullNames)
                    select += "\n,isnull(lu.us_lastname + ', ' + lu.us_firstname,'') [last updated by]";
                else
                    select += "\n,isnull(lu.us_username,'') [last updated by]";
                select += "\n,bg_last_updated_date [last updated on]";

                if (security.User.TagsFieldPermissionLevel != Security.PermissionNone)
                    select += ",\nisnull(bg_tags,'') [tags]";

                if (security.User.ProjectFieldPermissionLevel != Security.PermissionNone)
                    select += ",\nisnull(pj_name,'') [project]";

                if (security.User.OrgFieldPermissionLevel != Security.PermissionNone)
                    select += ",\nisnull(og_name,'') [organization]";

                if (security.User.CategoryFieldPermissionLevel != Security.PermissionNone)
                    select += ",\nisnull(ct_name,'') [category]";

                if (security.User.PriorityFieldPermissionLevel != Security.PermissionNone)
                    select += ",\nisnull(pr_name,'') [priority]";

                if (security.User.AssignedToFieldPermissionLevel != Security.PermissionNone)
                {
                    if (this.UseFullNames)
                        select += ",\nisnull(asg.us_lastname + ', ' + asg.us_firstname,'') [assigned to]";
                    else
                        select += ",\nisnull(asg.us_username,'') [assigned to]";
                }

                if (security.User.StatusFieldPermissionLevel != Security.PermissionNone)
                    select += ",\nisnull(st_name,'') [status]";

                if (security.User.UdfFieldPermissionLevel != Security.PermissionNone)
                    if (this.ShowUdf)
                    {
                        var udfName = Util.GetSetting("UserDefinedBugAttributeName", "YOUR ATTRIBUTE");
                        select += ",\nisnull(udf_name,'') [" + udfName + "]";
                    }

                // let results include custom columns
                var customColsSql = "";
                var userTypeCnt = 1;
                foreach (DataRow drcc in this.DsCustomCols.Tables[0].Rows)
                {
                    var columnName = (string) drcc["name"];
                    if (security.User.DictCustomFieldPermissionLevel[columnName] ==
                        Security.PermissionNone) continue;

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
                var selectedProjects = get_selected_projects();

                var projectDropdownSelectColsServerSide = "";

                string alias1 = null;
                string alias2 = null;
                string alias3 = null;

                foreach (var selectedProject in selectedProjects)
                {
                    if (selectedProject.Value == "0")
                        continue;

                    var pjId = Convert.ToInt32(selectedProject.Value);

                    if (this.MapProjects.ContainsKey(pjId))
                    {
                        var btnetProject = this.MapProjects[pjId];

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
                    projectDropdownSelectColsServerSide
                        += ",\nisnull(bg_project_custom_dropdown_value1,'') [" + alias1 + "]";
                if (alias2 != null)
                    projectDropdownSelectColsServerSide
                        += ",\nisnull(bg_project_custom_dropdown_value2,'') [" + alias2 + "]";
                if (alias3 != null)
                    projectDropdownSelectColsServerSide
                        += ",\nisnull(bg_project_custom_dropdown_value3,'') [" + alias3 + "]";

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
                foreach (DataRow drcc in this.DsCustomCols.Tables[0].Rows)
                {
                    var columnName = (string) drcc["name"];
                    if (security.User.DictCustomFieldPermissionLevel[columnName] ==
                        Security.PermissionNone) continue;

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

                if (this.ShowUdf)
                    select += "left outer join user_defined_attribute on udf_id = bg_user_defined_attribute";

                this.Sql = select + where + " order by bg_id desc";
            }
            else
            {
                searchSql = searchSql.Replace("[br]", "\n");
                this.Sql = searchSql.Replace("$WHERE$", where);
            }

            this.Sql = Util.AlterSqlPerProjectPermissions(this.Sql, security);

            var ds = DbUtil.GetDataSet(this.Sql);
            this.Dv = new DataView(ds.Tables[0]);
            Session["bugs"] = this.Dv;
            Session["bugs_unfiltered"] = ds.Tables[0];
        }

        public string format_from_date(string dt)
        {
            return Util.FormatLocalDateIntoDbFormat(dt).Replace(" 12:00:00", "").Replace(" 00:00:00", "");
        }

        public string format_to_date(string dt)
        {
            return Util.FormatLocalDateIntoDbFormat(dt).Replace(" 12:00:00", " 23:59:59")
                .Replace(" 00:00:00", " 23:59:59");
        }

        public void load_project_custom_dropdown(ListBox dropdown, string valsString,
            Dictionary<string, string> duplicateDetectionDictionary)
        {
            var valsArray = Util.SplitDropdownVals(valsString);
            for (var i = 0; i < valsArray.Length; i++)
                if (!duplicateDetectionDictionary.ContainsKey(valsArray[i]))
                {
                    dropdown.Items.Add(new ListItem(valsArray[i], "'" + valsArray[i].Replace("'", "''") + "'"));
                    duplicateDetectionDictionary.Add(valsArray[i], valsArray[i]);
                }
        }

        public void handle_project_custom_dropdowns()
        {
            // How many projects selected?
            var selectedProjects = get_selected_projects();
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
            foreach (ListItem li in this.project_custom_dropdown1.Items)
                if (li.Selected)
                    previousSelectionDictionaries[0].Add(li.Value, li.Value);
            foreach (ListItem li in this.project_custom_dropdown2.Items)
                if (li.Selected)
                    previousSelectionDictionaries[1].Add(li.Value, li.Value);
            foreach (ListItem li in this.project_custom_dropdown3.Items)
                if (li.Selected)
                    previousSelectionDictionaries[2].Add(li.Value, li.Value);

            this.ProjectDropdownSelectCols = "";

            this.project_custom_dropdown1_label.InnerText = "";
            this.project_custom_dropdown2_label.InnerText = "";
            this.project_custom_dropdown3_label.InnerText = "";

            this.project_custom_dropdown1.Items.Clear();
            this.project_custom_dropdown2.Items.Clear();
            this.project_custom_dropdown3.Items.Clear();

            foreach (var selectedProject in selectedProjects)
            {
                // Read the project dropdown info from the db.
                // Load the dropdowns as necessary

                if (selectedProject.Value == "0")
                    continue;

                var pjId = Convert.ToInt32(selectedProject.Value);

                if (this.MapProjects.ContainsKey(pjId))
                {
                    var btnetProject = this.MapProjects[pjId];

                    if (btnetProject.MapDropdowns[1].Enabled)
                    {
                        if (this.project_custom_dropdown1_label.InnerText == "")
                        {
                            this.project_custom_dropdown1_label.InnerText = btnetProject.MapDropdowns[1].Label;
                            this.project_custom_dropdown1_label.Style["display"] = "inline";
                            this.project_custom_dropdown1.Style["display"] = "block";
                        }
                        else if (this.project_custom_dropdown1_label.InnerText != btnetProject.MapDropdowns[1].Label)
                        {
                            this.project_custom_dropdown1_label.InnerText = "dropdown1";
                        }

                        load_project_custom_dropdown(this.project_custom_dropdown1,
                            btnetProject.MapDropdowns[1].Values,
                            dupeDetectionDictionaries[0]);
                    }

                    if (btnetProject.MapDropdowns[2].Enabled)
                    {
                        if (this.project_custom_dropdown2_label.InnerText == "")
                        {
                            this.project_custom_dropdown2_label.InnerText = btnetProject.MapDropdowns[2].Label;
                            this.project_custom_dropdown2_label.Style["display"] = "inline";
                            this.project_custom_dropdown2.Style["display"] = "block";
                        }
                        else if (this.project_custom_dropdown2_label.InnerText != btnetProject.MapDropdowns[2].Label)
                        {
                            this.project_custom_dropdown2_label.InnerText = "dropdown2";
                        }

                        load_project_custom_dropdown(this.project_custom_dropdown2,
                            btnetProject.MapDropdowns[2].Values,
                            dupeDetectionDictionaries[1]);
                    }

                    if (btnetProject.MapDropdowns[3].Enabled)
                    {
                        if (this.project_custom_dropdown3_label.InnerText == "")
                        {
                            this.project_custom_dropdown3_label.InnerText = btnetProject.MapDropdowns[3].Label;
                            this.project_custom_dropdown3_label.Style["display"] = "inline";
                            this.project_custom_dropdown3.Style["display"] = "block";
                            load_project_custom_dropdown(this.project_custom_dropdown3,
                                btnetProject.MapDropdowns[3].Values, dupeDetectionDictionaries[2]);
                        }
                        else if (this.project_custom_dropdown3_label.InnerText != btnetProject.MapDropdowns[3].Label)
                        {
                            this.project_custom_dropdown3_label.InnerText = "dropdown3";
                        }

                        load_project_custom_dropdown(this.project_custom_dropdown3,
                            btnetProject.MapDropdowns[3].Values,
                            dupeDetectionDictionaries[2]);
                    }
                }
            }

            if (this.project_custom_dropdown1_label.InnerText == "")
            {
                this.project_custom_dropdown1.Items.Clear();
                this.project_custom_dropdown1_label.Style["display"] = "none";
                this.project_custom_dropdown1.Style["display"] = "none";
            }
            else
            {
                this.project_custom_dropdown1_label.Style["display"] = "inline";
                this.project_custom_dropdown1.Style["display"] = "block";
                this.ProjectDropdownSelectCols
                    += ",\\nisnull(bg_project_custom_dropdown_value1,'') [" +
                       this.project_custom_dropdown1_label.InnerText + "]";
            }

            if (this.project_custom_dropdown2_label.InnerText == "")
            {
                this.project_custom_dropdown2.Items.Clear();
                this.project_custom_dropdown2_label.Style["display"] = "none";
                this.project_custom_dropdown2.Style["display"] = "none";
            }
            else
            {
                this.project_custom_dropdown2_label.Style["display"] = "inline";
                this.project_custom_dropdown2.Style["display"] = "block";
                this.ProjectDropdownSelectCols
                    += ",\\nisnull(bg_project_custom_dropdown_value2,'') [" +
                       this.project_custom_dropdown2_label.InnerText + "]";
            }

            if (this.project_custom_dropdown3_label.InnerText == "")
            {
                this.project_custom_dropdown3.Items.Clear();
                this.project_custom_dropdown3_label.Style["display"] = "none";
                this.project_custom_dropdown3.Style["display"] = "none";
            }
            else
            {
                this.project_custom_dropdown3_label.Style["display"] = "inline";
                this.project_custom_dropdown3.Style["display"] = "block";
                this.ProjectDropdownSelectCols
                    += ",\\nisnull(bg_project_custom_dropdown_value3,'') [" +
                       this.project_custom_dropdown3_label.InnerText + "]";
            }

            // Restore user's previous selections.
            foreach (ListItem li in this.project_custom_dropdown1.Items)
                li.Selected = previousSelectionDictionaries[0].ContainsKey(li.Value);
            foreach (ListItem li in this.project_custom_dropdown2.Items)
                li.Selected = previousSelectionDictionaries[1].ContainsKey(li.Value);
            foreach (ListItem li in this.project_custom_dropdown3.Items)
                li.Selected = previousSelectionDictionaries[2].ContainsKey(li.Value);
        }

        public void load_drop_downs(Security security)
        {
            this.reported_by.DataSource = this.DtUsers;
            this.reported_by.DataTextField = "us_username";
            this.reported_by.DataValueField = "us_id";
            this.reported_by.DataBind();

            // only show projects where user has permissions
            if (security.User.IsAdmin)
            {
                this.Sql = "/* drop downs */ select pj_id, pj_name from projects order by pj_name;";
            }
            else
            {
                this.Sql = @"/* drop downs */ select pj_id, pj_name
            from projects
            left outer join project_user_xref on pj_id = pu_project
            and pu_user = $us
            where isnull(pu_permission_level,$dpl) <> 0
            order by pj_name;";

                this.Sql = this.Sql.Replace("$us", Convert.ToString(security.User.Usid));
                this.Sql = this.Sql.Replace("$dpl", Util.GetSetting("DefaultPermissionLevel", "2"));
            }

            if (security.User.OtherOrgsPermissionLevel != 0)
            {
                this.Sql += " select og_id, og_name from orgs order by og_name;";
            }
            else
            {
                this.Sql += " select og_id, og_name from orgs where og_id = " +
                            Convert.ToInt32(security.User.Org) +
                            " order by og_name;";
                this.org.Visible = false;
                this.org_label.Visible = false;
            }

            this.Sql += @"
    select ct_id, ct_name from categories order by ct_sort_seq, ct_name;
    select pr_id, pr_name from priorities order by pr_sort_seq, pr_name;
    select st_id, st_name from statuses order by st_sort_seq, st_name;
    select udf_id, udf_name from user_defined_attribute order by udf_sort_seq, udf_name";

            var dsDropdowns = DbUtil.GetDataSet(this.Sql);

            this.project.DataSource = dsDropdowns.Tables[0];
            this.project.DataTextField = "pj_name";
            this.project.DataValueField = "pj_id";
            this.project.DataBind();
            this.project.Items.Insert(0, new ListItem("[no project]", "0"));

            this.org.DataSource = dsDropdowns.Tables[1];
            this.org.DataTextField = "og_name";
            this.org.DataValueField = "og_id";
            this.org.DataBind();
            this.org.Items.Insert(0, new ListItem("[no organization]", "0"));

            this.category.DataSource = dsDropdowns.Tables[2];
            this.category.DataTextField = "ct_name";
            this.category.DataValueField = "ct_id";
            this.category.DataBind();
            this.category.Items.Insert(0, new ListItem("[no category]", "0"));

            this.priority.DataSource = dsDropdowns.Tables[3];
            this.priority.DataTextField = "pr_name";
            this.priority.DataValueField = "pr_id";
            this.priority.DataBind();
            this.priority.Items.Insert(0, new ListItem("[no priority]", "0"));

            this.status.DataSource = dsDropdowns.Tables[4];
            this.status.DataTextField = "st_name";
            this.status.DataValueField = "st_id";
            this.status.DataBind();
            this.status.Items.Insert(0, new ListItem("[no status]", "0"));

            this.assigned_to.DataSource = this.reported_by.DataSource;
            this.assigned_to.DataTextField = "us_username";
            this.assigned_to.DataValueField = "us_id";
            this.assigned_to.DataBind();
            this.assigned_to.Items.Insert(0, new ListItem("[not assigned]", "0"));

            if (this.ShowUdf)
            {
                this.udf.DataSource = dsDropdowns.Tables[5];
                this.udf.DataTextField = "udf_name";
                this.udf.DataValueField = "udf_id";
                this.udf.DataBind();
                this.udf.Items.Insert(0, new ListItem("[none]", "0"));
            }

            if (security.User.ProjectFieldPermissionLevel == Security.PermissionNone)
            {
                this.project_label.Style["display"] = "none";
                this.project.Style["display"] = "none";
            }

            if (security.User.OrgFieldPermissionLevel == Security.PermissionNone)
            {
                this.org_label.Style["display"] = "none";
                this.org.Style["display"] = "none";
            }

            if (security.User.CategoryFieldPermissionLevel == Security.PermissionNone)
            {
                this.category_label.Style["display"] = "none";
                this.category.Style["display"] = "none";
            }

            if (security.User.PriorityFieldPermissionLevel == Security.PermissionNone)
            {
                this.priority_label.Style["display"] = "none";
                this.priority.Style["display"] = "none";
            }

            if (security.User.StatusFieldPermissionLevel == Security.PermissionNone)
            {
                this.status_label.Style["display"] = "none";
                this.status.Style["display"] = "none";
            }

            if (security.User.AssignedToFieldPermissionLevel == Security.PermissionNone)
            {
                this.assigned_to_label.Style["display"] = "none";
                this.assigned_to.Style["display"] = "none";
            }

            if (security.User.UdfFieldPermissionLevel == Security.PermissionNone)
            {
                this.udf_label.Style["display"] = "none";
                this.udf.Style["display"] = "none";
            }
        }

        public void write_custom_date_control(string name)
        {
            Response.Write("<input type=text class='txt date'");
            Response.Write("  onkeyup=\"on_change()\" ");
            var size = 10;
            var sizeString = Convert.ToString(size);

            Response.Write(" size=" + sizeString);
            Response.Write(" maxlength=" + sizeString);

            Response.Write(" name=\"" + name + "\"");
            Response.Write(" id=\"" + name.Replace(" ", "") + "\"");

            Response.Write(" value=\"");
            if (Request[name] != "") Response.Write(HttpUtility.HtmlEncode(Request[name]));
            Response.Write("\"");
            Response.Write(">");

            Response.Write("<a style='font-size: 8pt;'  href=\"javascript:show_calendar('");
            Response.Write(name.Replace(" ", ""));
            Response.Write("')\">&nbsp;[select]</a>");
        }

        public void write_custom_date_controls(string name)
        {
            Response.Write("from:&nbsp;&nbsp;");
            write_custom_date_control(name);
            Response.Write("&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;&nbsp;to:&nbsp;&nbsp;");
            write_custom_date_control("to__" + name); // magic
        }

        public void display_bugs(bool showCheckboxes, Security security)
        {
            BugList.DisplayBugs(
                showCheckboxes, this.Dv,
                Response, security, this.new_page.Value,
                IsPostBack, this.DsCustomCols, this.filter.Value);
        }

        public void call_sort_and_filter_buglist_dataview()
        {
            var filterVal = this.filter.Value;
            var sortVal = this.sort.Value;
            var prevSortVal = this.prev_sort.Value;
            var prevDirVal = this.prev_dir.Value;

            BugList.SortAndFilterBugListDataView(this.Dv, IsPostBack, this.actn.Value,
                ref filterVal,
                ref sortVal,
                ref prevSortVal,
                ref prevDirVal);

            this.filter.Value = filterVal;
            this.sort.Value = sortVal;
            this.prev_sort.Value = prevSortVal;
            this.prev_dir.Value = prevDirVal;
        }

        public class ProjectDropdown
        {
            public bool Enabled;
            public string Label = "";
            public string Values = "";
        }

        public class BtnetProject
        {
            public Dictionary<int, ProjectDropdown> MapDropdowns = new Dictionary<int, ProjectDropdown>();
        }
    }
}