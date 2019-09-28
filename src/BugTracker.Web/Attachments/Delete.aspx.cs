/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Attachments
{
    using System;
    using System.IO;
    using System.Text;
    using System.Web.UI;
    using Core;

    public partial class Delete : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }

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
            MainMenu.SelectedItem = ApplicationSettings.PluralBugLabel;

            if (security.User.IsAdmin || security.User.CanEditAndDeletePosts)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            var attachmentIdString = Util.SanitizeInteger(Request["id"]);
            var bugIdString = Util.SanitizeInteger(Request["bug_id"]);

            var permissionLevel = Bug.GetBugPermissionLevel(Convert.ToInt32(bugIdString), security);
            if (permissionLevel != Security.PermissionAll)
            {
                Response.Write("You are not allowed to edit this item");
                Response.End();
            }

            if (IsPostBack)
            {
                // save the filename before deleting the row
                this.Sql = @"select bp_file from bug_posts where bp_id = $ba";
                this.Sql = this.Sql.Replace("$ba", attachmentIdString);
                var filename = (string) DbUtil.ExecuteScalar(this.Sql);

                // delete the row representing the attachment
                this.Sql = @"delete bug_post_attachments where bpa_post = $ba
            delete bug_posts where bp_id = $ba";
                this.Sql = this.Sql.Replace("$ba", attachmentIdString);
                DbUtil.ExecuteNonQuery(this.Sql);

                // delete the file too
                var uploadFolder = Util.GetUploadFolder();
                if (uploadFolder != null)
                {
                    var path = new StringBuilder(uploadFolder);
                    path.Append("\\");
                    path.Append(bugIdString);
                    path.Append("_");
                    path.Append(attachmentIdString);
                    path.Append("_");
                    path.Append(filename);
                    if (File.Exists(path.ToString())) File.Delete(path.ToString());
                }

                Response.Redirect($"~/Bugs/Edit.aspx?id={bugIdString}");
            }
            else
            {
                Page.Title = $"{ApplicationSettings.AppTitle} - delete attachment";

                this.back_href.HRef = $"~/Bugs/Edit.aspx?id={bugIdString}" ;

                this.Sql = @"select bp_file from bug_posts where bp_id = $1";
                this.Sql = this.Sql.Replace("$1", attachmentIdString);

                var dr = DbUtil.GetDataRow(this.Sql);

                var s = Convert.ToString(dr["bp_file"]);

                this.confirm_href.InnerText = "confirm delete of attachment: " + s;

                this.row_id.Value = attachmentIdString;
            }
        }
    }
}