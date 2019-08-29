/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.IO;
    using System.Text;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class delete_attachment : Page
    {
        public Security security;
        public string sql;

        public void Page_Init(object sender, EventArgs e)
        {
            ViewStateUserKey = Session.SessionID;
        }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();

            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK_EXCEPT_GUEST);

            if (this.security.user.is_admin || this.security.user.can_edit_and_delete_posts)
            {
                //
            }
            else
            {
                Response.Write("You are not allowed to use this page.");
                Response.End();
            }

            var attachment_id_string = Util.sanitize_integer(Request["id"]);
            var bug_id_string = Util.sanitize_integer(Request["bug_id"]);

            var permission_level = Bug.get_bug_permission_level(Convert.ToInt32(bug_id_string), this.security);
            if (permission_level != Security.PERMISSION_ALL)
            {
                Response.Write("You are not allowed to edit this item");
                Response.End();
            }

            if (IsPostBack)
            {
                // save the filename before deleting the row
                this.sql = @"select bp_file from bug_posts where bp_id = $ba";
                this.sql = this.sql.Replace("$ba", attachment_id_string);
                var filename = (string) DbUtil.execute_scalar(this.sql);

                // delete the row representing the attachment
                this.sql = @"delete bug_post_attachments where bpa_post = $ba
            delete bug_posts where bp_id = $ba";
                this.sql = this.sql.Replace("$ba", attachment_id_string);
                DbUtil.execute_nonquery(this.sql);

                // delete the file too
                var upload_folder = Util.get_upload_folder();
                if (upload_folder != null)
                {
                    var path = new StringBuilder(upload_folder);
                    path.Append("\\");
                    path.Append(bug_id_string);
                    path.Append("_");
                    path.Append(attachment_id_string);
                    path.Append("_");
                    path.Append(filename);
                    if (File.Exists(path.ToString())) File.Delete(path.ToString());
                }

                Response.Redirect("edit_bug.aspx?id=" + bug_id_string);
            }
            else
            {
                Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                            + "delete attachment";

                this.back_href.HRef = "edit_bug.aspx?id=" + bug_id_string;

                this.sql = @"select bp_file from bug_posts where bp_id = $1";
                this.sql = this.sql.Replace("$1", attachment_id_string);

                var dr = DbUtil.get_datarow(this.sql);

                var s = Convert.ToString(dr["bp_file"]);

                this.confirm_href.InnerText = "confirm delete of attachment: " + s;

                this.row_id.Value = attachment_id_string;
            }
        }
    }
}