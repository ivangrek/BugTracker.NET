/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Categories
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
        public ICategoryService CategoryService { get; set; }

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
                var id = Convert.ToInt32(Util.SanitizeInteger(this.rowId.Value));

                CategoryService.Delete(id);

                Response.Redirect("~/Administration/Categories/List.aspx");
            }
            else
            {
                Page.Title = $"{ApplicationSettings.AppTitle} - delete category";

                var id = Convert.ToInt32(Util.SanitizeInteger(Request["id"]));
                var (valid, name) = CategoryService.CheckDeleting(id);

                if (valid)
                {
                    this.confirmHref.InnerText = $"confirm delete of \"{name}\"";
                    this.rowId.Value = Convert.ToString(id);
                }
                else
                {
                    Response.Write($"You can't delete category \"{name}\" because some bugs still reference it.");
                    Response.End();
                }
            }
        }
    }
}