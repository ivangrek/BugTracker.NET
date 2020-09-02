using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BugTracker.Web.Migrations
{
    public partial class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "categories",
                columns: table => new
                {
                    ct_id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ct_name = table.Column<string>(maxLength: 80, nullable: false),
                    ct_sort_seq = table.Column<int>(nullable: false, defaultValue: 0),
                    ct_default = table.Column<int>(nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categories", x => x.ct_id);
                });

            migrationBuilder.CreateTable(
                name: "dashboard_items",
                columns: table => new
                {
                    ds_id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ds_user = table.Column<int>(nullable: false),
                    ds_report = table.Column<int>(nullable: false),
                    ds_chart_type = table.Column<string>(maxLength: 8, nullable: false),
                    ds_col = table.Column<int>(nullable: false),
                    ds_row = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_dashboard_items", x => x.ds_id);
                });

            migrationBuilder.CreateTable(
                name: "orgs",
                columns: table => new
                {
                    og_id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    og_name = table.Column<string>(maxLength: 80, nullable: false),
                    og_domain = table.Column<string>(maxLength: 80, nullable: true),
                    og_active = table.Column<int>(nullable: false, defaultValue: 1),
                    og_non_admins_can_use = table.Column<int>(nullable: false, defaultValue: 0),
                    og_external_user = table.Column<int>(nullable: false, defaultValue: 0),
                    og_can_be_assigned_to = table.Column<int>(nullable: false, defaultValue: 1),
                    og_can_only_see_own_reported = table.Column<int>(nullable: false, defaultValue: 0),
                    og_can_edit_sql = table.Column<int>(nullable: false, defaultValue: 0),
                    og_can_delete_bug = table.Column<int>(nullable: false, defaultValue: 0),
                    og_can_edit_and_delete_posts = table.Column<int>(nullable: false, defaultValue: 0),
                    og_can_merge_bugs = table.Column<int>(nullable: false, defaultValue: 0),
                    og_can_mass_edit_bugs = table.Column<int>(nullable: false, defaultValue: 0),
                    og_can_use_reports = table.Column<int>(nullable: false, defaultValue: 0),
                    og_can_edit_reports = table.Column<int>(nullable: false, defaultValue: 0),
                    og_can_view_tasks = table.Column<int>(nullable: false, defaultValue: 0),
                    og_can_edit_tasks = table.Column<int>(nullable: false, defaultValue: 0),
                    og_can_search = table.Column<int>(nullable: false, defaultValue: 1),
                    og_other_orgs_permission_level = table.Column<int>(nullable: false, defaultValue: 2),
                    og_can_assign_to_internal_users = table.Column<int>(nullable: false, defaultValue: 0),
                    og_category_field_permission_level = table.Column<int>(nullable: false, defaultValue: 2),
                    og_priority_field_permission_level = table.Column<int>(nullable: false, defaultValue: 2),
                    og_assigned_to_field_permission_level = table.Column<int>(nullable: false, defaultValue: 2),
                    og_status_field_permission_level = table.Column<int>(nullable: false, defaultValue: 2),
                    og_project_field_permission_level = table.Column<int>(nullable: false, defaultValue: 2),
                    og_org_field_permission_level = table.Column<int>(nullable: false, defaultValue: 2),
                    og_udf_field_permission_level = table.Column<int>(nullable: false, defaultValue: 2),
                    og_tags_field_permission_level = table.Column<int>(nullable: false, defaultValue: 2)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_orgs", x => x.og_id);
                });

            migrationBuilder.CreateTable(
                name: "projects",
                columns: table => new
                {
                    pj_id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    pj_name = table.Column<string>(maxLength: 80, nullable: false),
                    pj_active = table.Column<int>(nullable: false, defaultValue: 1),
                    pj_default_user = table.Column<int>(nullable: true),
                    pj_auto_assign_default_user = table.Column<int>(nullable: true),
                    pj_auto_subscribe_default_user = table.Column<int>(nullable: true),
                    pj_enable_pop3 = table.Column<int>(nullable: true),
                    pj_pop3_username = table.Column<string>(maxLength: 50, nullable: true),
                    pj_pop3_password = table.Column<string>(maxLength: 20, nullable: true),
                    pj_pop3_email_from = table.Column<string>(maxLength: 120, nullable: true),
                    pj_enable_custom_dropdown1 = table.Column<int>(nullable: false, defaultValue: 0),
                    pj_enable_custom_dropdown2 = table.Column<int>(nullable: false, defaultValue: 0),
                    pj_enable_custom_dropdown3 = table.Column<int>(nullable: false, defaultValue: 0),
                    pj_custom_dropdown_label1 = table.Column<string>(maxLength: 80, nullable: true),
                    pj_custom_dropdown_label2 = table.Column<string>(maxLength: 80, nullable: true),
                    pj_custom_dropdown_label3 = table.Column<string>(maxLength: 80, nullable: true),
                    pj_custom_dropdown_values1 = table.Column<string>(maxLength: 800, nullable: true),
                    pj_custom_dropdown_values2 = table.Column<string>(maxLength: 800, nullable: true),
                    pj_custom_dropdown_values3 = table.Column<string>(maxLength: 800, nullable: true),
                    pj_default = table.Column<int>(nullable: false, defaultValue: 0),
                    pj_description = table.Column<string>(maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_projects", x => x.pj_id);
                });

            migrationBuilder.CreateTable(
                name: "queries",
                columns: table => new
                {
                    qu_id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    qu_desc = table.Column<string>(maxLength: 200, nullable: false),
                    qu_sql = table.Column<string>(nullable: false),
                    qu_default = table.Column<int>(nullable: false),
                    qu_user = table.Column<int>(nullable: true),
                    qu_org = table.Column<int>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_queries", x => x.qu_id);
                });

            migrationBuilder.CreateTable(
                name: "reports",
                columns: table => new
                {
                    rp_id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    rp_desc = table.Column<string>(maxLength: 200, nullable: false),
                    rp_sql = table.Column<string>(nullable: false),
                    rp_chart_type = table.Column<string>(maxLength: 8, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_reports", x => x.rp_id);
                });

            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    us_id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    us_username = table.Column<string>(maxLength: 40, nullable: false),
                    us_salt = table.Column<string>(maxLength: 200, nullable: true),
                    us_password = table.Column<string>(maxLength: 200, nullable: false),
                    password_reset_key = table.Column<string>(maxLength: 200, nullable: true),
                    us_firstname = table.Column<string>(maxLength: 60, nullable: true),
                    us_lastname = table.Column<string>(maxLength: 60, nullable: true),
                    us_email = table.Column<string>(maxLength: 120, nullable: true),
                    us_admin = table.Column<int>(nullable: false, defaultValue: 0),
                    us_default_query = table.Column<int>(nullable: false, defaultValue: 0),
                    us_enable_notifications = table.Column<int>(nullable: false, defaultValue: 1),
                    us_auto_subscribe = table.Column<int>(nullable: false, defaultValue: 0),
                    us_auto_subscribe_own_bugs = table.Column<int>(nullable: true, defaultValue: 0),
                    us_auto_subscribe_reported_bugs = table.Column<int>(nullable: true, defaultValue: 0),
                    us_send_notifications_to_self = table.Column<int>(nullable: true, defaultValue: 0),
                    us_active = table.Column<int>(nullable: false, defaultValue: 1),
                    us_bugs_per_page = table.Column<int>(nullable: true),
                    us_forced_project = table.Column<int>(nullable: true),
                    us_reported_notifications = table.Column<int>(nullable: false, defaultValue: 4),
                    us_assigned_notifications = table.Column<int>(nullable: false, defaultValue: 4),
                    us_subscribed_notifications = table.Column<int>(nullable: false, defaultValue: 4),
                    us_signature = table.Column<string>(maxLength: 1000, nullable: true),
                    us_use_fckeditor = table.Column<int>(nullable: false, defaultValue: 0),
                    us_enable_bug_list_popups = table.Column<int>(nullable: false, defaultValue: 1),
                    us_created_user = table.Column<int>(nullable: false, defaultValue: 1),
                    us_org = table.Column<int>(nullable: false, defaultValue: 0),
                    us_most_recent_login_datetime = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.us_id);
                });

            migrationBuilder.InsertData(
                table: "categories",
                columns: new[] { "ct_id", "ct_name" },
                values: new object[,]
                {
                    { 1, "Bug" },
                    { 2, "Enhancement" },
                    { 3, "Task" },
                    { 4, "Question" },
                    { 5, "Ticket" }
                });

            migrationBuilder.InsertData(
                table: "orgs",
                columns: new[] { "og_id", "og_can_be_assigned_to", "og_domain", "og_name", "og_other_orgs_permission_level" },
                values: new object[,]
                {
                    { 1, 1, null, "org1", 2 },
                    { 2, 1, null, "developers", 2 },
                    { 3, 1, null, "testers", 2 }
                });

            migrationBuilder.InsertData(
                table: "orgs",
                columns: new[] { "og_id", "og_domain", "og_external_user", "og_name" },
                values: new object[,]
                {
                    { 4, null, 1, "client one" },
                    { 5, null, 1, "client two" }
                });

            migrationBuilder.InsertData(
                table: "projects",
                columns: new[] { "pj_id", "pj_auto_assign_default_user", "pj_auto_subscribe_default_user", "pj_custom_dropdown_label1", "pj_custom_dropdown_values1", "pj_custom_dropdown_label2", "pj_custom_dropdown_values2", "pj_custom_dropdown_label3", "pj_custom_dropdown_values3", "pj_default_user", "pj_description", "pj_enable_pop3", "pj_name", "pj_pop3_email_from", "pj_pop3_password", "pj_pop3_username" },
                values: new object[,]
                {
                    { 1, null, null, null, null, null, null, null, null, null, null, null, "Project 1", null, null, null },
                    { 2, null, null, null, null, null, null, null, null, null, null, null, "Project 2", null, null, null }
                });

            migrationBuilder.InsertData(
                table: "queries",
                columns: new[] { "qu_id", "qu_default", "qu_desc", "qu_org", "qu_sql", "qu_user" },
                values: new object[,]
                {
                    { 10, 0, "Demo votes feature", null, @"
                                        select ''#ffffff'', bg_id [id],
                                        (isnull(vote_total,0) * 10000) + isnull(bu_vote,0) [$VOTE],
                                        bg_short_desc [desc], isnull(st_name,'''') [status]
                                        from bugs
                                        left outer join bug_user on bu_bug = bg_id and bu_user = $ME
                                        left outer join votes_view on vote_bug = bg_id
                                        left outer join statuses on st_id = bg_status
                                        order by 3 desc", null },
                    { 9, 0, "Bugs with related bugs", null, @"
                                        select ''#ffffff'', bg_id [id], bg_short_desc [desc],
                                        isnull(st_name,'''') [status],
                                        count(*) [number of related bugs]
                                        from bugs
                                        inner join bug_relationships on re_bug1 = bg_id
                                        inner join statuses on bg_status = st_id
                                        /*ENDWHR*/
                                        group by bg_id, bg_short_desc, isnull(st_name,'''')
                                        order by bg_id desc ", null },
                    { 7, 0, "Days in status", null, @"
                                        select case 
                                        when datediff(d, isnull(bp_date,bg_reported_date), getdate()) > 90 then ''#ff9999''
                                        when datediff(d, isnull(bp_date,bg_reported_date), getdate()) > 30 then ''#ffcccc''
                                        when datediff(d, isnull(bp_date,bg_reported_date), getdate()) > 7 then ''#ffdddd''
                                        else ''#ffffff'' end,
                                        bg_id [id], bg_short_desc [desc],
                                        datediff(d, isnull(bp_date,bg_reported_date), getdate()) [days in status],
                                        st_name [status],
                                        isnull(bp_comment,'''') [last status change], isnull(bp_date,bg_reported_date) [status date]
                                        from bugs
                                        inner join statuses on bg_status = st_id
                                        left outer join bug_posts on bg_id = bp_bug
                                        and bp_type = ''update'' 
                                        and bp_comment like ''changed status from%''
                                        and bp_date in (select max(bp_date) from bug_posts where bp_bug = bg_id)
                                        WhErE 1 = 1
                                        order by 4 desc", null },
                    { 6, 0, "Demo last comment as column", null, @"
                                        select ''#ffffff'', bg_id [id], bg_short_desc [desc], 
                                        substring(bp_comment_search,1,40) [last comment], bp_date [last comment date]
                                        from bugs
                                        left outer join bug_posts on bg_id = bp_bug
                                        and bp_type = ''comment''' 
                                        and bp_date in (select max(bp_date) from bug_posts where bp_bug = bg_id)
                                        WhErE 1 = 1
                                        order by bg_id desc", null },
                    { 8, 0, "Bugs with attachments", null, @"
                                        select bp_bug, sum(bp_size) bytes
                                        into #t
                                        from bug_posts
                                        where bp_type = ''file''
                                        group by bp_bug '
                                        select ''#ffffff'', bg_id [id], bg_short_desc [desc],
                                        bytes, rpt.us_username [reported by]
                                        from bugs
                                        inner join #t on bp_bug = bg_id
                                        left outer join users rpt on rpt.us_id = bg_reported_user
                                        WhErE 1 = 1
                                        order by bytes desc
                                        drop table #t", null },
                    { 4, 0, "Checked in bugs - for QA", null, @"
                                        select isnull(pr_background_color,''#ffffff''), bg_id [id], isnull(bu_flag,0) [$FLAG],
                                        bg_short_desc [desc], isnull(pj_name,'''') [project], isnull(og_name,'''') [organization], isnull(ct_name,'''') [category], rpt.us_username [reported by],
                                        bg_reported_date [reported on], isnull(pr_name,'''') [priority], isnull(asg.us_username,'''') [assigned to],'
                                        isnull(st_name,'''') [status], isnull(lu.us_username,'''') [last updated by], bg_last_updated_date [last updated on]
                                        from bugs
                                        left outer join bug_user on bu_bug = bg_id and bu_user = $ME
                                        left outer join users rpt on rpt.us_id = bg_reported_user
                                        left outer join users asg on asg.us_id = bg_assigned_to_user
                                        left outer join users lu on lu.us_id = bg_last_updated_user
                                        left outer join projects on pj_id = bg_project
                                        left outer join orgs on og_id = bg_org
                                        left outer join categories on ct_id = bg_category
                                        left outer join priorities on pr_id = bg_priority
                                        left outer join statuses on st_id = bg_status
                                        where bg_status = 3 order by bg_id desc", null },
                    { 3, 0, "Open bugs assigned to me", null, @"
                                        select isnull(pr_background_color,''#ffffff''), bg_id [id], isnull(bu_flag,0) [$FLAG],
                                        bg_short_desc [desc], isnull(pj_name,'''') [project], isnull(og_name,'''') [organization], isnull(ct_name,'''') [category], rpt.us_username [reported by],
                                        bg_reported_date [reported on], isnull(pr_name,'''') [priority], isnull(asg.us_username,'''') [assigned to],
                                        isnull(st_name,'''') [status], isnull(lu.us_username,'''') [last updated by], bg_last_updated_date [last updated on]
                                        from bugs
                                        left outer join bug_user on bu_bug = bg_id and bu_user = $ME
                                        left outer join users rpt on rpt.us_id = bg_reported_user
                                        left outer join users asg on asg.us_id = bg_assigned_to_user
                                        left outer join users lu on lu.us_id = bg_last_updated_user
                                        left outer join projects on pj_id = bg_project
                                        left outer join orgs on og_id = bg_org
                                        left outer join categories on ct_id = bg_category
                                        left outer join priorities on pr_id = bg_priority
                                        left outer join statuses on st_id = bg_status
                                        where bg_status <> 5 and bg_assigned_to_user = $ME order by bg_id desc", null },
                    { 2, 0, "Open bugs", null, @"
                                        select isnull(pr_background_color,''#ffffff''), bg_id [id], isnull(bu_flag,0) [$FLAG],
                                        bg_short_desc [desc], isnull(pj_name,'''') [project], isnull(og_name,'''') [organization], isnull(ct_name,'''') [category], rpt.us_username [reported by],
                                        bg_reported_date [reported on], isnull(pr_name,'''') [priority], isnull(asg.us_username,'''') [assigned to],
                                        isnull(st_name,'''') [status], isnull(lu.us_username,'''') [last updated by], bg_last_updated_date [last updated on]
                                        from bugs
                                        left outer join bug_user on bu_bug = bg_id and bu_user = $ME
                                        left outer join users rpt on rpt.us_id = bg_reported_user
                                        left outer join users asg on asg.us_id = bg_assigned_to_user
                                        left outer join users lu on lu.us_id = bg_last_updated_user
                                        left outer join projects on pj_id = bg_project
                                        left outer join orgs on og_id = bg_org
                                        left outer join categories on ct_id = bg_category
                                        left outer join priorities on pr_id = bg_priority
                                        left outer join statuses on st_id = bg_status
                                        where bg_status <> 5 order by bg_id desc", null },
                    { 1, 1, "All bugs", null, @"
                                        select isnull(pr_background_color,''#ffffff''), bg_id [id], isnull(bu_flag,0) [$FLAG], 
                                        bg_short_desc [desc], isnull(pj_name,'''') [project], isnull(og_name,'''') [organization], isnull(ct_name,'''') [category], rpt.us_username [reported by],
                                        bg_reported_date [reported on], isnull(pr_name,'''') [priority], isnull(asg.us_username,'''') [assigned to],
                                        isnull(st_name,'''') [status], isnull(lu.us_username,'''') [last updated by], bg_last_updated_date [last updated on]
                                        from bugs
                                        left outer join bug_user on bu_bug = bg_id and bu_user = $ME
                                        left outer join users rpt on rpt.us_id = bg_reported_user
                                        left outer join users asg on asg.us_id = bg_assigned_to_user
                                        left outer join users lu on lu.us_id = bg_last_updated_user
                                        left outer join projects on pj_id = bg_project
                                        left outer join orgs on og_id = bg_org
                                        left outer join categories on ct_id = bg_category
                                        left outer join priorities on pr_id = bg_priority
                                        left outer join statuses on st_id = bg_status
                                        order by bg_id desc", null },
                    { 5, 0, "Demo use of css classes", null, @"
                                        select isnull(pr_style + st_style,''datad''), bg_id [id], isnull(bu_flag,0) [$FLAG], bg_short_desc [desc], isnull(pr_name,'''') [priority], isnull(st_name,'''') [status]
                                        from bugs
                                        left outer join bug_user on bu_bug = bg_id and bu_user = $ME
                                        left outer join priorities on pr_id = bg_priority
                                        left outer join statuses on st_id = bg_status
                                        order by bg_id desc", null }
                });

            migrationBuilder.InsertData(
                table: "reports",
                columns: new[] { "rp_id", "rp_chart_type", "rp_desc", "rp_sql" },
                values: new object[,]
                {
                    { 8, "table", "Hours Remaining by Project", @"
                                        select
                                            pj_name [project],
                                            convert(
                                                decimal(8,1),
                                                sum(
                                                    case
                                                        when tsk_duration_units = ''minutes''
                                                            then tsk_planned_duration / 60.0 * .01 * (100 - isnull(tsk_percent_complete,0))
                                                        when tsk_duration_units = ''days''
                                                            then tsk_planned_duration * 8.0  * .01 * (100 - isnull(tsk_percent_complete,0))
                                                        else tsk_planned_duration * .01 * (100 - isnull(tsk_percent_complete,0))
                                                    end
                                                )
                                            ) [total hours]
                                        from
                                            bug_tasks

                                            inner join bugs on tsk_bug = bg_id
                                            inner join projects on bg_project = pj_id
                                        where
                                            isnull(tsk_planned_duration,0) <> 0
                                        group
                                            by pj_name" },
                    { 7, "table", "Hours by Org, Year, Month", @"
                                        select
                                            og_name [organization],
                                            datepart(year,tsk_created_date) [year],
                                            datepart(month,tsk_created_date) [month],
                                            convert(decimal(8,1),
                                            sum( 
                                                case 
                                                when tsk_duration_units = ''minutes''
                                                    then tsk_actual_duration / 60.0
                                                when tsk_duration_units = ''days''
                                                    then tsk_actual_duration * 8.0
                                                else tsk_actual_duration * 1.0 end)
                                            ) [total hours]
                                        from
                                            bug_tasks

                                            inner join bugs on tsk_bug = bg_id
                                            inner join orgs on bg_org = og_id
                                        where
                                            isnull(tsk_actual_duration,0) <> 0
                                        group by
                                            og_name,
                                            datepart(year,tsk_created_date),
                                            datepart(month,tsk_created_date)" },
                    { 5, "line", "Bugs by Day of Year", @"
                                        select
                                            datepart(dy, bg_reported_date) [day of year],
                                            count(1) [count]
                                        from
                                            bugs
                                        group by
                                            datepart(dy, bg_reported_date),
                                            datepart(dy,bg_reported_date)
                                        order by 1" },
                    { 6, "table", "Bugs by User", @"
                                        select
                                            bg_reported_user,
                                            count(1) [r]
                                        into #t
                                        from
                                            bugs
                                        group by
                                            bg_reported_user;

                                        select
                                            bg_assigned_to_user,
                                            count(1) [a]
                                        into #t2
                                        from
                                            bugs
                                        group by
                                            bg_assigned_to_user;

                                        select
                                            us_username,
                                            r [reported],
                                            a [assigned]
                                        from
                                            users
                                            
                                            left outer join #t on bg_reported_user = us_id
                                            left outer join #t2 on bg_assigned_to_user = us_id
                                        order by 1" },
                    { 3, "pie", "Bugs by Category", @"
                                        select
                                            ct_name [category],
                                            count(1) [count]
                                        from
                                            bugs

                                            inner join categories on bg_category = ct_id
                                        group
                                            by ct_name
                                        order by
                                            ct_name" },
                    { 2, "pie", "Bugs by Priority", @"
                                        select
                                            pr_name [priority],
                                            count(1) [count]
                                        from
                                            bugs

                                            inner join priorities on bg_priority = pr_id
                                        group by
                                            pr_name
                                        order by
                                            pr_name" },
                    { 1, "pie", "Bugs by Status", @"
                                        select
                                            st_name [status],
                                            count(1) [count]
                                        from
                                            bugs 

                                            inner join statuses on bg_status = st_id
                                        group by
                                            st_name
                                        order by
                                            st_name" },
                    { 4, "bar", "Bugs by Month", @"
                                        select
                                            month(bg_reported_date) [month],
                                            count(1) [count]
                                        from
                                            bugs
                                        group by
                                            year(bg_reported_date),
                                            month(bg_reported_date)
                                        order by
                                            year(bg_reported_date),
                                            month(bg_reported_date)" }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "us_id", "us_active", "us_bugs_per_page", "us_default_query", "us_email", "us_firstname", "us_forced_project", "us_lastname", "us_most_recent_login_datetime", "us_org", "us_password", "password_reset_key", "us_salt", "us_signature", "us_username" },
                values: new object[] { 8, 1, null, 1, null, "Report And", 1, "Comment Only", null, 1, "*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$", null, "uTgBGWekorP3r", null, "reporter" });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "us_id", "us_active", "us_admin", "us_bugs_per_page", "us_default_query", "us_email", "us_firstname", "us_forced_project", "us_lastname", "us_most_recent_login_datetime", "us_org", "us_password", "password_reset_key", "us_salt", "us_signature", "us_username" },
                values: new object[] { 1, 1, 1, null, 1, null, "System", null, "Administrator", null, 1, "*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$", null, "uTgBGWekorP3r", null, "admin" });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "us_id", "us_active", "us_bugs_per_page", "us_default_query", "us_email", "us_firstname", "us_forced_project", "us_lastname", "us_most_recent_login_datetime", "us_org", "us_password", "password_reset_key", "us_salt", "us_signature", "us_username" },
                values: new object[,]
                {
                    { 2, 1, null, 2, null, "Al", null, "Kaline", null, 2, "*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$", null, "uTgBGWekorP3r", null, "developer" },
                    { 3, 1, null, 4, null, "Norman", null, "Cash", null, 4, "*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$", null, "uTgBGWekorP3r", null, "tester" },
                    { 4, 1, null, 1, null, "Bill", null, "Freehan", null, 4, "*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$", null, "uTgBGWekorP3r", null, "customer1" },
                    { 5, 1, null, 1, null, "Denny", null, "McClain", null, 5, "*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$", null, "uTgBGWekorP3r", null, "customer2" },
                    { 6, 1, null, 1, null, "for POP3", null, "BugTracker.MailService.exe", null, 1, "*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$", null, "uTgBGWekorP3r", null, "email" },
                    { 7, 1, null, 1, null, "Read", 1, "Only", null, 1, "*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$", null, "uTgBGWekorP3r", null, "viewer" }
                });

            migrationBuilder.InsertData(
                table: "users",
                columns: new[] { "us_id", "us_bugs_per_page", "us_default_query", "us_email", "us_firstname", "us_forced_project", "us_lastname", "us_most_recent_login_datetime", "us_org", "us_password", "password_reset_key", "us_salt", "us_signature", "us_username" },
                values: new object[] { 9, null, 1, null, "Special", 1, "Cannot save searches", null, 1, "*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$", null, "uTgBGWekorP3r", null, "guest" });

            migrationBuilder.CreateIndex(
                name: "IX_categories_ct_name",
                table: "categories",
                column: "ct_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_dashboard_items_ds_user",
                table: "dashboard_items",
                column: "ds_user");

            migrationBuilder.CreateIndex(
                name: "IX_orgs_og_name",
                table: "orgs",
                column: "og_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_projects_pj_name",
                table: "projects",
                column: "pj_name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_queries_qu_desc_qu_user_qu_org",
                table: "queries",
                columns: new[] { "qu_desc", "qu_user", "qu_org" },
                unique: true,
                filter: "[qu_user] IS NOT NULL AND [qu_org] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_reports_rp_desc",
                table: "reports",
                column: "rp_desc",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_users_us_username",
                table: "users",
                column: "us_username",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "categories");

            migrationBuilder.DropTable(
                name: "dashboard_items");

            migrationBuilder.DropTable(
                name: "orgs");

            migrationBuilder.DropTable(
                name: "projects");

            migrationBuilder.DropTable(
                name: "queries");

            migrationBuilder.DropTable(
                name: "reports");

            migrationBuilder.DropTable(
                name: "users");
        }
    }
}
