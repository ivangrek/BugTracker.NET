/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.UserDefinedAttributes
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
        public IUserDefinedAttributeService UserDefinedAttributeService { get; set; }

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

                UserDefinedAttributeService.Delete(id);

                Response.Redirect("~/Admin/UserDefinedAttributes/List.aspx");
            }
            else
            {
                Page.Title = $"{ApplicationSettings.AppTitle} - delete user defined attribute value";

                var id = Convert.ToInt32(Util.SanitizeInteger(Request["id"]));
                var (valid, name) = UserDefinedAttributeService.CheckDeleting(id);

                if (valid)
                {
                    this.confirmHref.InnerText = $"confirm delete of \"{name}\"";
                    this.rowId.Value = Convert.ToString(id);
                }
                else
                {
                    Response.Write($"You can't delete value \"{name}\" because some bugs still reference it.");
                    Response.End();
                }
            }
        }
    }
}