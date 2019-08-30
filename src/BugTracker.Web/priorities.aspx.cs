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

    public partial class Priorities : Page
    {
        public DataSet Ds;
        public Security Security;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);

            Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "priorities";

            this.Ds = DbUtil.GetDataSet(
                @"select pr_id [id],
		pr_name [description],
		pr_sort_seq [sort seq],
		'<div style=''background:' + pr_background_color + ';''>' + pr_background_color + '</div>' [background<br>color],
		pr_style [css<br>class],
		case when pr_default = 1 then 'Y' else 'N' end [default],
		pr_id [hidden] from priorities");
        }
    }
}