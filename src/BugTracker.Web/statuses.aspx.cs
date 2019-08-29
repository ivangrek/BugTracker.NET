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

    public partial class statuses : Page
    {
        public DataSet ds;

        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.MUST_BE_ADMIN);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "statuses";

            this.ds = DbUtil.get_dataset(
                @"select st_id [id],
		st_name [status],
		st_sort_seq [sort seq],
		st_style [css<br>class],
		case when st_default = 1 then 'Y' else 'N' end [default],
		st_id [hidden]
		from statuses order by st_sort_seq");
        }
    }
}