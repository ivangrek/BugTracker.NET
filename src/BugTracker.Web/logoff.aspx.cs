/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class logoff : Page
    {
        ///////////////////////////////////////////////////////////////////
        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            Util.set_context(HttpContext.Current);

            DbUtil.get_sqlconnection();

            // delete the session row

            var cookie = Request.Cookies["se_id"];

            if (cookie != null)
            {
                var se_id = cookie.Value.Replace("'", "''");

                var sql = @"delete from sessions
			where se_id = N'$se'
			or datediff(d, se_date, getdate()) > 2";
                sql = sql.Replace("$se", se_id);
                DbUtil.execute_nonquery(sql);

                Session[se_id] = 0;

                Session["SelectedBugQuery"] = null;
                Session["bugs"] = null;
                Session["bugs_unfiltered"] = null;
                Session["project"] = null;
            }

            Response.Redirect("default.aspx?msg=logged+off");
        }
    }
}