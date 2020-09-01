namespace BugTracker.Web.Core.Persistence
{
    using BugTracker.Web.Core.Persistence.Configurations;
    using BugTracker.Web.Core.Persistence.Models;
    using Microsoft.EntityFrameworkCore;

    public sealed class BtNetDbContext : DbContext
    {
        public BtNetDbContext(DbContextOptions<BtNetDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }

        public DbSet<Organization> Organizations { get; set; }

        public DbSet<Query> Queries { get; set; }

        public DbSet<Report> Reports { get; set; }

        public DbSet<DashboardItem> DashboardItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new UserConfiguration());
            modelBuilder.ApplyConfiguration(new OrganizationConfiguration());
            modelBuilder.ApplyConfiguration(new QueryConfiguration());
            modelBuilder.ApplyConfiguration(new ReportConfiguration());
            modelBuilder.ApplyConfiguration(new DashboardItemConfiguration());

            Seed(modelBuilder);
        }

        private void Seed(ModelBuilder modelBuilder)
        {
            #region User

            modelBuilder.Entity<User>()
                .HasData(new User
                {
                    Id = 1,
                    Username = "admin",
                    Salt = "uTgBGWekorP3r",
                    Password = @"*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$",
                    FirstName = "System",
                    LastName = "Administrator",
                    Admin = 1,
                    DefaultQueryId = 1,
                    OrganizationId = 1,
                    ForcedProjectId = null,
                    Active = 1
                });

            modelBuilder.Entity<User>()
                .HasData(new User
                {
                    Id = 2,
                    Username = "developer",
                    Salt = "uTgBGWekorP3r",
                    Password = @"*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$",
                    FirstName = "Al",
                    LastName = "Kaline",
                    Admin = 0,
                    DefaultQueryId = 2,
                    OrganizationId = 2,
                    ForcedProjectId = null,
                    Active = 1
                });

            modelBuilder.Entity<User>()
                .HasData(new User
                {
                    Id = 3,
                    Username = "tester",
                    Salt = "uTgBGWekorP3r",
                    Password = @"*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$",
                    FirstName = "Norman",
                    LastName = "Cash",
                    Admin = 0,
                    DefaultQueryId = 4,
                    OrganizationId = 4,
                    ForcedProjectId = null,
                    Active = 1
                });

            modelBuilder.Entity<User>()
                .HasData(new User
                {
                    Id = 4,
                    Username = "customer1",
                    Salt = "uTgBGWekorP3r",
                    Password = @"*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$",
                    FirstName = "Bill",
                    LastName = "Freehan",
                    Admin = 0,
                    DefaultQueryId = 1,
                    OrganizationId = 4,
                    ForcedProjectId = null,
                    Active = 1
                });

            modelBuilder.Entity<User>()
                .HasData(new User
                {
                    Id = 5,
                    Username = "customer2",
                    Salt = "uTgBGWekorP3r",
                    Password = @"*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$",
                    FirstName = "Denny",
                    LastName = "McClain",
                    Admin = 0,
                    DefaultQueryId = 1,
                    OrganizationId = 5,
                    ForcedProjectId = null,
                    Active = 1
                });

            modelBuilder.Entity<User>()
                .HasData(new User
                {
                    Id = 6,
                    Username = "email",
                    Salt = "uTgBGWekorP3r",
                    Password = @"*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$",
                    FirstName = "for POP3",
                    LastName = "BugTracker.MailService.exe",
                    Admin = 0,
                    DefaultQueryId = 1,
                    OrganizationId = 1,
                    ForcedProjectId = null,
                    Active = 1
                });

            modelBuilder.Entity<User>()
                .HasData(new User
                {
                    Id = 7,
                    Username = "viewer",
                    Salt = "uTgBGWekorP3r",
                    Password = @"*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$",
                    FirstName = "Read",
                    LastName = "Only",
                    Admin = 0,
                    DefaultQueryId = 1,
                    OrganizationId = 1,
                    ForcedProjectId = 1,
                    Active = 1
                });

            modelBuilder.Entity<User>()
                .HasData(new User
                {
                    Id = 8,
                    Username = "reporter",
                    Salt = "uTgBGWekorP3r",
                    Password = @"*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$",
                    FirstName = "Report And",
                    LastName = "Comment Only",
                    Admin = 0,
                    DefaultQueryId = 1,
                    OrganizationId = 1,
                    ForcedProjectId = 1,
                    Active = 1
                });

            modelBuilder.Entity<User>()
                .HasData(new User
                {
                    Id = 9,
                    Username = "guest",
                    Salt = "uTgBGWekorP3r",
                    Password = @"*�d6�t�d>bK�6V�)&u8E�R��e����sA�L<+���~Yf�������jb��5���t˯9���D���&d���B��kHs�5�׌L��1�g���F���䁪N�Ke4~����c^$",
                    FirstName = "Special",
                    LastName = "Cannot save searches",
                    Admin = 0,
                    DefaultQueryId = 1,
                    OrganizationId = 1,
                    ForcedProjectId = 1,
                    Active = 0
                });

            #endregion User

            #region Organization

            modelBuilder.Entity<Organization>()
                .HasData(new Organization
                {
                    Id = 1,
                    Name = "org1",
                    ExternalUser = 0,
                    CanBeAssignedTo = 1,
                    OtherOrgsPermissionLevel = 2
                });

            modelBuilder.Entity<Organization>()
                .HasData(new Organization
                {
                    Id = 2,
                    Name = "developers",
                    ExternalUser = 0,
                    CanBeAssignedTo = 1,
                    OtherOrgsPermissionLevel = 2
                });

            modelBuilder.Entity<Organization>()
                .HasData(new Organization
                {
                    Id = 3,
                    Name = "testers",
                    ExternalUser = 0,
                    CanBeAssignedTo = 1,
                    OtherOrgsPermissionLevel = 2
                });

            modelBuilder.Entity<Organization>()
                .HasData(new Organization
                {
                    Id = 4,
                    Name = "client one",
                    ExternalUser = 1,
                    CanBeAssignedTo = 0,
                    OtherOrgsPermissionLevel = 0
                });

            modelBuilder.Entity<Organization>()
                .HasData(new Organization
                {
                    Id = 5,
                    Name = "client two",
                    ExternalUser = 1,
                    CanBeAssignedTo = 0,
                    OtherOrgsPermissionLevel = 0
                });

            #endregion Organization

            #region Query

            modelBuilder.Entity<Query>()
                .HasData(new Query
                {
                    Id = 1,
                    Name = "All bugs",
                    Sql = @"
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
                        order by bg_id desc",
                    Default = 1
                });

            modelBuilder.Entity<Query>()
                .HasData(new Query
                {
                    Id = 2,
                    Name = "Open bugs",
                    Sql = @"
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
                        where bg_status <> 5 order by bg_id desc",
                    Default = 0
                });

            modelBuilder.Entity<Query>()
                .HasData(new Query
                {
                    Id = 3,
                    Name = "Open bugs assigned to me",
                    Sql = @"
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
                        where bg_status <> 5 and bg_assigned_to_user = $ME order by bg_id desc",
                    Default = 0
                });

            modelBuilder.Entity<Query>()
                .HasData(new Query
                {
                    Id = 4,
                    Name = "Checked in bugs - for QA",
                    Sql = @"
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
                        where bg_status = 3 order by bg_id desc",
                    Default = 0
                });

            modelBuilder.Entity<Query>()
                .HasData(new Query
                {
                    Id = 5,
                    Name = "Demo use of css classes",
                    Sql = @"
                        select isnull(pr_style + st_style,''datad''), bg_id [id], isnull(bu_flag,0) [$FLAG], bg_short_desc [desc], isnull(pr_name,'''') [priority], isnull(st_name,'''') [status]
                        from bugs
                        left outer join bug_user on bu_bug = bg_id and bu_user = $ME
                        left outer join priorities on pr_id = bg_priority
                        left outer join statuses on st_id = bg_status
                        order by bg_id desc",
                    Default = 0
                });

            modelBuilder.Entity<Query>()
                .HasData(new Query
                {
                    Id = 6,
                    Name = "Demo last comment as column",
                    Sql = @"
                        select ''#ffffff'', bg_id [id], bg_short_desc [desc], 
                        substring(bp_comment_search,1,40) [last comment], bp_date [last comment date]
                        from bugs
                        left outer join bug_posts on bg_id = bp_bug
                        and bp_type = ''comment''' 
                        and bp_date in (select max(bp_date) from bug_posts where bp_bug = bg_id)
                        WhErE 1 = 1
                        order by bg_id desc",
                    Default = 0
                });

            modelBuilder.Entity<Query>()
                .HasData(new Query
                {
                    Id = 7,
                    Name = "Days in status",
                    Sql = @"
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
                        order by 4 desc",
                    Default = 0
                });

            modelBuilder.Entity<Query>()
                .HasData(new Query
                {
                    Id = 8,
                    Name = "Bugs with attachments",
                    Sql = @"
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
                        drop table #t",
                    Default = 0
                });

            modelBuilder.Entity<Query>()
                .HasData(new Query
                {
                    Id = 9,
                    Name = "Bugs with related bugs",
                    Sql = @"
                        select ''#ffffff'', bg_id [id], bg_short_desc [desc],
                        isnull(st_name,'''') [status],
                        count(*) [number of related bugs]
                        from bugs
                        inner join bug_relationships on re_bug1 = bg_id
                        inner join statuses on bg_status = st_id
                        /*ENDWHR*/
                        group by bg_id, bg_short_desc, isnull(st_name,'''')
                        order by bg_id desc ",
                    Default = 0
                });

            modelBuilder.Entity<Query>()
                .HasData(new Query
                {
                    Id = 10,
                    Name = "Demo votes feature",
                    Sql = @"
                        select ''#ffffff'', bg_id [id],
                        (isnull(vote_total,0) * 10000) + isnull(bu_vote,0) [$VOTE],
                        bg_short_desc [desc], isnull(st_name,'''') [status]
                        from bugs
                        left outer join bug_user on bu_bug = bg_id and bu_user = $ME
                        left outer join votes_view on vote_bug = bg_id
                        left outer join statuses on st_id = bg_status
                        order by 3 desc",
                    Default = 0
                });

            #endregion Query

            #region Report

            modelBuilder.Entity<Report>()
                .HasData(new Report
                {
                    Id = 1,
                    Name = "Bugs by Status",
                    Sql = @"
                        select
                            st_name [status],
                            count(1) [count]
                        from
                            bugs 

                            inner join statuses on bg_status = st_id
                        group by
                            st_name
                        order by
                            st_name",
                    ChartType = "pie"
                });

            modelBuilder.Entity<Report>()
                .HasData(new Report
                {
                    Id = 2,
                    Name = "Bugs by Priority",
                    Sql = @"
                        select
                            pr_name [priority],
                            count(1) [count]
                        from
                            bugs

                            inner join priorities on bg_priority = pr_id
                        group by
                            pr_name
                        order by
                            pr_name",
                    ChartType = "pie"
                });

            modelBuilder.Entity<Report>()
                .HasData(new Report
                {
                    Id = 3,
                    Name = "Bugs by Category",
                    Sql = @"
                        select
                            ct_name [category],
                            count(1) [count]
                        from
                            bugs

                            inner join categories on bg_category = ct_id
                        group
                            by ct_name
                        order by
                            ct_name",
                    ChartType = "pie"
                });

            modelBuilder.Entity<Report>()
                .HasData(new Report
                {
                    Id = 4,
                    Name = "Bugs by Month",
                    Sql = @"
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
                            month(bg_reported_date)",
                    ChartType = "bar"
                });

            modelBuilder.Entity<Report>()
                .HasData(new Report
                {
                    Id = 5,
                    Name = "Bugs by Day of Year",
                    Sql = @"
                        select
                            datepart(dy, bg_reported_date) [day of year],
                            count(1) [count]
                        from
                            bugs
                        group by
                            datepart(dy, bg_reported_date),
                            datepart(dy,bg_reported_date)
                        order by 1",
                    ChartType = "line"
                });

            modelBuilder.Entity<Report>()
                .HasData(new Report
                {
                    Id = 6,
                    Name = "Bugs by User",
                    Sql = @"
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
                        order by 1",
                    ChartType = "table"
                });

            modelBuilder.Entity<Report>()
                .HasData(new Report
                {
                    Id = 7,
                    Name = "Hours by Org, Year, Month",
                    Sql = @"
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
                            datepart(month,tsk_created_date)",
                    ChartType = "table"
                });

            modelBuilder.Entity<Report>()
                .HasData(new Report
                {
                    Id = 8,
                    Name = "Hours Remaining by Project",
                    Sql = @"
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
                            by pj_name",
                    ChartType = "table"
                });

            #endregion Report

            #region DashboardItem

            #endregion DashboardItem
        }
    }
}