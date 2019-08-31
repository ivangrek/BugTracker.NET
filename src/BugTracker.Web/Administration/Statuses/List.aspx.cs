/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Statuses
{
    using System;
    using System.Data;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class List : Page
    {
        public DataSet Ds;
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - statuses";

            this.Ds = DbUtil.GetDataSet(
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