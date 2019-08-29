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

    public partial class search : Page
    {
        public DataSet ds_custom_cols;

        public DataTable dt_users;
        public DataView dv;

        public Dictionary<int, BtnetProject> map_projects = new Dictionary<int, BtnetProject>();

        public string project_dropdown_select_cols = "";
        public Security security;
        public bool show_udf;

        public string sql;
        public bool use_full_names;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            if (this.security.user.is_admin || this.security.user.can_search)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "search";

            this.show_udf = Util.get_setting("ShowUserDefinedBugAttribute", "1") == "1";
            this.use_full_names = Util.get_setting("UseFullNames", "0") == "1";

            this.ds_custom_cols = Util.get_custom_columns();

            this.dt_users = Util.get_related_users(this.security, false);

            if (!IsPostBack)
            {
                load_drop_downs();
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

                var projects_with_custom_dropdowns = (int) DbUtil.execute_scalar(sql);

                if (projects_with_custom_dropdowns == 0) this.project.AutoPostBack = false;
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

                var ds_projects = DbUtil.get_dataset(sql);

                foreach (DataRow dr in ds_projects.Tables[0].Rows)
                {
                    var btnet_project = new BtnetProject();

                    ProjectDropdown dropdown;

                    dropdown = new ProjectDropdown();
                    dropdown.enabled = Convert.ToBoolean((int) dr["pj_enable_custom_dropdown1"]);
                    dropdown.label = (string) dr["pj_custom_dropdown_label1"];
                    dropdown.values = (string) dr["pj_custom_dropdown_values1"];
                    btnet_project.map_dropdowns[1] = dropdown;

                    dropdown = new ProjectDropdown();
                    dropdown.enabled = Convert.ToBoolean((int) dr["pj_enable_custom_dropdown2"]);
                    dropdown.label = (string) dr["pj_custom_dropdown_label2"];
                    dropdown.values = (string) dr["pj_custom_dropdown_values2"];
                    btnet_project.map_dropdowns[2] = dropdown;

                    dropdown = new ProjectDropdown();
                    dropdown.enabled = Convert.ToBoolean((int) dr["pj_enable_custom_dropdown3"]);
                    dropdown.label = (string) dr["pj_custom_dropdown_label3"];
                    dropdown.values = (string) dr["pj_custom_dropdown_values3"];
                    btnet_project.map_dropdowns[3] = dropdown;

                    this.map_projects[(int) dr["pj_id"]] = btnet_project;
                }

                // which button did the user hit?

                if (this.project_changed.Value == "1" && this.project.AutoPostBack)
                {
                    handle_project_custom_dropdowns();
                }
                else if (this.hit_submit_button.Value == "1")
                {
                    handle_project_custom_dropdowns();
                    do_query();
                }
                else
                {
                    this.dv = (DataView) Session["bugs"];
                    if (this.dv == null) do_query();
                    call_sort_and_filter_buglist_dataview();
                }
            }

            this.hit_submit_button.Value = "0";
            this.project_changed.Value = "0";

            if (this.security.user.is_admin || this.security.user.can_edit_sql)
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
                var and_or = this.and.Checked ? "and " : "or ";
                sql += and_or;
                sql += clause;
            }

            return sql;
        }

        public string build_clause_from_listbox(ListBox lb, string column_name)
        {
            var clause = "";
            foreach (ListItem li in lb.Items)
                if (li.Selected)
                {
                    if (clause == "")
                        clause += column_name + " in (";
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

            var s2 = Util.split_string_using_commas(s);
            for (var i = 0; i < s2.Length; i++)
            {
                if (opts != "") opts += ",";

                var one_opt = "N'";
                one_opt += s2[i].Replace("'", "''");
                one_opt += "'";

                opts += one_opt;
            }

            vals += opts;
            vals += ")";

            return vals;
        }

        public List<ListItem> get_selected_projects()
        {
            var selected_projects = new List<ListItem>();

            foreach (ListItem li in this.project.Items)
                if (li.Selected)
                    selected_projects.Add(li);

            return selected_projects;
        }

        public void do_query()
        {
            this.prev_sort.Value = "-1";
            this.prev_dir.Value = "ASC";
            this.new_page.Value = "0";

            // Create "WHERE" clause

            var where = "";

            var reported_by_clause = build_clause_from_listbox(this.reported_by, "bg_reported_user");
            var assigned_to_clause = build_clause_from_listbox(this.assigned_to, "bg_assigned_to_user");
            var project_clause = build_clause_from_listbox(this.project, "bg_project");

            var project_custom_dropdown1_clause
                = build_clause_from_listbox(this.project_custom_dropdown1, "bg_project_custom_dropdown_value1");
            var project_custom_dropdown2_clause
                = build_clause_from_listbox(this.project_custom_dropdown2, "bg_project_custom_dropdown_value2");
            var project_custom_dropdown3_clause
                = build_clause_from_listbox(this.project_custom_dropdown3, "bg_project_custom_dropdown_value3");

            var org_clause = build_clause_from_listbox(this.org, "bg_org");
            var category_clause = build_clause_from_listbox(this.category, "bg_category");
            var priority_clause = build_clause_from_listbox(this.priority, "bg_priority");
            var status_clause = build_clause_from_listbox(this.status, "bg_status");
            var udf_clause = "";

            if (this.show_udf) udf_clause = build_clause_from_listbox(this.udf, "bg_user_defined_attribute");

            // SQL "LIKE" uses [, %, and _ in a special way

            var like_string = this.like.Value.Replace("'", "''");
            like_string = like_string.Replace("[", "[[]");
            like_string = like_string.Replace("%", "[%]");
            like_string = like_string.Replace("_", "[_]");

            var like2_string = this.like2.Value.Replace("'", "''");
            like2_string = like2_string.Replace("[", "[[]");
            like2_string = like2_string.Replace("%", "[%]");
            like2_string = like2_string.Replace("_", "[_]");

            var desc_clause = "";
            if (this.like.Value != "")
            {
                desc_clause = " bg_short_desc like";
                desc_clause += " N'%" + like_string + "%'\n";
            }

            var comments_clause = "";
            if (this.like2.Value != "")
            {
                comments_clause =
                    " bg_id in (select bp_bug from bug_posts where bp_type in ('comment','received','sent') and isnull(bp_comment_search,bp_comment) like";
                comments_clause += " N'%" + like2_string + "%'";
                if (this.security.user.external_user) comments_clause += " and bp_hidden_from_external_users = 0";
                comments_clause += ")\n";
            }

            var comments_since_clause = "";
            if (this.comments_since.Value != "")
            {
                comments_since_clause =
                    " bg_id in (select bp_bug from bug_posts where bp_type in ('comment','received','sent') and bp_date > '";
                comments_since_clause += format_to_date(this.comments_since.Value);
                comments_since_clause += "')\n";
            }

            var from_clause = "";
            if (this.from_date.Value != "")
                from_clause = " bg_reported_date >= '" + format_from_date(this.from_date.Value) + "'\n";

            var to_clause = "";
            if (this.to_date.Value != "")
                to_clause = " bg_reported_date <= '" + format_to_date(this.to_date.Value) + "'\n";

            var lu_from_clause = "";
            if (this.lu_from_date.Value != "")
                lu_from_clause = " bg_last_updated_date >= '" + format_from_date(this.lu_from_date.Value) + "'\n";

            var lu_to_clause = "";
            if (this.lu_to_date.Value != "")
                lu_to_clause = " bg_last_updated_date <= '" + format_to_date(this.lu_to_date.Value) + "'\n";

            where = build_where(where, reported_by_clause);
            where = build_where(where, assigned_to_clause);
            where = build_where(where, project_clause);
            where = build_where(where, project_custom_dropdown1_clause);
            where = build_where(where, project_custom_dropdown2_clause);
            where = build_where(where, project_custom_dropdown3_clause);
            where = build_where(where, org_clause);
            where = build_where(where, category_clause);
            where = build_where(where, priority_clause);
            where = build_where(where, status_clause);
            where = build_where(where, desc_clause);
            where = build_where(where, comments_clause);
            where = build_where(where, comments_since_clause);
            where = build_where(where, from_clause);
            where = build_where(where, to_clause);
            where = build_where(where, lu_from_clause);
            where = build_where(where, lu_to_clause);

            if (this.show_udf) where = build_where(where, udf_clause);

            foreach (DataRow drcc in this.ds_custom_cols.Tables[0].Rows)
            {
                var column_name = (string) drcc["name"];
                if (this.security.user.dict_custom_field_permission_level[column_name] ==
                    Security.PERMISSION_NONE) continue;

                var values = Request[column_name];

                if (values != null)
                {
                    values = values.Replace("'", "''");

                    var custom_clause = "";

                    var datatype = (string) drcc["datatype"];

                    if ((datatype == "varchar" || datatype == "nvarchar" || datatype == "char" || datatype == "nchar")
                        && (string) drcc["dropdown type"] == "")
                    {
                        if (values != "")
                        {
                            custom_clause = " [" + column_name + "] like '%" + values + "%'\n";
                            where = build_where(where, custom_clause);
                        }
                    }
                    else if (datatype == "datetime")
                    {
                        if (values != "")
                        {
                            custom_clause = " [" + column_name + "] >= '" + format_from_date(values) + "'\n";
                            where = build_where(where, custom_clause);

                            // reset, and do the to date
                            custom_clause = "";
                            values = Request["to__" + column_name];
                            if (values != "")
                            {
                                custom_clause = " [" + column_name + "] <= '" + format_to_date(values) + "'\n";
                                where = build_where(where, custom_clause);
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
                            var in_not_in = format_in_not_in(values);
                            custom_clause = " [" + column_name + "] in " + in_not_in + "\n";
                            where = build_where(where, custom_clause);
                        }
                    }
                }
            }

            // The rest of the SQL is either built in or comes from Web.config

            var search_sql = Util.get_setting("SearchSQL", "");

            if (search_sql == "")
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
                if (this.use_full_names)
                    select += "\n,isnull(rpt.us_lastname + ', ' + rpt.us_firstname,'') [reported by]";
                else
                    select += "\n,isnull(rpt.us_username,'') [reported by]";
                select += "\n,bg_reported_date [reported on]";

                // last updated
                if (this.use_full_names)
                    select += "\n,isnull(lu.us_lastname + ', ' + lu.us_firstname,'') [last updated by]";
                else
                    select += "\n,isnull(lu.us_username,'') [last updated by]";
                select += "\n,bg_last_updated_date [last updated on]";

                if (this.security.user.tags_field_permission_level != Security.PERMISSION_NONE)
                    select += ",\nisnull(bg_tags,'') [tags]";

                if (this.security.user.project_field_permission_level != Security.PERMISSION_NONE)
                    select += ",\nisnull(pj_name,'') [project]";

                if (this.security.user.org_field_permission_level != Security.PERMISSION_NONE)
                    select += ",\nisnull(og_name,'') [organization]";

                if (this.security.user.category_field_permission_level != Security.PERMISSION_NONE)
                    select += ",\nisnull(ct_name,'') [category]";

                if (this.security.user.priority_field_permission_level != Security.PERMISSION_NONE)
                    select += ",\nisnull(pr_name,'') [priority]";

                if (this.security.user.assigned_to_field_permission_level != Security.PERMISSION_NONE)
                {
                    if (this.use_full_names)
                        select += ",\nisnull(asg.us_lastname + ', ' + asg.us_firstname,'') [assigned to]";
                    else
                        select += ",\nisnull(asg.us_username,'') [assigned to]";
                }

                if (this.security.user.status_field_permission_level != Security.PERMISSION_NONE)
                    select += ",\nisnull(st_name,'') [status]";

                if (this.security.user.udf_field_permission_level != Security.PERMISSION_NONE)
                    if (this.show_udf)
                    {
                        var udf_name = Util.get_setting("UserDefinedBugAttributeName", "YOUR ATTRIBUTE");
                        select += ",\nisnull(udf_name,'') [" + udf_name + "]";
                    }

                // let results include custom columns
                var custom_cols_sql = "";
                var user_type_cnt = 1;
                foreach (DataRow drcc in this.ds_custom_cols.Tables[0].Rows)
                {
                    var column_name = (string) drcc["name"];
                    if (this.security.user.dict_custom_field_permission_level[column_name] ==
                        Security.PERMISSION_NONE) continue;

                    if (Convert.ToString(drcc["dropdown type"]) == "users")
                    {
                        custom_cols_sql += ",\nisnull(users"
                                           + Convert.ToString(user_type_cnt++)
                                           + ".us_username,'') "
                                           + "["
                                           + column_name + "]";
                    }
                    else
                    {
                        if (Convert.ToString(drcc["datatype"]) == "decimal")
                            custom_cols_sql += ",\nisnull(["
                                               + column_name
                                               + "],0)["
                                               + column_name + "]";
                        else
                            custom_cols_sql += ",\nisnull(["
                                               + column_name
                                               + "],'')["
                                               + column_name + "]";
                    }
                }

                select += custom_cols_sql;

                // Handle project custom dropdowns
                var selected_projects = get_selected_projects();

                var project_dropdown_select_cols_server_side = "";

                string alias1 = null;
                string alias2 = null;
                string alias3 = null;

                foreach (var selected_project in selected_projects)
                {
                    if (selected_project.Value == "0")
                        continue;

                    var pj_id = Convert.ToInt32(selected_project.Value);

                    if (this.map_projects.ContainsKey(pj_id))
                    {
                        var btnet_project = this.map_projects[pj_id];

                        if (btnet_project.map_dropdowns[1].enabled)
                        {
                            if (alias1 == null)
                                alias1 = btnet_project.map_dropdowns[1].label;
                            else
                                alias1 = "dropdown1";
                        }

                        if (btnet_project.map_dropdowns[2].enabled)
                        {
                            if (alias2 == null)
                                alias2 = btnet_project.map_dropdowns[2].label;
                            else
                                alias2 = "dropdown2";
                        }

                        if (btnet_project.map_dropdowns[3].enabled)
                        {
                            if (alias3 == null)
                                alias3 = btnet_project.map_dropdowns[3].label;
                            else
                                alias3 = "dropdown3";
                        }
                    }
                }

                if (alias1 != null)
                    project_dropdown_select_cols_server_side
                        += ",\nisnull(bg_project_custom_dropdown_value1,'') [" + alias1 + "]";
                if (alias2 != null)
                    project_dropdown_select_cols_server_side
                        += ",\nisnull(bg_project_custom_dropdown_value2,'') [" + alias2 + "]";
                if (alias3 != null)
                    project_dropdown_select_cols_server_side
                        += ",\nisnull(bg_project_custom_dropdown_value3,'') [" + alias3 + "]";

                select += project_dropdown_select_cols_server_side;

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

                user_type_cnt = 1;
                foreach (DataRow drcc in this.ds_custom_cols.Tables[0].Rows)
                {
                    var column_name = (string) drcc["name"];
                    if (this.security.user.dict_custom_field_permission_level[column_name] ==
                        Security.PERMISSION_NONE) continue;

                    if (Convert.ToString(drcc["dropdown type"]) == "users")
                    {
                        select += "left outer join users users"
                                  + Convert.ToString(user_type_cnt)
                                  + " on users"
                                  + Convert.ToString(user_type_cnt)
                                  + ".us_id = bugs."
                                  + "[" + column_name + "]\n";

                        user_type_cnt++;
                    }
                }

                if (this.show_udf)
                    select += "left outer join user_defined_attribute on udf_id = bg_user_defined_attribute";

                this.sql = select + where + " order by bg_id desc";
            }
            else
            {
                search_sql = search_sql.Replace("[br]", "\n");
                this.sql = search_sql.Replace("$WHERE$", where);
            }

            this.sql = Util.alter_sql_per_project_permissions(this.sql, this.security);

            var ds = DbUtil.get_dataset(this.sql);
            this.dv = new DataView(ds.Tables[0]);
            Session["bugs"] = this.dv;
            Session["bugs_unfiltered"] = ds.Tables[0];
        }

        public string format_from_date(string dt)
        {
            return Util.format_local_date_into_db_format(dt).Replace(" 12:00:00", "").Replace(" 00:00:00", "");
        }

        public string format_to_date(string dt)
        {
            return Util.format_local_date_into_db_format(dt).Replace(" 12:00:00", " 23:59:59")
                .Replace(" 00:00:00", " 23:59:59");
        }

        public void load_project_custom_dropdown(ListBox dropdown, string vals_string,
            Dictionary<string, string> duplicate_detection_dictionary)
        {
            var vals_array = Util.split_dropdown_vals(vals_string);
            for (var i = 0; i < vals_array.Length; i++)
                if (!duplicate_detection_dictionary.ContainsKey(vals_array[i]))
                {
                    dropdown.Items.Add(new ListItem(vals_array[i], "'" + vals_array[i].Replace("'", "''") + "'"));
                    duplicate_detection_dictionary.Add(vals_array[i], vals_array[i]);
                }
        }

        public void handle_project_custom_dropdowns()
        {
            // How many projects selected?
            var selected_projects = get_selected_projects();
            var dupe_detection_dictionaries = new Dictionary<string, string>[3];
            var previous_selection_dictionaries = new Dictionary<string, string>[3];
            for (var i = 0; i < dupe_detection_dictionaries.Length; i++)
            {
                // Initialize Dictionary to accumulate ListItem values as they are added to the ListBox
                // so that duplicate values from multiple projects are not added to the ListBox twice.
                dupe_detection_dictionaries[i] = new Dictionary<string, string>();

                previous_selection_dictionaries[i] = new Dictionary<string, string>();
            }

            // Preserve user's previous selections (necessary if this is called during a postback).
            foreach (ListItem li in this.project_custom_dropdown1.Items)
                if (li.Selected)
                    previous_selection_dictionaries[0].Add(li.Value, li.Value);
            foreach (ListItem li in this.project_custom_dropdown2.Items)
                if (li.Selected)
                    previous_selection_dictionaries[1].Add(li.Value, li.Value);
            foreach (ListItem li in this.project_custom_dropdown3.Items)
                if (li.Selected)
                    previous_selection_dictionaries[2].Add(li.Value, li.Value);

            this.project_dropdown_select_cols = "";

            this.project_custom_dropdown1_label.InnerText = "";
            this.project_custom_dropdown2_label.InnerText = "";
            this.project_custom_dropdown3_label.InnerText = "";

            this.project_custom_dropdown1.Items.Clear();
            this.project_custom_dropdown2.Items.Clear();
            this.project_custom_dropdown3.Items.Clear();

            foreach (var selected_project in selected_projects)
            {
                // Read the project dropdown info from the db.
                // Load the dropdowns as necessary

                if (selected_project.Value == "0")
                    continue;

                var pj_id = Convert.ToInt32(selected_project.Value);

                if (this.map_projects.ContainsKey(pj_id))
                {
                    var btnet_project = this.map_projects[pj_id];

                    if (btnet_project.map_dropdowns[1].enabled)
                    {
                        if (this.project_custom_dropdown1_label.InnerText == "")
                        {
                            this.project_custom_dropdown1_label.InnerText = btnet_project.map_dropdowns[1].label;
                            this.project_custom_dropdown1_label.Style["display"] = "inline";
                            this.project_custom_dropdown1.Style["display"] = "block";
                        }
                        else if (this.project_custom_dropdown1_label.InnerText != btnet_project.map_dropdowns[1].label)
                        {
                            this.project_custom_dropdown1_label.InnerText = "dropdown1";
                        }

                        load_project_custom_dropdown(this.project_custom_dropdown1,
                            btnet_project.map_dropdowns[1].values,
                            dupe_detection_dictionaries[0]);
                    }

                    if (btnet_project.map_dropdowns[2].enabled)
                    {
                        if (this.project_custom_dropdown2_label.InnerText == "")
                        {
                            this.project_custom_dropdown2_label.InnerText = btnet_project.map_dropdowns[2].label;
                            this.project_custom_dropdown2_label.Style["display"] = "inline";
                            this.project_custom_dropdown2.Style["display"] = "block";
                        }
                        else if (this.project_custom_dropdown2_label.InnerText != btnet_project.map_dropdowns[2].label)
                        {
                            this.project_custom_dropdown2_label.InnerText = "dropdown2";
                        }

                        load_project_custom_dropdown(this.project_custom_dropdown2,
                            btnet_project.map_dropdowns[2].values,
                            dupe_detection_dictionaries[1]);
                    }

                    if (btnet_project.map_dropdowns[3].enabled)
                    {
                        if (this.project_custom_dropdown3_label.InnerText == "")
                        {
                            this.project_custom_dropdown3_label.InnerText = btnet_project.map_dropdowns[3].label;
                            this.project_custom_dropdown3_label.Style["display"] = "inline";
                            this.project_custom_dropdown3.Style["display"] = "block";
                            load_project_custom_dropdown(this.project_custom_dropdown3,
                                btnet_project.map_dropdowns[3].values, dupe_detection_dictionaries[2]);
                        }
                        else if (this.project_custom_dropdown3_label.InnerText != btnet_project.map_dropdowns[3].label)
                        {
                            this.project_custom_dropdown3_label.InnerText = "dropdown3";
                        }

                        load_project_custom_dropdown(this.project_custom_dropdown3,
                            btnet_project.map_dropdowns[3].values,
                            dupe_detection_dictionaries[2]);
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
                this.project_dropdown_select_cols
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
                this.project_dropdown_select_cols
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
                this.project_dropdown_select_cols
                    += ",\\nisnull(bg_project_custom_dropdown_value3,'') [" +
                       this.project_custom_dropdown3_label.InnerText + "]";
            }

            // Restore user's previous selections.
            foreach (ListItem li in this.project_custom_dropdown1.Items)
                li.Selected = previous_selection_dictionaries[0].ContainsKey(li.Value);
            foreach (ListItem li in this.project_custom_dropdown2.Items)
                li.Selected = previous_selection_dictionaries[1].ContainsKey(li.Value);
            foreach (ListItem li in this.project_custom_dropdown3.Items)
                li.Selected = previous_selection_dictionaries[2].ContainsKey(li.Value);
        }

        public void load_drop_downs()
        {
            this.reported_by.DataSource = this.dt_users;
            this.reported_by.DataTextField = "us_username";
            this.reported_by.DataValueField = "us_id";
            this.reported_by.DataBind();

            // only show projects where user has permissions
            if (this.security.user.is_admin)
            {
                this.sql = "/* drop downs */ select pj_id, pj_name from projects order by pj_name;";
            }
            else
            {
                this.sql = @"/* drop downs */ select pj_id, pj_name
			from projects
			left outer join project_user_xref on pj_id = pu_project
			and pu_user = $us
			where isnull(pu_permission_level,$dpl) <> 0
			order by pj_name;";

                this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));
                this.sql = this.sql.Replace("$dpl", Util.get_setting("DefaultPermissionLevel", "2"));
            }

            if (this.security.user.other_orgs_permission_level != 0)
            {
                this.sql += " select og_id, og_name from orgs order by og_name;";
            }
            else
            {
                this.sql += " select og_id, og_name from orgs where og_id = " +
                            Convert.ToInt32(this.security.user.org) +
                            " order by og_name;";
                this.org.Visible = false;
                this.org_label.Visible = false;
            }

            this.sql += @"
	select ct_id, ct_name from categories order by ct_sort_seq, ct_name;
	select pr_id, pr_name from priorities order by pr_sort_seq, pr_name;
	select st_id, st_name from statuses order by st_sort_seq, st_name;
	select udf_id, udf_name from user_defined_attribute order by udf_sort_seq, udf_name";

            var ds_dropdowns = DbUtil.get_dataset(this.sql);

            this.project.DataSource = ds_dropdowns.Tables[0];
            this.project.DataTextField = "pj_name";
            this.project.DataValueField = "pj_id";
            this.project.DataBind();
            this.project.Items.Insert(0, new ListItem("[no project]", "0"));

            this.org.DataSource = ds_dropdowns.Tables[1];
            this.org.DataTextField = "og_name";
            this.org.DataValueField = "og_id";
            this.org.DataBind();
            this.org.Items.Insert(0, new ListItem("[no organization]", "0"));

            this.category.DataSource = ds_dropdowns.Tables[2];
            this.category.DataTextField = "ct_name";
            this.category.DataValueField = "ct_id";
            this.category.DataBind();
            this.category.Items.Insert(0, new ListItem("[no category]", "0"));

            this.priority.DataSource = ds_dropdowns.Tables[3];
            this.priority.DataTextField = "pr_name";
            this.priority.DataValueField = "pr_id";
            this.priority.DataBind();
            this.priority.Items.Insert(0, new ListItem("[no priority]", "0"));

            this.status.DataSource = ds_dropdowns.Tables[4];
            this.status.DataTextField = "st_name";
            this.status.DataValueField = "st_id";
            this.status.DataBind();
            this.status.Items.Insert(0, new ListItem("[no status]", "0"));

            this.assigned_to.DataSource = this.reported_by.DataSource;
            this.assigned_to.DataTextField = "us_username";
            this.assigned_to.DataValueField = "us_id";
            this.assigned_to.DataBind();
            this.assigned_to.Items.Insert(0, new ListItem("[not assigned]", "0"));

            if (this.show_udf)
            {
                this.udf.DataSource = ds_dropdowns.Tables[5];
                this.udf.DataTextField = "udf_name";
                this.udf.DataValueField = "udf_id";
                this.udf.DataBind();
                this.udf.Items.Insert(0, new ListItem("[none]", "0"));
            }

            if (this.security.user.project_field_permission_level == Security.PERMISSION_NONE)
            {
                this.project_label.Style["display"] = "none";
                this.project.Style["display"] = "none";
            }

            if (this.security.user.org_field_permission_level == Security.PERMISSION_NONE)
            {
                this.org_label.Style["display"] = "none";
                this.org.Style["display"] = "none";
            }

            if (this.security.user.category_field_permission_level == Security.PERMISSION_NONE)
            {
                this.category_label.Style["display"] = "none";
                this.category.Style["display"] = "none";
            }

            if (this.security.user.priority_field_permission_level == Security.PERMISSION_NONE)
            {
                this.priority_label.Style["display"] = "none";
                this.priority.Style["display"] = "none";
            }

            if (this.security.user.status_field_permission_level == Security.PERMISSION_NONE)
            {
                this.status_label.Style["display"] = "none";
                this.status.Style["display"] = "none";
            }

            if (this.security.user.assigned_to_field_permission_level == Security.PERMISSION_NONE)
            {
                this.assigned_to_label.Style["display"] = "none";
                this.assigned_to.Style["display"] = "none";
            }

            if (this.security.user.udf_field_permission_level == Security.PERMISSION_NONE)
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
            var size_string = Convert.ToString(size);

            Response.Write(" size=" + size_string);
            Response.Write(" maxlength=" + size_string);

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

        public void display_bugs(bool show_checkboxes)
        {
            BugList.display_bugs(
                show_checkboxes, this.dv,
                Response, this.security, this.new_page.Value,
                IsPostBack, this.ds_custom_cols, this.filter.Value);
        }

        public void call_sort_and_filter_buglist_dataview()
        {
            var filter_val = this.filter.Value;
            var sort_val = this.sort.Value;
            var prev_sort_val = this.prev_sort.Value;
            var prev_dir_val = this.prev_dir.Value;

            BugList.sort_and_filter_buglist_dataview(this.dv, IsPostBack, this.actn.Value,
                ref filter_val,
                ref sort_val,
                ref prev_sort_val,
                ref prev_dir_val);

            this.filter.Value = filter_val;
            this.sort.Value = sort_val;
            this.prev_sort.Value = prev_sort_val;
            this.prev_dir.Value = prev_dir_val;
        }

        public class ProjectDropdown
        {
            public bool enabled;
            public string label = "";
            public string values = "";
        }

        public class BtnetProject
        {
            public Dictionary<int, ProjectDropdown> map_dropdowns = new Dictionary<int, ProjectDropdown>();
        }
    }
}