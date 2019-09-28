/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Reports
{
    using System;
    using System.Web.UI;
    using Core;

    public partial class Edit : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }

        public int Id;
        public string Sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.AnyUserOkExceptGuest);

            MainMenu.Security = security;
            MainMenu.SelectedItem = "reports";

            if (security.User.IsAdmin || security.User.CanEditReports)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            Page.Title = $"{ApplicationSettings.AppTitle} - edit report";

            this.msg.InnerText = "";

            var var = Request.QueryString["id"];
            if (var == null)
                this.Id = 0;
            else
                this.Id = Convert.ToInt32(var);

            if (!IsPostBack)
            {
                // add or edit?
                if (this.Id == 0)
                {
                    this.sub.Value = "Create";
                    this.sql_text.Value = Request.Form["sql_text"]; // if coming from Search.aspx
                    this.table.Checked = true;
                }
                else
                {
                    this.sub.Value = "Update";

                    // Get this entry's data from the db and fill in the form
                    this.Sql = @"select
                rp_desc, rp_sql, rp_chart_type
                from reports where rp_id = $1";
                    this.Sql = this.Sql.Replace("$1", Convert.ToString(this.Id));
                    var dr = DbUtil.GetDataRow(this.Sql);

                    // Fill in this form
                    this.desc.Value = (string) dr["rp_desc"];

                    //			if (Util.GetSetting("HtmlEncodeSql","0") == "1")
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
                if (this.Id == 0)
                {
                    // insert new
                    this.Sql = @"insert into reports
                (rp_desc, rp_sql, rp_chart_type)
                values (N'$de', N'$sq', N'$ct')";
                }
                else
                {
                    // edit existing
                    this.Sql = @"update reports set
                rp_desc = N'$de',
                rp_sql = N'$sq',
                rp_chart_type = N'$ct'
                where rp_id = $id";
                    this.Sql = this.Sql.Replace("$id", Convert.ToString(this.Id));
                }

                this.Sql = this.Sql.Replace("$de", this.desc.Value.Replace("'", "''"));
                this.Sql = this.Sql.Replace("$sq", Server.HtmlDecode(this.sql_text.Value.Replace("'", "''")));

                if (this.pie.Checked)
                    ct = "pie";
                else if (this.bar.Checked)
                    ct = "bar";
                else if (this.line.Checked)
                    ct = "line";
                else
                    ct = "table";

                this.Sql = this.Sql.Replace("$ct", ct);

                DbUtil.ExecuteNonQuery(this.Sql);
                Response.Redirect("~/Reports/List.aspx");
            }
            else
            {
                if (this.Id == 0)
                    // insert new
                    this.msg.InnerText += "Query was not created.";
                else
                    // edit existing
                    this.msg.InnerText += "Query was not updated.";
            }
        }
    }
}