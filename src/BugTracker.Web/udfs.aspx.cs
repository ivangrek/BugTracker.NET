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

    public partial class Udfs : Page
    {
        public DataSet Ds;
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "user defined attribute values";

            this.Ds = DbUtil.GetDataSet(
                @"select udf_id [id],
		udf_name [user defined attribute value],
		udf_sort_seq [sort seq],
		case when udf_default = 1 then 'Y' else 'N' end [default],
		udf_id [hidden]
		from user_defined_attribute order by udf_name");
        }
    }
}