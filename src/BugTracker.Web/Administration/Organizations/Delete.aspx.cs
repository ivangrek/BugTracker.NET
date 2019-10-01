/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Organizations
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
                this.Sql = @"delete orgs where og_id = $1";
                this.Sql = this.Sql.Replace("$1", Util.SanitizeInteger(this.row_id.Value));
                DbUtil.ExecuteNonQuery(this.Sql);
                Response.Redirect("~/Administration/Organizations/List.aspx");
            }
            else
            {
                Page.Title = $"{ApplicationSettings.AppTitle} - delete organization";

                var id = Util.SanitizeInteger(Request["id"]);

                this.Sql = @"declare @cnt int
            select @cnt = count(1) from users where us_org = $1;
            select @cnt = @cnt + count(1) from queries where qu_org = $1;
            select @cnt = @cnt + count(1) from bugs where bg_org = $1;
            select og_name, @cnt [cnt] from orgs where og_id = $1";
                this.Sql = this.Sql.Replace("$1", id);

                var dr = DbUtil.GetDataRow(this.Sql);

                if ((int) dr["cnt"] > 0)
                {
                    Response.Write("You can't delete organization \""
                                   + Convert.ToString(dr["og_name"])
                                   + "\" because some bugs, users, queries still reference it.");
                    Response.End();
                }
                else
                {
                    this.confirm_href.InnerText = "confirm delete of \""
                                                  + Convert.ToString(dr["og_name"])
                                                  + "\"";

                    this.row_id.Value = id;
                }
            }
        }
    }
}