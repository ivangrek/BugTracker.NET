/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Attachments
{
    using System;
    using System.IO;
    using System.Web.UI;
    using Core;

    public partial class Add : Page
    {
        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public int Bugid;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOk);

            Page.Title = $"{ApplicationSettings.AppTitle} - add attachment";

            var stringId = Util.SanitizeInteger(Request.QueryString["id"]);

            if (stringId == null || stringId == "0")
            {
                write_msg("Invalid id.", false);
                Response.End();
                return;
            }

            this.Bugid = Convert.ToInt32(stringId);
            var permissionLevel = Bug.GetBugPermissionLevel(this.Bugid, Security);

            if (permissionLevel == SecurityPermissionLevel.PermissionNone
                || permissionLevel == SecurityPermissionLevel.PermissionReadonly)
            {
                write_msg("You are not allowed to edit this item", false);
                Response.End();
                return;
            }

            if (Security.User.ExternalUser || !ApplicationSettings.EnableInternalOnlyPosts)
            {
                this.internal_only.Visible = false;
                this.internal_only_label.Visible = false;
            }

            if (IsPostBack) on_update(Security);
        }

        public void write_msg(string msg, bool rewritePosts)
        {
            var script = "script"; // C# compiler doesn't like s c r i p t

            Response.Write("<html><" + script + ">");
            Response.Write("function foo() {");
            Response.Write("parent.set_msg('");
            Response.Write(msg);
            Response.Write("'); ");

            if (rewritePosts) Response.Write("parent.opener.rewrite_posts(" + Convert.ToString(this.Bugid) + ")");
            Response.Write("}</" + script + ">");
            Response.Write("<body onload='foo()'>");
            Response.Write("</body></html>");
            Response.End();
        }

        public void on_update(ISecurity security)
        {
            if (this.attached_file.PostedFile == null)
            {
                write_msg("Please select file", false);
                return;
            }

            var filename = Path.GetFileName(this.attached_file.PostedFile.FileName);
            if (string.IsNullOrEmpty(filename))
            {
                write_msg("Please select file", false);
                return;
            }

            var maxUploadSize = ApplicationSettings.MaxUploadSize;
            var contentLength = this.attached_file.PostedFile.ContentLength;

            if (contentLength > maxUploadSize)
            {
                write_msg("File exceeds maximum allowed length of "
                          + Convert.ToString(maxUploadSize)
                          + ".", false);
                return;
            }

            if (contentLength == 0)
            {
                write_msg("No data was uploaded.", false);
                return;
            }

            var good = false;

            try
            {
                Bug.InsertPostAttachment(security, this.Bugid, this.attached_file.PostedFile.InputStream,
                    contentLength,
                    filename, this.desc.Value, this.attached_file.PostedFile.ContentType,
                    -1, // parent
                    this.internal_only.Checked,
                    true);

                good = true;
            }
            catch (Exception ex)
            {
                write_msg("caught exception:" + ex.Message, false);
                return;
            }

            if (good)
                write_msg(
                    filename
                    + " was successfully upload ("
                    + this.attached_file.PostedFile.ContentType
                    + "), "
                    + Convert.ToString(contentLength)
                    + " bytes"
                    , true);
            else
                // This should never happen....
                write_msg("Unexpected error with file upload.", false);
        }
    }
}