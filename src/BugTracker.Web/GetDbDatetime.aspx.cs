/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Web.UI;
    using Core;

    public partial class GetDbDatetime : Page
    {
        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var dt = (DateTime) DbUtil.ExecuteScalar("select getdate()");

            Response.Write(dt.ToString("yyyyMMdd HH\\:mm\\:ss\\:fff"));
        }
    }
}