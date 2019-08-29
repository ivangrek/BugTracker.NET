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

    public partial class edit_report : Page
    {
        public int id;

        public Security security;
        public string sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();

            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK_EXCEPT_GUEST);

            if (this.security.user.is_admin || this.security.user.can_edit_reports)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "edit report";

            this.msg.InnerText = "";

            var var = Request.QueryString["id"];
            if (var == null)
                this.id = 0;
            else
                this.id = Convert.ToInt32(var);

            if (!IsPostBack)
            {
                // add or edit?
                if (this.id == 0)
                {
                    this.sub.Value = "Create";
                    this.sql_text.Value = Request.Form["sql_text"]; // if coming from search.aspx
                    this.table.Checked = true;
                }
                else
                {
                    this.sub.Value = "Update";

                    // Get this entry's data from the db and fill in the form
                    this.sql = @"select
				rp_desc, rp_sql, rp_chart_type
				from reports where rp_id = $1";
                    this.sql = this.sql.Replace("$1", Convert.ToString(this.id));
                    var dr = DbUtil.get_datarow(this.sql);

                    // Fill in this form
                    this.desc.Value = (string) dr["rp_desc"];

                    //			if (Util.get_setting("HtmlEncodeSql","0") == "1")
                    //			{
                    //				sql_text.Value = Server.HtmlEncode((string) dr["rp_sql"]);
                    //			}
                    //			else
                    //			{
                    this.sql_text.Value = (string) dr["rp_sql"];
                    //			}

                    switch ((string) dr["rp_chart_type"])
                    {
                        case "pie":
                            this.pie.Checked = true;
                            break;
                        case "bar":
                            this.bar.Checked = true;
                            break;
                        case "line":
                            this.line.Checked = true;
                            break;
                        default:
                            this.table.Checked = true;
                            break;
                    }
                }
            }
            else
            {
                on_update();
            }
        }

        public bool validate()
        {
            var good = true;

            if (this.desc.Value == "")
            {
                good = false;
                this.desc_err.InnerText = "Description is required.";
            }
            else
            {
                this.desc_err.InnerText = "";
            }

            if (this.sql_text.Value == "")
            {
                good = false;
                this.msg.InnerText = "The SQL statement is required.  ";
            }
            else
            {
                this.msg.InnerText = "";
            }

            return good;
        }

        public void on_update()
        {
            var good = validate();
            string ct;

            if (good)
            {
                if (this.id == 0)
                {
                    // insert new
                    this.sql = @"insert into reports
				(rp_desc, rp_sql, rp_chart_type)
				values (N'$de', N'$sq', N'$ct')";
                }
                else
                {
                    // edit existing
                    this.sql = @"update reports set
				rp_desc = N'$de',
				rp_sql = N'$sq',
				rp_chart_type = N'$ct'
				where rp_id = $id";
                    this.sql = this.sql.Replace("$id", Convert.ToString(this.id));
                }

                this.sql = this.sql.Replace("$de", this.desc.Value.Replace("'", "''"));
                this.sql = this.sql.Replace("$sq", Server.HtmlDecode(this.sql_text.Value.Replace("'", "''")));

                if (this.pie.Checked)
                    ct = "pie";
                else if (this.bar.Checked)
                    ct = "bar";
                else if (this.line.Checked)
                    ct = "line";
                else
                    ct = "table";

                this.sql = this.sql.Replace("$ct", ct);

                DbUtil.execute_nonquery(this.sql);
                Server.Transfer("reports.aspx");
            }
            else
            {
                if (this.id == 0)
                    // insert new
                    this.msg.InnerText += "Query was not created.";
                else
                    // edit existing
                    this.msg.InnerText += "Query was not updated.";
            }
        }
    }
}