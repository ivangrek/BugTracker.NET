/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.UserDefinedAttributes
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;
    using Core.Administration;
    using Core.Persistence;

    public partial class Delete : Page
    {
        private readonly IUserDefinedAttributeService userDefinedAttributeService = new UserDefinedAttributeService(new ApplicationContext());

        protected Security Security { get; set; }

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

                this.userDefinedAttributeService.Delete(id);

                Server.Transfer("~/Administration/UserDefinedAttributes/List.aspx");
            }
            else
            {
                Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - delete user defined attribute value";

                var id = Convert.ToInt32(Util.SanitizeInteger(Request["id"]));
                var (valid, name) = this.userDefinedAttributeService.CheckDeleting(id);

                if (valid)
                {
                    Response.Write($"You can't delete value \"{name}\" because some bugs still reference it.");
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