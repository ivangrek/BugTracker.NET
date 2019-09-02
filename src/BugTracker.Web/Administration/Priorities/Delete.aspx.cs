/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Administration.Priorities
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;
    using Core.Administration;
    using Core.Persistence;

    public partial class Delete : Page
    {
        private readonly IPriorityService priorityService = new PriorityService(new ApplicationContext());

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
                // do delete here
                var id = Convert.ToInt32(Util.SanitizeInteger(this.rowId.Value));

                this.priorityService.Delete(id);

                Server.Transfer("~/Administration/Priorities/List.aspx");
            }
            else
            {
                Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - delete priority";

                var id = Convert.ToInt32(Util.SanitizeInteger(Request["id"]));
                var (valid, name) = this.priorityService.CheckDeleting(id);

                if (valid)
                {
                    Response.Write($"You can't delete priority \"{name}\" because some bugs still reference it.");
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