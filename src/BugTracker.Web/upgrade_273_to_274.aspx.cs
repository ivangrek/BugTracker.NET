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

    public partial class upgrade_273_to_274 : Page
    {
        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            var random = new Random();

            var sql = "select us_username, us_id, us_password from users where len(us_password) < 32";

            var ds = DbUtil.get_dataset(sql);
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                Thread.Sleep(10); // give time for the random number to seed differently;
                var us_username = (string) dr["us_username"];
                var us_id = (int) dr["us_id"];
                var us_password = (string) dr["us_password"];
                {
                    Response.Write("encrypting " + us_username + "<br>");
                    Util.update_user_password(us_id, us_password);
                }
            }

            Response.Write("done encrypting unencrypted passwords");
        }
    }
}