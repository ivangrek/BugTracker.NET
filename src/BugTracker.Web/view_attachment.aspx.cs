/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Text;
    using System.Web;
    using System.Web.UI;
    using Core;

    public partial class view_attachment : Page
    {
        public Security security;

        public void Page_Load(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(Request["bug_id"]))
                // This is to prevent exceoptions and error emails from getting triggered
                // by "Microsoft Office Existence Discovery".  Google it for more info.
                Response.End();

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK);

            var bp_id = Util.sanitize_integer(Request["id"]);
            var bug_id = Util.sanitize_integer(Request["bug_id"]);

            var sql = @"
select bp_file, isnull(bp_content_type,'') [bp_content_type] 
from bug_posts 
where bp_id = $bp_id 
and bp_bug = $bug_id";

            sql = sql.Replace("$bp_id", bp_id);
            sql = sql.Replace("$bug_id", bug_id);

            var dr = DbUtil.get_datarow(sql);

            if (dr == null) Response.End();

            var permission_level = Bug.get_bug_permission_level(Convert.ToInt32(bug_id), this.security);
            if (permission_level == Security.PERMISSION_NONE)
            {
                Response.Write("You are not allowed to view this item");
                Response.End();
            }

            var var = Request["download"];
            bool download;
            if (var == null || var == "1")
                download = true;
            else
                download = false;

            var filename = (string) dr["bp_file"];
            var content_type = (string) dr["bp_content_type"];

            // First, try to find it in the bug_post_attachments table.
            sql = @"select bpa_content
            from bug_post_attachments
            where bpa_post = @bp_id";

            var foundInDatabase = false;
            string foundAtPath = null;
            using (var cmd = new SqlCommand(sql))
            {
                cmd.Parameters.AddWithValue("@bp_id", bp_id);

                // Use an SqlDataReader so that we can write out the blob data in chunks.

                using (var reader =
                    DbUtil.execute_reader(cmd, CommandBehavior.CloseConnection | CommandBehavior.SequentialAccess))
                {
                    if (reader.Read()) // Did we find the content in the database?
                    {
                        foundInDatabase = true;
                    }
                    else
                    {
                        // Otherwise, try to find the content in the UploadFolder.

                        var upload_folder = Util.get_upload_folder();
                        if (upload_folder != null)
                        {
                            var path = new StringBuilder(upload_folder);
                            path.Append("\\");
                            path.Append(bug_id);
                            path.Append("_");
                            path.Append(bp_id);
                            path.Append("_");
                            path.Append(filename);

                            if (File.Exists(path.ToString())) foundAtPath = path.ToString();
                        }
                    }

                    // We must have found the content in the database or on the disk to proceed.

                    if (!foundInDatabase && foundAtPath == null)
                    {
                        Response.Write("File not found:<br>" + filename);
                        return;
                    }

                    // Write the ContentType header.

                    if (string.IsNullOrEmpty(content_type))
                        Response.ContentType = Util.filename_to_content_type(filename);
                    else
                        Response.ContentType = content_type;

                    if (download)
                    {
                        Response.AddHeader("content-disposition", "attachment; filename=\"" + filename + "\"");
                    }
                    else
                    {
                        Response.Cache.SetExpires(DateTime.Now.AddDays(3));
                        Response.AddHeader("content-disposition", "inline; filename=\"" + filename + "\"");
                    }

                    // Write the data.

                    if (foundInDatabase)
                    {
                        long totalRead = 0;
                        var dataLength = reader.GetBytes(0, 0, null, 0, 0);
                        var buffer = new byte[16 * 1024];

                        while (totalRead < dataLength)
                        {
                            var bytesRead = reader.GetBytes(0, totalRead, buffer, 0,
                                (int) Math.Min(dataLength - totalRead, buffer.Length));
                            totalRead += bytesRead;

                            Response.OutputStream.Write(buffer, 0, (int) bytesRead);
                        }
                    }
                    else if (foundAtPath != null)
                    {
                        if (Util.get_setting("UseTransmitFileInsteadOfWriteFile", "0") == "1")
                            Response.TransmitFile(foundAtPath);
                        else
                            Response.WriteFile(foundAtPath);
                    }
                    else
                    {
                        Response.Write("File not found:<br>" + filename);
                    }
                } // end using sql reader
            } // end using sql command
        } // end page load
    }
}