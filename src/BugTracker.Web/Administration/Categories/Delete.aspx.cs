/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Categories
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;
    using Core.Administration;
    using Core.Persistence;

    public partial class Delete : Page
    {
        private readonly ICategoryService categoryService = new CategoryService(new ApplicationContext());

        protected Security Security;

        protected void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security = new Security();
            Security.CheckSecurity(HttpContext.Current, Security.MustBeAdmin);

            if (IsPostBack)
            {
                var id = Convert.ToInt32(Util.SanitizeInteger(this.rowId.Value));

                this.categoryService.Delete(id);

                Server.Transfer("~/Administration/Categories/List.aspx");
            }
            else
            {
                Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - delete category";

                var id = Convert.ToInt32(Util.SanitizeInteger(Request["id"]));
                var (valid, name) = this.categoryService.CheckDeleting(id);

                if (valid)
                {
                    Response.Write($"You can't delete category \"{name}\" because some bugs still reference it.");
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