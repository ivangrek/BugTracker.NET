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

    public partial class print_bugs : Page
    {
        public DataSet ds;
        public DataView dv;

        public Security security;
        public string sql;

        public void Page_Load(object sender, EventArgs e)
        {
            if (Request["format"] != "excel") Util.do_not_cache(Response);

            this.security = new Security();

            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            // fetch the sql
            var qu_id_string = Util.sanitize_integer(Request["qu_id"]);

            this.ds = null;
            this.dv = null;

            if (qu_id_string != null)
            {
                // use sql specified in query string
                var qu_id = Convert.ToInt32(qu_id_string);
                this.sql = @"select qu_sql from queries where qu_id = $1";
                this.sql = this.sql.Replace("$1", qu_id_string);
                var bug_sql = (string) DbUtil.execute_scalar(this.sql);

                // replace magic variables
                bug_sql = bug_sql.Replace("$ME", Convert.ToString(this.security.user.usid));

                bug_sql = Util.alter_sql_per_project_permissions(bug_sql, this.security);

                this.ds = DbUtil.get_dataset(bug_sql);
                this.dv = new DataView(this.ds.Tables[0]);
            }
            else
            {
                this.dv = (DataView) Session["bugs"];
            }

            if (this.dv == null)
            {
                Response.Write("Please recreate the list before trying to print...");
                Response.End();
            }

            var format = Request["format"];
            if (format != null && format == "excel")
                Util.print_as_excel(Response, this.dv);
            else
                print_as_html();
        }

        public void print_as_html()
        {
            Response.Write("<html><head><link rel='StyleSheet' href='btnet.css' type='text/css'></head><body>");

            Response.Write("<table class=bugt border=1>");
            int col;

            for (col = 1; col < this.dv.Table.Columns.Count; col++)
            {
                Response.Write("<td class=bugh>\n");
                if (this.dv.Table.Columns[col].ColumnName == "$FLAG")
                    Response.Write("flag");
                else if (this.dv.Table.Columns[col].ColumnName == "$SEEN")
                    Response.Write("new");
                else
                    Response.Write(this.dv.Table.Columns[col].ColumnName);
                Response.Write("</td>");
            }

            foreach (DataRowView drv in this.dv)
            {
                Response.Write("<tr>");
                for (col = 1; col < this.dv.Table.Columns.Count; col++)
                {
                    if (this.dv.Table.Columns[col].ColumnName == "$FLAG")
                    {
                        var flag = (int) drv[col];
                        var cls = "wht";
                        if (flag == 1) cls = "red";
                        else if (flag == 2) cls = "grn";

                        Response.Write("<td class=datad><span class=" + cls + ">&nbsp;</span>");
                    }
                    else if (this.dv.Table.Columns[col].ColumnName == "$SEEN")
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
                        var datatype = this.dv.Table.Columns[col].DataType;

                        if (Util.is_numeric_datatype(datatype))
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