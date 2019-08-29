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

    public partial class install : Page
    {
        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            var dbname = Request["dbname"];

            if (!string.IsNullOrEmpty(dbname))
            {
                dbname = dbname.Replace("'", "''");
                ;
                try
                {
                    // don't allow lots of dbs to be created by somebody malicious
                    if (Application["dbs"] == null) Application["dbs"] = 0;

                    var dbs = (int) Application["dbs"];

                    if (dbs > 10) Response.End();

                    Application["dbs"] = ++dbs;

                    DbUtil.get_sqlconnection();
                    var sql = @"use master
				create database [$db]";

                    sql = sql.Replace("$db", dbname);
                    DbUtil.execute_nonquery(sql);

                    Response.Write("<font color=red><b>Database Created.</b></font>");
                }
                catch (Exception ex)
                {
                    Response.Write("<font color=red><b>" + ex.Message + "</b></font>");
                }
            }
        }
    }
}