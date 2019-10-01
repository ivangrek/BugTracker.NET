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
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        protected string Sql {get; set; }

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            MainMenu.SelectedItem = ApplicationSettings.PluralBugLabel;

            if (Security.User.IsAdmin || Security.User.CanEditAndDeletePosts)
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

                var permissionLevel = Bug.GetBugPermissionLevel(Convert.ToInt32(bugId), Security);
                if (permissionLevel != SecurityPermissionLevel.PermissionAll)
                {
                    Response.Write("You are not allowed to edit this item");
                    Response.End();
                }

                Page.Title = $"{ApplicationSettings.AppTitle} - delete comment";

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