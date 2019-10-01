/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.Web.UI;
    using Core;

    public partial class Ajax2 : Page
    {
        public ISecurity Security { get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            // will this be too slow?

            // we could index on bg_short_desc and then do '$str%' rather than '%$str%'

            try
            {
                var sql = @"select distinct top 10 bg_short_desc from bugs
            where bg_short_desc like '%$str%'
            order by 1";

                // if you don't use permissions, comment out this line for speed?
                sql = Util.AlterSqlPerProjectPermissions(sql, Security);

                var text = Request["q"];
                sql = sql.Replace("$str", text.Replace("'", "''"));

                var ds = DbUtil.GetDataSet(sql);

                if (ds.Tables[0].Rows.Count > 0)
                {
                    Response.Write("<select id='suggest_select' class='suggest_select'	size=6 ");
                    Response.Write(
                        " onclick='select_suggestion(this)' onkeydown='return suggest_sel_onkeydown(this, event)'>");
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        Response.Write("<option>");
                        Response.Write(dr[0]);
                        Response.Write("</option>");
                    }

                    Response.Write("</select>");
                }
                else
                {
                    Response.Write("");
                }
            }
            catch (Exception)
            {
                Response.Write("");
            }
        }
    }
}