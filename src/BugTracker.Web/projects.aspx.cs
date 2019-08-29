/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class projects : Page
    {
        public DataSet ds;
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.MUST_BE_ADMIN);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "projects";

            this.ds = DbUtil.get_dataset(
                @"select
		pj_id [id],
		'<a href=edit_project.aspx?&id=' + convert(varchar,pj_id) + '>edit</a>' [$no_sort_edit],
		'<a href=edit_user_permissions2.aspx?projects=y&id=' + convert(varchar,pj_id) + '>permissions</a>' [$no_sort_per user<br>permissions],
		'<a href=delete_project.aspx?id=' + convert(varchar,pj_id) + '>delete</a>' [$no_sort_delete],
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