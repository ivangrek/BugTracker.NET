/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Projects
{
    using System;
    using System.Web.UI;
    using BugTracker.Web.Core.Controls;
    using Core;

    public partial class Delete : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        protected string Sql {get; set; }

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.MustBeAdmin);

            MainMenu.SelectedItem = MainMenuSections.Administration;

            if (IsPostBack)
            {
                // do delete here
                this.Sql = @"delete projects where pj_id = $1";
                this.Sql = this.Sql.Replace("$1", Util.SanitizeInteger(this.row_id.Value));
                DbUtil.ExecuteNonQuery(this.Sql);
                Response.Redirect("~/Administration/Projects/List.aspx");
            }
            else
            {
                Page.Title = $"{ApplicationSettings.AppTitle} - delete project";

                var id = Util.SanitizeInteger(Request["id"]);

                this.Sql = @"declare @cnt int
            select @cnt = count(1) from bugs where bg_project = $1
            select pj_name, @cnt [cnt] from projects where pj_id = $1";
                this.Sql = this.Sql.Replace("$1", id);

                var dr = DbUtil.GetDataRow(this.Sql);

                if ((int)dr["cnt"] > 0)
                {
                    Response.Write("You can't delete project \""
                                   + Convert.ToString(dr["pj_name"])
                                   + "\" because some bugs still reference it.");
                    Response.End();
                }
                else
                {
                    this.confirm_href.InnerText = "confirm delete of \""
                                                  + Convert.ToString(dr["pj_name"])
                                                  + "\"";

                    this.row_id.Value = id;
                }
            }
        }
    }
}