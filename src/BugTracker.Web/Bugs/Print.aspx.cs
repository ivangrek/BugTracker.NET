/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Bugs
{
    using System;
    using System.Data;
    using System.Web.UI;
    using Core;

    public partial class Print : Page
    {
        public ISecurity Security { get; set; }

        protected DataSet Ds { get; set; }
        public DataView Dv;
        protected string Sql {get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            if (Request["format"] != "excel") Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            // fetch the sql
            var quIdString = Util.SanitizeInteger(Request["qu_id"]);

            this.Ds = null;
            this.Dv = null;

            if (quIdString != null)
            {
                // use sql specified in query string
                var quId = Convert.ToInt32(quIdString);
                this.Sql = @"select qu_sql from queries where qu_id = $1";
                this.Sql = this.Sql.Replace("$1", quIdString);
                var bugSql = (string) DbUtil.ExecuteScalar(this.Sql);

                // replace magic variables
                bugSql = bugSql.Replace("$ME", Convert.ToString(Security.User.Usid));

                bugSql = Util.AlterSqlPerProjectPermissions(bugSql, Security);

                this.Ds = DbUtil.GetDataSet(bugSql);
                this.Dv = new DataView(this.Ds.Tables[0]);
            }
            else
            {
                this.Dv = (DataView) Session["bugs"];
            }

            if (this.Dv == null)
            {
                Response.Write("Please recreate the list before trying to print...");
                Response.End();
            }

            var format = Request["format"];
            if (format != null && format == "excel")
                Util.PrintAsExcel(Response, this.Dv);
            else
                print_as_html();
        }

        public void print_as_html()
        {
            Response.Write("<html><head><link rel='StyleSheet' href='Content/btnet.css' type='text/css'></head><body>");

            Response.Write("<table class=bugt border=1>");
            int col;

            for (col = 1; col < this.Dv.Table.Columns.Count; col++)
            {
                Response.Write("<td class=bugh>\n");
                if (this.Dv.Table.Columns[col].ColumnName == "$FLAG")
                    Response.Write("flag");
                else if (this.Dv.Table.Columns[col].ColumnName == "$SEEN")
                    Response.Write("new");
                else
                    Response.Write(this.Dv.Table.Columns[col].ColumnName);
                Response.Write("</td>");
            }

            foreach (DataRowView drv in this.Dv)
            {
                Response.Write("<tr>");
                for (col = 1; col < this.Dv.Table.Columns.Count; col++)
                {
                    if (this.Dv.Table.Columns[col].ColumnName == "$FLAG")
                    {
                        var flag = (int) drv[col];
                        var cls = "wht";
                        if (flag == 1) cls = "red";
                        else if (flag == 2) cls = "grn";

                        Response.Write("<td class=datad><span class=" + cls + ">&nbsp;</span>");
                    }
                    else if (this.Dv.Table.Columns[col].ColumnName == "$SEEN")
                    {
                        var seen = (int) drv[col];
                        var cls = "old";
                        if (seen == 0)
                            cls = "new";
                        else
                            cls = "old";
                        Response.Write("<td class=datad><span class=" + cls + ">&nbsp;</span>");
                    }
                    else
                    {
                        var datatype = this.Dv.Table.Columns[col].DataType;

                        if (Util.IsNumericDataType(datatype))
                            Response.Write("<td class=bugd align=right>");
                        else
                            Response.Write("<td class=bugd>");

                        // write the data
                        if (drv[col].ToString() == "")
                            Response.Write("&nbsp;");
                        else
                            Response.Write(Server.HtmlEncode(drv[col].ToString()).Replace("\n", "<br>"));
                    }

                    Response.Write("</td>");
                }

                Response.Write("</tr>");
            }

            Response.Write("</table></body></html>");
        }
    }
}