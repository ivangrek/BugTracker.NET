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
        public IQueryService QueryService { get; set; }

        protected string Sql { get; set; }

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
                var id = Convert.ToInt32(Util.SanitizeInteger(this.rowId.Value));

                QueryService.Delete(id);

                Response.Redirect("~/Queries/List.aspx");
            }
            else
            {
                Page.Title = $"{ApplicationSettings.AppTitle} - delete query";

                var id = Convert.ToInt32(Util.SanitizeInteger(Request["id"]));
                var (valid, name) = QueryService.CheckDeleting(id);

                if (valid || IsAuthorized)
                {
                    this.confirmHref.InnerText = $"confirm delete of query: \"{name}\"";
                    this.rowId.Value = Convert.ToString(id);
                }
                else
                {
                    Response.Write("You are not allowed to delete this item");
                    Response.End();
                }
            }
        }

        private bool IsAuthorized => Security.User.IsAdmin
            || Security.User.CanEditSql;
    }
}