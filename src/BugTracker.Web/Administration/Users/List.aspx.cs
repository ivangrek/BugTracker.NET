/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Users
{
    using System;
    using System.Data;
    using System.Web.UI;
    using Core;

    public partial class List : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }

        public DataSet Ds;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.MustBeAdminOrProjectAdmin);

            MainMenu.Security = security;
            MainMenu.SelectedItem = "admin";

            Page.Title = $"{ApplicationSettings.AppTitle} - users";

            string sql;

            if (security.User.IsAdmin)
                sql = @"
            select distinct pu_user
            into #t
            from
            project_user_xref
            where pu_admin = 1;

            select u.us_id [id],
            '<a href=" + ResolveUrl("~/Administration/Users/Edit.aspx") + @"?id=' + convert(varchar,u.us_id) + '>edit</a>' [$no_sort_edit],
            '<a href=" + ResolveUrl("~/Administration/Users/Edit.aspx") + @"?copy=y&id=' + convert(varchar,u.us_id) + '>copy</a>' [$no_sort_add<br>like<br>this],
            '<a href=" + ResolveUrl("~/Administration/Users/Delete.aspx") + @"?id=' + convert(varchar,u.us_id) + '>delete</a>' [$no_sort_delete],

            u.us_username [username],
            isnull(u.us_firstname,'') + ' ' + isnull(u.us_lastname,'') [name],
            '<a sort=''' + og_name + ''' href=" + ResolveUrl("~/Administration/Organizations/Edit.aspx") + @"?id=' + convert(varchar,og_id) + '>' + og_name + '</a>' [org],
            isnull(u.us_email,'') [email],
            case when u.us_admin = 1 then 'Y' else 'N' end [admin],
            case when pu_user is null then 'N' else 'Y' end [project<br>admin],
            case when u.us_active = 1 then 'Y' else 'N' end [active],
            case when og_external_user = 1 then 'Y' else 'N' end [external],
            isnull(pj_name,'') [forced<br>project],
            isnull(qu_desc,'') [default query],
            case when u.us_enable_notifications = 1 then 'Y' else 'N' end [notif-<br>ications],
            u.us_most_recent_login_datetime [most recent login],
            u2.us_username [created<br>by]

            from users u
            inner join orgs on u.us_org = og_id
            left outer join queries on u.us_default_query = qu_id
            left outer join projects on u.us_forced_project = pj_id
            left outer join users u2 on u.us_created_user = u2.us_id
            left outer join #t on u.us_id = pu_user
            where u.us_active in (1 $inactive)
            $filter_users
            order by u.us_username;

            drop table #t";
            else
                sql = @"
            select distinct pu_user
            into #t
            from
            project_user_xref
            where pu_admin = 1;

            select u.us_id [id],
            '<a href=" + ResolveUrl("~/Administration/Users/Edit.aspx") + @"?id=' + convert(varchar,u.us_id) + '>edit</a>' [$no_sort_edit],
            '<a href=" + ResolveUrl("~/Administration/Users/Edit.aspx") + @"?copy=y&id=' + convert(varchar,u.us_id) + '>copy</a>' [$no_sort_add<br>like<br>this],
            '<a href=" + ResolveUrl("~/Administration/Users/Delete.aspx") + @"?id=' + convert(varchar,u.us_id) + '>delete</a>' [$no_sort_delete],

            u.us_username [username],
            isnull(u.us_firstname,'') + ' ' + isnull(u.us_lastname,'') [name],
            og_name [org],
            isnull(u.us_email,'') [email],			
            case when u.us_admin = 1 then 'Y' else 'N' end [admin],
            case when pu_user is null then 'N' else 'Y' end [project<br>admin],
            case when u.us_active = 1 then 'Y' else 'N' end [active],
            case when og_external_user = 1 then 'Y' else 'N' end [external],
            isnull(pj_name,'') [forced<br>project],
            isnull(qu_desc,'') [default query],
            case when u.us_enable_notifications = 1 then 'Y' else 'N' end [notif-<br>ications],
            u.us_most_recent_login_datetime [most recent login]
            from users u
            inner join orgs on us_org = og_id
            left outer join queries on us_default_query = qu_id
            left outer join projects on us_forced_project = pj_id
            left outer join #t on us_id = pu_user
            where us_created_user = $us
            and us_active in (1 $inactive)
            $filter_users
            order by us_username;

            drop table #t";

            if (!IsPostBack)
            {
                var cookie = Request.Cookies["hide_inactive_users"];
                if (cookie != null)
                    if (cookie.Value == "1")
                        this.hide_inactive_users.Checked = true;

                var cookie2 = Request.Cookies["filter_users"];
                if (cookie2 != null)
                    this.filter_users.Value = cookie2.Value;
                else
                    this.filter_users.Value = "";
            }

            if (this.hide_inactive_users.Checked)
                sql = sql.Replace("$inactive", "");
            else
                sql = sql.Replace("$inactive", ",0");

            if (this.filter_users.Value != "")
                sql = sql.Replace("$filter_users",
                    "and u.us_username like '" + this.filter_users.Value.Replace("'", "''") + "%'");
            else
                sql = sql.Replace("$filter_users", "");

            sql = sql.Replace("$us", Convert.ToString(security.User.Usid));
            this.Ds = DbUtil.GetDataSet(sql);

            // cookies
            if (this.hide_inactive_users.Checked)
                Response.Cookies["hide_inactive_users"].Value = "1";
            else
                Response.Cookies["hide_inactive_users"].Value = "0";

            Response.Cookies["filter_users"].Value = this.filter_users.Value;

            var dt = DateTime.Now;
            var ts = new TimeSpan(365, 0, 0, 0);
            Response.Cookies["hide_inactive_users"].Expires = dt.Add(ts);
            Response.Cookies["filter_users"].Expires = dt.Add(ts);
        }
    }
}