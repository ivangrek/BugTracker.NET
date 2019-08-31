/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Categories
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

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - categories";

            this.Ds = DbUtil.GetDataSet(
                @"select
        ct_id [id],
        ct_name [category],
        ct_sort_seq [sort seq],
        case when ct_default = 1 then 'Y' else 'N' end [default],
        ct_id [hidden]
        from categories order by ct_name");
        }
    }
}