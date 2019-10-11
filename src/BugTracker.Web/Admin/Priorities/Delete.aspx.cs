/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Priorities
{
    using System;
    using System.Web.UI;
    using BugTracker.Web.Core.Controls;
    using Core;
    using Core.Administration;

    public partial class Delete : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }
        public IPriorityService PriorityService { get; set; }

        protected void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.MustBeAdmin);

            MainMenu.SelectedItem = MainMenuSections.Administration;

            if (IsPostBack)
            {
                // do delete here
                var id = Convert.ToInt32(Util.SanitizeInteger(this.rowId.Value));

                PriorityService.Delete(id);

                Response.Redirect("~/Admin/Priorities/List.aspx");
            }
            else
            {
                Page.Title = $"{ApplicationSettings.AppTitle} - delete priority";

                var id = Convert.ToInt32(Util.SanitizeInteger(Request["id"]));
                var (valid, name) = PriorityService.CheckDeleting(id);

                if (valid)
                {
                    this.confirmHref.InnerText = $"confirm delete of \"{name}\"";
                    this.rowId.Value = Convert.ToString(id);
                }
                else
                {
                    Response.Write($"You can't delete priority \"{name}\" because some bugs still reference it.");
                    Response.End();
                }
            }
        }
    }
}