/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Comments
{
    using System;
    using System.Web.UI;
    using Core;

    public partial class Delete : Page
    {
        public string Sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            var security = new Security();

            security.CheckSecurity(Security.AnyUserOkExceptGuest);

            MainMenu.Security = security;
            MainMenu.SelectedItem = Util.GetSetting("PluralBugLabel", "bugs");

            if (security.User.IsAdmin || security.User.CanEditAndDeletePosts)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            if (IsPostBack)
            {
                // do delete here

                this.Sql = @"delete bug_posts where bp_id = $1";
                this.Sql = this.Sql.Replace("$1", Util.SanitizeInteger(this.row_id.Value));
                DbUtil.ExecuteNonQuery(this.Sql);
                Response.Redirect("~/Bugs/Edit.aspx?id=" + Util.SanitizeInteger(this.redirect_bugid.Value));
            }
            else
            {
                var bugId = Util.SanitizeInteger(Request["bug_id"]);
                this.redirect_bugid.Value = bugId;

                var permissionLevel = Bug.GetBugPermissionLevel(Convert.ToInt32(bugId), security);
                if (permissionLevel != Security.PermissionAll)
                {
                    Response.Write("You are not allowed to edit this item");
                    Response.End();
                }

                Page.Title = Util.GetSetting("AppTitle", "BugTracker.NET") + " - delete comment";

                var id = Util.SanitizeInteger(Request["id"]);

                this.back_href.HRef = ResolveUrl($"~/Bugs/Edit.aspx?id={bugId}");

                this.Sql = @"select bp_comment from bug_posts where bp_id = $1";
                this.Sql = this.Sql.Replace("$1", id);

                var dr = DbUtil.GetDataRow(this.Sql);

                // show the first few chars of the comment
                var s = Convert.ToString(dr["bp_comment"]);
                var len = 20;
                if (s.Length < len) len = s.Length;

                this.confirm_href.InnerText = "confirm delete of comment: "
                                              + s.Substring(0, len)
                                              + "...";

                this.row_id.Value = id;
            }
        }
    }
}