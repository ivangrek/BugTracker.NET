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

    public partial class get_db_datetime : Page
    {
        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            var dt = (DateTime) DbUtil.execute_scalar("select getdate()");

            Response.Write(dt.ToString("yyyyMMdd HH\\:mm\\:ss\\:fff"));
        }
    }
}