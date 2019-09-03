/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Bugs
{
    using System;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class Delete : Page
    {
        public Security Security;
        public string Sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            this.Security = new Security();
            this.Security.CheckSecurity(HttpContext.Current, Security.AnyUserOkExceptGuest);

            if (this.Security.User.IsAdmin || this.Security.User.CanDeleteBug)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            var id = Util.SanitizeInteger(Request["id"]);

            var permissionLevel = Bug.GetBugPermissionLevel(Convert.ToInt32(id), this.Security);
            if (permissionLevel != Security.PermissionAll)
            {
                Response.Write("You are not allowed to edit this item");
                Response.End();
            }

            if (IsPostBack)
            {
                Bug.DeleteBug(Convert.ToInt32(this.row_id.Value));
                Server.Transfer("~/Bugs/List.aspx");
            }
            else
            {
                Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - "
                                                                            + "delete " +
                                                                            Util.GetSetting("SingularBugLabel",
                                                                                "bug");

                this.back_href.HRef = ResolveUrl($"~/Bugs/Edit.aspx?id={id}");

                this.Sql = @"select bg_short_desc from bugs where bg_id = $1";
                this.Sql = this.Sql.Replace("$1", id);

                var dr = DbUtil.GetDataRow(this.Sql);

                this.confirm_href.InnerText = "confirm delete of "
                                              + Util.GetSetting("SingularBugLabel", "bug")
                                              + ": "
                                              + Convert.ToString(dr["bg_short_desc"]);

                this.row_id.Value = id;
            }
        }
    }
}