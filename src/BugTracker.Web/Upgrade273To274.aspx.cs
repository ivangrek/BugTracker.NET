/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.Threading;
    using System.Web.UI;
    using Core;

    public partial class Upgrade273To274 : Page
    {
        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var random = new Random();

            var sql = "select us_username, us_id, us_password from users where len(us_password) < 32";

            var ds = DbUtil.GetDataSet(sql);
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                Thread.Sleep(10); // give time for the random number to seed differently;
                var usUsername = (string) dr["us_username"];
                var usId = (int) dr["us_id"];
                var usPassword = (string) dr["us_password"];
                {
                    Response.Write("encrypting " + usUsername + "<br>");
                    Util.UpdateUserPassword(usId, usPassword);
                }
            }

            Response.Write("done encrypting unencrypted passwords");
        }
    }
}