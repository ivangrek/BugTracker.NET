/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Reports
{
    using System;
    using System.Collections.Generic;
    using System.Web.UI;
    using BugTracker.Web.Core.Controls;
    using Core;

    public partial class Edit : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }
        public IReportService ReportService { get; set; }

        public int Id;
        protected string Sql {get; set; }

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            if (!IsAuthorized)
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            MainMenu.SelectedItem = MainMenuSections.Reports;

            int.TryParse(Request.QueryString["id"], out var id);

            this.msg.InnerText = string.Empty;

            if (IsPostBack)
            {
                OnUpdate(id);
            }
            else
            {
                Page.Title = $"{ApplicationSettings.AppTitle} - edit report";

                // add or edit?
                if (id == 0)
                {
                    this.sub.Value = "Create";
                    this.sqlText.Value = Request.Form["sql_text"]; // if coming from Search.aspx
                    this.table.Checked = true;
                }
                else
                {
                    this.sub.Value = "Update";

                    // Get this entry's data from the db and fill in the form
                    var dataRow = ReportService.LoadOne(id);

                    // Fill in this form
                    this.desc.Value = dataRow.Name;

                    //if (Util.GetSetting("HtmlEncodeSql", "0") == "1")
                    //{
                    //    sql_text.Value = Server.HtmlEncode((string)dr["rp_sql"]);
                    //}
                    //else
                    //{
                    this.sqlText.Value = dataRow.Sql;
                    //}

                    switch (dataRow.ChartType)
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
        }

        private bool IsAuthorized => Security.User.IsAdmin
            || Security.User.CanEditReports;

        private void OnUpdate(int id)
        {
            var good = ValidateForm();

            if (good)
            {
                string ct;

                if (this.pie.Checked)
                {
                    ct = "pie";
                }
                else if (this.bar.Checked)
                {
                    ct = "bar";
                }
                else if (this.line.Checked)
                {
                    ct = "line";
                }
                else
                {
                    ct = "table";
                }

                var parameters = new Dictionary<string, string>
                {
                    { "$id", Convert.ToString(id)},
                    { "$de", this.desc.Value.Replace("'", "''") },
                    { "$sq", Server.HtmlDecode(this.sqlText.Value.Replace("'", "''")) },
                    { "$ct", ct },
                };

                if (id == 0) // insert new
                {
                    ReportService.Create(parameters);
                }
                else // edit existing
                {
                    ReportService.Update(parameters);
                }

                Response.Redirect("~/Reports/List.aspx");
            }
            else
            {
                if (id == 0) // insert new
                {
                    this.msg.InnerText = "Query was not created.";
                }
                else // edit existing
                {
                    this.msg.InnerText = "Query was not updated.";
                }
            }
        }

        private bool ValidateForm()
        {
            var good = true;

            if (this.desc.Value == string.Empty)
            {
                good = false;
                this.desc_err.InnerText = "Description is required.";
            }
            else
            {
                this.desc_err.InnerText = string.Empty;
            }

            if (this.sqlText.Value == string.Empty)
            {
                good = false;
                this.msg.InnerText = "The SQL statement is required.  ";
            }
            else
            {
                this.msg.InnerText = string.Empty;
            }

            return good;
        }
    }
}