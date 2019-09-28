/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Statuses
{
    using System;
    using System.Web.UI;
    using Core;
    using Core.Administration;

    public partial class Delete : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public IStatusService StatusService { get; set; }

        protected Security Security { get; set; }

        protected void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.MustBeAdmin);

            MainMenu.Security = security;
            MainMenu.SelectedItem = "admin";

            if (IsPostBack)
            {
                // do delete here
                var id = Convert.ToInt32(Util.SanitizeInteger(this.rowId.Value));

                StatusService.Delete(id);
                Response.Redirect("~/Administration/Statuses/List.aspx");
            }
            else
            {
                Page.Title = $"{ApplicationSettings.AppTitle} - delete status";

                var id = Convert.ToInt32(Util.SanitizeInteger(Request["id"]));
                var (valid, name) = StatusService.CheckDeleting(id);

                if (valid)
                {
                    Response.Write($"You can't delete status \"{name}\" because some bugs still reference it.");
                    Response.End();
                }
                else
                {
                    this.confirmHref.InnerText = $"confirm delete of \"{name}\"";
                    this.rowId.Value = Convert.ToString(id);
                }
            }
        }
    }
}