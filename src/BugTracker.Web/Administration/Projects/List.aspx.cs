/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Projects
{
    using System;
    using System.Data;
    using System.Web.UI;
    using BugTracker.Web.Core.Controls;
    using Core;

    public partial class List : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public DataSet Ds;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.MustBeAdmin);

            MainMenu.SelectedItem = MainMenuSections.Administration;

            Page.Title = $"{ApplicationSettings.AppTitle} - projects";

            this.Ds = DbUtil.GetDataSet(
                @"select
        pj_id [id],
        '<a href=" + ResolveUrl("~/Administration/Projects/Edit.aspx") + @"?&id=' + convert(varchar,pj_id) + '>edit</a>' [$no_sort_edit],
        '<a href=" + ResolveUrl("~/Administration/Projects/EditUserPermissions2.aspx") + @"?projects=y&id=' + convert(varchar,pj_id) + '>permissions</a>' [$no_sort_per user<br>permissions],
        '<a href=" + ResolveUrl("~/Administration/Projects/Delete.aspx") + @"?id=' + convert(varchar,pj_id) + '>delete</a>' [$no_sort_delete],
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
        }
    }
}