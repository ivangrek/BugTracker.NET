/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Queries
{
    using System;
    using System.Web.UI;
    using BugTracker.Web.Core.Controls;
    using Core;

    public partial class Delete : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public string Sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            MainMenu.SelectedItem = MainMenuSections.Queries;

            if (IsPostBack)
            {
                // do delete here
                this.Sql = @"delete queries where qu_id = $1";
                this.Sql = this.Sql.Replace("$1", Util.SanitizeInteger(this.row_id.Value));
                DbUtil.ExecuteNonQuery(this.Sql);
                Response.Redirect("~/Queries/List.aspx");
            }
            else
            {
                Page.Title = $"{ApplicationSettings.AppTitle} - delete query";

                var id = Util.SanitizeInteger(Request["id"]);

                this.Sql = @"select qu_desc, isnull(qu_user,0) qu_user from queries where qu_id = $1";
                this.Sql = this.Sql.Replace("$1", id);

                var dr = DbUtil.GetDataRow(this.Sql);

                if ((int) dr["qu_user"] != Security.User.Usid)
                {
                    if (Security.User.IsAdmin || Security.User.CanEditSql)
                    {
                        // can do anything
                    }
                    else
                    {
                        Response.Write("You are not allowed to delete this item");
                        Response.End();
                    }
                }

                this.confirm_href.InnerText = "confirm delete of query: "
                                              + Convert.ToString(dr["qu_desc"]);

                this.row_id.Value = id;
            }
        }
    }
}