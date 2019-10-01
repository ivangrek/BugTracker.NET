/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Organizations
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

            Page.Title = $"{ApplicationSettings.AppTitle} - organizations";

            this.Ds = DbUtil.GetDataSet(
                @"select og_id [id],
        '<a href=" + ResolveUrl("~/Administration/Organizations/Edit.aspx") + @"?id=' + convert(varchar,og_id) + '>edit</a>' [$no_sort_edit],
        '<a href=" + ResolveUrl("~/Administration/Organizations/Delete.aspx") + @"?id=' + convert(varchar,og_id) + '>delete</a>' [$no_sort_delete],
        og_name[desc],
        case when og_active = 1 then 'Y' else 'N' end [active],
        case when og_can_search = 1 then 'Y' else 'N' end [can<br>search],
        case when og_non_admins_can_use = 1 then 'Y' else 'N' end [non-admin<br>can use],
        case when og_can_only_see_own_reported = 1 then 'Y' else 'N' end [can see<br>only own bugs],
        case
            when og_other_orgs_permission_level = 0 then 'None'
            when og_other_orgs_permission_level = 1 then 'Read Only'
            else 'Add/Edit' end [other orgs<br>permission<br>level],
        case when og_external_user = 1 then 'Y' else 'N' end [external],
        case when og_can_be_assigned_to = 1 then 'Y' else 'N' end [can<br>be assigned to],
        case
            when og_status_field_permission_level = 0 then 'None'
            when og_status_field_permission_level = 1 then 'Read Only'
            else 'Add/Edit' end [status<br>permission<br>level],
        case
            when og_assigned_to_field_permission_level = 0 then 'None'
            when og_assigned_to_field_permission_level = 1 then 'Read Only'
            else 'Add/Edit' end [assigned to<br>permission<br>level],
        case
            when og_priority_field_permission_level = 0 then 'None'
            when og_priority_field_permission_level = 1 then 'Read Only'
            else 'Add/Edit' end [priority<br>permission<br>level],
        isnull(og_domain,'')[domain]
        from orgs order by og_name");
        }
    }
}