/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Queries
{
    using System;
    using System.Data;
    using System.Web.UI;
    using Core;

    public partial class List : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }

        public DataSet Ds;

        public Security Security { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.AnyUserOkExceptGuest);

            MainMenu.Security = security;
            MainMenu.SelectedItem = "queries";

            Security = security;

            Page.Title = $"{ApplicationSettings.AppTitle} - queries";

            var sql = "";

            if (security.User.IsAdmin || security.User.CanEditSql)
            {
                // allow admin to edit all queries

                sql = @"select
            qu_desc [query],
            case
                when isnull(qu_user,0) = 0 and isnull(qu_org,0) is null then 'everybody'
                when isnull(qu_user,0) <> 0 then 'user:' + us_username
                when isnull(qu_org,0) <> 0 then 'org:' + og_name
                else ' '
                end [visibility],
            '<a href=" + ResolveUrl("~/Bugs/List.aspx") + @"?qu_id=' + convert(varchar,qu_id) + '>view list</a>' [view list],
            '<a target=_blank href=" + ResolveUrl("~/Bugs/Print.aspx") + @"?qu_id=' + convert(varchar,qu_id) + '>print list</a>' [print list],
            '<a target=_blank href=" + ResolveUrl("~/Bugs/Print.aspx") + @"?format=excel&qu_id=' + convert(varchar,qu_id) + '>export as excel</a>' [export as excel],
            '<a target=_blank href=" + ResolveUrl("~/Bugs/Print2.aspx") + @"?qu_id=' + convert(varchar,qu_id) + '>print detail</a>' [print list<br>with detail],
            '<a href=" + ResolveUrl("~/Queries/Edit.aspx") + @"?id=' + convert(varchar,qu_id) + '>edit</a>' [edit],
            '<a href=" + ResolveUrl("~/Queries/Delete.aspx") + @"?id=' + convert(varchar,qu_id) + '>delete</a>' [delete],
            replace(convert(nvarchar(4000),qu_sql), char(10),'<br>') [sql]
            from queries
            left outer join users on qu_user = us_id
            left outer join orgs on qu_org = og_id
            where 1 = $all /* all */
            or isnull(qu_user,0) = $us
            or isnull(qu_user,0) = 0
            order by qu_desc";

                sql = sql.Replace("$all", this.show_all.Checked ? "1" : "0");
            }
            else
            {
                // allow editing for users' own queries

                sql = @"select
            qu_desc [query],
            '<a href=" + ResolveUrl("~/Bugs/List.aspx") + @"?qu_id=' + convert(varchar,qu_id) + '>view list</a>' [view list],
            '<a target=_blank href=" + ResolveUrl("~/Bugs/Print.aspx") + @"?qu_id=' + convert(varchar,qu_id) + '>print list</a>' [print list],
            '<a target=_blank href=" + ResolveUrl("~/Bugs/Print.aspx") + @"?format=excel&qu_id=' + convert(varchar,qu_id) + '>export as excel</a>' [export as excel],
            '<a target=_blank href=" + ResolveUrl("~/Bugs/Print2.aspx") + @"?qu_id=' + convert(varchar,qu_id) + '>print detail</a>' [print list<br>with detail],
            '<a href=" + ResolveUrl("~/Queries/Edit.aspx") + @"?id=' + convert(varchar,qu_id) + '>rename</a>' [rename],
            '<a href=" + ResolveUrl("~/Queries/Delete.aspx") + @"?id=' + convert(varchar,qu_id) + '>delete</a>' [delete]
            from queries
            inner join users on qu_user = us_id
            where isnull(qu_user,0) = $us
            order by qu_desc";
            }

            sql = sql.Replace("$us", Convert.ToString(security.User.Usid));
            this.Ds = DbUtil.GetDataSet(sql);
        }
    }
}