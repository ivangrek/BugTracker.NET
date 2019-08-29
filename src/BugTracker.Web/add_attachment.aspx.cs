/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.IO;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class add_attachment : Page
    {
        public int bugid;
        public Security security;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);
            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "add attachment";

            var string_id = Util.sanitize_integer(Request.QueryString["id"]);

            if (string_id == null || string_id == "0")
            {
                write_msg("Invalid id.", false);
                Response.End();
                return;
            }

            this.bugid = Convert.ToInt32(string_id);
            var permission_level = Bug.get_bug_permission_level(this.bugid, this.security);
            if (permission_level == Security.PERMISSION_NONE
                || permission_level == Security.PERMISSION_READONLY)
            {
                write_msg("You are not allowed to edit this item", false);
                Response.End();
                return;
            }

            if (this.security.user.external_user || Util.get_setting("EnableInternalOnlyPosts", "0") == "0")
            {
                this.internal_only.Visible = false;
                this.internal_only_label.Visible = false;
            }

            if (IsPostBack) on_update();
        }

        public void write_msg(string msg, bool rewrite_posts)
        {
            var script = "script"; // C# compiler doesn't like s c r i p t
            Response.Write("<html><" + script + ">");
            Response.Write("function foo() {");
            Response.Write("parent.set_msg('");
            Response.Write(msg);
            Response.Write("'); ");

            if (rewrite_posts) Response.Write("parent.opener.rewrite_posts(" + Convert.ToString(this.bugid) + ")");
            Response.Write("}</" + script + ">");
            Response.Write("<body onload='foo()'>");
            Response.Write("</body></html>");
            Response.End();
        }

        public void on_update()
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

            var max_upload_size = Convert.ToInt32(Util.get_setting("MaxUploadSize", "100000"));
            var content_length = this.attached_file.PostedFile.ContentLength;
            if (content_length > max_upload_size)
            {
                write_msg("File exceeds maximum allowed length of "
                          + Convert.ToString(max_upload_size)
                          + ".", false);
                return;
            }

            if (content_length == 0)
            {
                write_msg("No data was uploaded.", false);
                return;
            }

            var good = false;

            try
            {
                Bug.insert_post_attachment(this.security, this.bugid, this.attached_file.PostedFile.InputStream,
                    content_length,
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
                    + Convert.ToString(content_length)
                    + " bytes"
                    , true);
            else
                // This should never happen....
                write_msg("Unexpected error with file upload.", false);
        }
    }
}