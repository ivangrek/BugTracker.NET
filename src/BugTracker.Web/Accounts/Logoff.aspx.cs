/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Accounts
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class Logoff : Page
    {
        ///////////////////////////////////////////////////////////////////
        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Util.SetContext(HttpContext.Current);

            DbUtil.GetSqlConnection();

            // delete the session row

            var cookie = Request.Cookies["se_id"];

            if (cookie != null)
            {
                var seId = cookie.Value.Replace("'", "''");

                var sql = @"delete from sessions
            where se_id = N'$se'
            or datediff(d, se_date, getdate()) > 2";
                sql = sql.Replace("$se", seId);
                DbUtil.ExecuteNonQuery(sql);

                Session[seId] = 0;

                Session["SelectedBugQuery"] = null;
                Session["bugs"] = null;
                Session["bugs_unfiltered"] = null;
                Session["project"] = null;
            }

            Response.Redirect("~/Accounts/Login.aspx?msg=logged+off");
        }
    }
}