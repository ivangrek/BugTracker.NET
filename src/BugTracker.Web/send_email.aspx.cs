/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Collections;
    using System.Data;
    using System.IO;
    using System.Net.Mail;
    using System.Text.RegularExpressions;
    using System.Web;
    using System.Web.UI;
    using System.Web.UI.WebControls;
    using Core;

    public partial class send_email : Page
    {
        public bool enable_internal_posts;
        public int project = -1;

        public Security security;
        public string sql;

        public void Page_Load(object sender, EventArgs e)
        {
            Util.do_not_cache(Response);

            this.security = new Security();
            this.security.check_security(HttpContext.Current, Security.ANY_USER_OK_EXCEPT_GUEST);

            Page.Title = Util.get_setting("AppTitle", "BugTracker.NET") + " - "
                                                                        + "send email";

            this.msg.InnerText = "";

            var string_bp_id = Request["bp_id"];
            var string_bg_id = Request["bg_id"];
            var request_to = Request["to"];
            var reply = Request["reply"];

            this.enable_internal_posts = Util.get_setting("EnableInternalOnlyPosts", "0") == "1";

            if (!this.enable_internal_posts)
            {
                this.include_internal_posts.Visible = false;
                this.include_internal_posts_label.Visible = false;
            }

            if (!IsPostBack)
            {
                Session["email_addresses"] = null;

                DataRow dr = null;

                if (string_bp_id != null)
                {
                    string_bp_id = Util.sanitize_integer(string_bp_id);

                    this.sql = @"select
				bp_parent,
                bp_file,
                bp_id,
				bg_id,
				bg_short_desc,
				bp_email_from,
				bp_comment,
				bp_email_from,
				bp_date,
				bp_type,
                bp_content_type,
				bg_project,
                bp_hidden_from_external_users,
				isnull(us_signature,'') [us_signature],
				isnull(pj_pop3_email_from,'') [pj_pop3_email_from],
				isnull(us_email,'') [us_email],
				isnull(us_firstname,'') [us_firstname],
				isnull(us_lastname,'') [us_lastname]				
				from bug_posts
				inner join bugs on bp_bug = bg_id
				inner join users on us_id = $us
				left outer join projects on bg_project = pj_id
				where bp_id = $id
				or (bp_parent = $id and bp_type='file')";

                    this.sql = this.sql.Replace("$id", string_bp_id);
                    this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));

                    var dv = DbUtil.get_dataview(this.sql);
                    dr = null;
                    if (dv.Count > 0)
                    {
                        dv.RowFilter = "bp_id = " + string_bp_id;
                        if (dv.Count > 0) dr = dv[0].Row;
                    }

                    var int_bg_id = (int) dr["bg_id"];
                    var permission_level = Bug.get_bug_permission_level(int_bg_id, this.security);
                    if (permission_level == Security.PERMISSION_NONE)
                    {
                        Response.Write("You are not allowed to view this item");
                        Response.End();
                    }

                    if ((int) dr["bp_hidden_from_external_users"] == 1)
                        if (this.security.user.external_user)
                        {
                            Response.Write("You are not allowed to view this post");
                            Response.End();
                        }

                    string_bg_id = Convert.ToString(dr["bg_id"]);
                    this.back_href.HRef = "edit_bug.aspx?id=" + string_bg_id;
                    this.bg_id.Value = string_bg_id;

                    this.to.Value = dr["bp_email_from"].ToString();

                    // Work around for a mysterious bug:
                    // http://sourceforge.net/tracker/?func=detail&aid=2815733&group_id=66812&atid=515837
                    if (Util.get_setting("StripDisplayNameFromEmailAddress", "0") == "1")
                        this.to.Value = Email.simplify_email_address(this.to.Value);

                    load_from_dropdown(dr, true); // list the project's email address first

                    if (reply != null && reply == "all")
                    {
                        var regex = new Regex("\n");
                        var lines = regex.Split((string) dr["bp_comment"]);
                        var cc_addrs = "";

                        var max = lines.Length < 5 ? lines.Length : 5;

                        // gather cc addresses, which might include the current user
                        for (var i = 0; i < max; i++)
                            if (lines[i].StartsWith("To:") || lines[i].StartsWith("Cc:"))
                            {
                                var cc_addr = lines[i].Substring(3, lines[i].Length - 3).Trim();

                                // don't cc yourself

                                if (cc_addr.IndexOf(this.from.SelectedItem.Value) == -1)
                                {
                                    if (cc_addrs != "") cc_addrs += ",";

                                    cc_addrs += cc_addr;
                                }
                            }

                        this.cc.Value = cc_addrs;
                    }

                    if (dr["us_signature"].ToString() != "")
                    {
                        if (this.security.user.use_fckeditor)
                        {
                            this.body.Value += "<br><br><br>";
                            this.body.Value += dr["us_signature"].ToString().Replace("\r\n", "<br>");
                            this.body.Value += "<br><br><br>";
                        }
                        else
                        {
                            this.body.Value += "\n\n\n";
                            this.body.Value += dr["us_signature"].ToString();
                            this.body.Value += "\n\n\n";
                        }
                    }

                    if (Request["quote"] != null)
                    {
                        var regex = new Regex("\n");
                        var lines = regex.Split((string) dr["bp_comment"]);

                        if (dr["bp_type"].ToString() == "received")
                        {
                            if (this.security.user.use_fckeditor)
                            {
                                this.body.Value += "<br><br><br>";
                                this.body.Value += "&#62;From: " +
                                                   dr["bp_email_from"].ToString().Replace("<", "&#60;")
                                                       .Replace(">", "&#62;") + "<br>";
                            }
                            else
                            {
                                this.body.Value += "\n\n\n";
                                this.body.Value += ">From: " + dr["bp_email_from"] + "\n";
                            }
                        }

                        var next_line_is_date = false;
                        for (var i = 0; i < lines.Length; i++)
                            if (i < 4 && (lines[i].IndexOf("To:") == 0 || lines[i].IndexOf("Cc:") == 0))
                            {
                                next_line_is_date = true;
                                if (this.security.user.use_fckeditor)
                                    this.body.Value +=
                                        "&#62;" + lines[i].Replace("<", "&#60;").Replace(">", "&#62;") + "<br>";
                                else
                                    this.body.Value += ">" + lines[i] + "\n";
                            }
                            else if (next_line_is_date)
                            {
                                next_line_is_date = false;
                                if (this.security.user.use_fckeditor)
                                    this.body.Value +=
                                        "&#62;Date: " + Convert.ToString(dr["bp_date"]) + "<br>&#62;<br>";
                                else
                                    this.body.Value += ">Date: " + Convert.ToString(dr["bp_date"]) + "\n>\n";
                            }
                            else
                            {
                                if (this.security.user.use_fckeditor)
                                {
                                    if (Convert.ToString(dr["bp_content_type"]) != "text/html")
                                    {
                                        this.body.Value +=
                                            "&#62;" + lines[i].Replace("<", "&#60;").Replace(">", "&#62;") +
                                            "<br>";
                                    }
                                    else
                                    {
                                        if (i == 0) this.body.Value += "<hr>";

                                        this.body.Value += lines[i];
                                    }
                                }
                                else
                                {
                                    this.body.Value += ">" + lines[i] + "\n";
                                }
                            }
                    }

                    if (reply == "forward")
                    {
                        this.to.Value = "";
                        //original attachments
                        //dv.RowFilter = "bp_parent = " + string_bp_id;
                        dv.RowFilter = "bp_type = 'file'";
                        foreach (DataRowView drv in dv)
                        {
                            this.attachments_label.InnerText = "Select attachments to forward:";
                            this.lstAttachments.Items.Add(new ListItem(drv["bp_file"].ToString(),
                                drv["bp_id"].ToString()));
                        }
                    }
                }
                else if (string_bg_id != null)
                {
                    string_bg_id = Util.sanitize_integer(string_bg_id);

                    var permission_level = Bug.get_bug_permission_level(Convert.ToInt32(string_bg_id), this.security);
                    if (permission_level == Security.PERMISSION_NONE
                        || permission_level == Security.PERMISSION_READONLY)
                    {
                        Response.Write("You are not allowed to edit this item");
                        Response.End();
                    }

                    this.sql = @"select
				bg_short_desc,
				bg_project,
				isnull(us_signature,'') [us_signature],
				isnull(us_email,'') [us_email],
				isnull(us_firstname,'') [us_firstname],
				isnull(us_lastname,'') [us_lastname],
				isnull(pj_pop3_email_from,'') [pj_pop3_email_from]
				from bugs
				inner join users on us_id = $us
				left outer join projects on bg_project = pj_id
				where bg_id = $bg";

                    this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));
                    this.sql = this.sql.Replace("$bg", string_bg_id);

                    dr = DbUtil.get_datarow(this.sql);

                    load_from_dropdown(dr, false); // list the user's email first, then the project

                    this.back_href.HRef = "edit_bug.aspx?id=" + string_bg_id;
                    this.bg_id.Value = string_bg_id;

                    if (request_to != null) this.to.Value = request_to;

                    // Work around for a mysterious bug:
                    // http://sourceforge.net/tracker/?func=detail&aid=2815733&group_id=66812&atid=515837
                    if (Util.get_setting("StripDisplayNameFromEmailAddress", "0") == "1")
                        this.to.Value = Email.simplify_email_address(this.to.Value);

                    if (dr["us_signature"].ToString() != "")
                    {
                        if (this.security.user.use_fckeditor)
                        {
                            this.body.Value += "<br><br><br>";
                            this.body.Value += dr["us_signature"].ToString().Replace("\r\n", "<br>");
                        }
                        else
                        {
                            this.body.Value += "\n\n\n";
                            this.body.Value += dr["us_signature"].ToString();
                        }
                    }
                }

                this.short_desc.Value = (string) dr["bg_short_desc"];

                if (string_bp_id != null || string_bg_id != null)
                {
                    this.subject.Value = (string) dr["bg_short_desc"]
                                         + "  (" + Util.get_setting("TrackingIdString", "DO NOT EDIT THIS:")
                                         + this.bg_id.Value
                                         + ")";

                    // for determining which users to show in "address book"
                    this.project = (int) dr["bg_project"];
                }
            }
            else
            {
                on_update();
            }
        }

        public void load_from_dropdown(DataRow dr, bool project_first)
        {
            // format from dropdown
            var project_email = dr["pj_pop3_email_from"].ToString();
            var us_email = dr["us_email"].ToString();
            var us_firstname = dr["us_firstname"].ToString();
            var us_lastname = dr["us_lastname"].ToString();

            if (project_first)
            {
                if (project_email != "")
                {
                    this.from.Items.Add(new ListItem(project_email));
                    if (us_firstname != "" && us_lastname != "")
                        this.from.Items.Add(
                            new ListItem("\"" + us_firstname + " " + us_lastname + "\" <" + project_email + ">"));
                }

                if (us_email != "")
                {
                    this.from.Items.Add(new ListItem(us_email));
                    if (us_firstname != "" && us_lastname != "")
                        this.from.Items.Add(
                            new ListItem("\"" + us_firstname + " " + us_lastname + "\" <" + us_email + ">"));
                }
            }
            else
            {
                if (us_email != "")
                {
                    this.from.Items.Add(new ListItem(us_email));
                    if (us_firstname != "" && us_lastname != "")
                        this.from.Items.Add(
                            new ListItem("\"" + us_firstname + " " + us_lastname + "\" <" + us_email + ">"));
                }

                if (project_email != "")
                {
                    this.from.Items.Add(new ListItem(project_email));
                    if (us_firstname != "" && us_lastname != "")
                        this.from.Items.Add(
                            new ListItem("\"" + us_firstname + " " + us_lastname + "\" <" + project_email + ">"));
                }
            }

            if (this.from.Items.Count == 0) this.from.Items.Add(new ListItem("[none]"));
        }

        public bool validate()
        {
            var good = true;

            if (this.to.Value == "")
            {
                good = false;
                this.to_err.InnerText = "\"To\" is required.";
            }
            else
            {
                try
                {
                    var dummy_msg = new MailMessage();
                    Email.add_addresses_to_email(dummy_msg, this.to.Value, Email.AddrType.to);
                    this.to_err.InnerText = "";
                }
                catch
                {
                    good = false;
                    this.to_err.InnerText = "\"To\" is not in a valid format. Separate multiple addresses with commas.";
                }
            }

            if (this.cc.Value != "")
                try
                {
                    var dummy_msg = new MailMessage();
                    Email.add_addresses_to_email(dummy_msg, this.cc.Value, Email.AddrType.cc);
                    this.cc_err.InnerText = "";
                }
                catch
                {
                    good = false;
                    this.cc_err.InnerText = "\"CC\" is not in a valid format. Separate multiple addresses with commas.";
                }

            if (this.from.SelectedItem.Value == "[none]")
            {
                good = false;
                this.from_err.InnerText = "\"From\" is required.  Use \"settings\" to fix.";
            }
            else
            {
                this.from_err.InnerText = "";
            }

            if (this.subject.Value == "")
            {
                good = false;
                this.subject_err.InnerText = "\"Subject\" is required.";
            }
            else
            {
                this.subject_err.InnerText = "";
            }

            this.msg.InnerText = "Email was not sent.";

            return good;
        }

        public string get_bug_text(int bugid)
        {
            // Get bug html

            var bug_dr = Bug.get_bug_datarow(bugid, this.security);

            // Create a fake response and let the code
            // write the html to that response
            var writer = new StringWriter();
            var my_response = new HttpResponse(writer);
            PrintBug.print_bug(my_response,
                bug_dr, this.security,
                true, // include style
                false, // images_inline
                true, // history_inline
                this.include_internal_posts.Checked); // internal_posts

            return writer.ToString();
        }

        public void on_update()
        {
            if (!validate()) return;

            this.sql = @"
insert into bug_posts
	(bp_bug, bp_user, bp_date, bp_comment, bp_comment_search, bp_email_from, bp_email_to, bp_type, bp_content_type, bp_email_cc)
	values($id, $us, getdate(), N'$cm', N'$cs', N'$fr',  N'$to', 'sent', N'$ct', N'$cc');
select scope_identity()
update bugs set
	bg_last_updated_user = $us,
	bg_last_updated_date = getdate()
	where bg_id = $id";

            this.sql = this.sql.Replace("$id", this.bg_id.Value);
            this.sql = this.sql.Replace("$us", Convert.ToString(this.security.user.usid));
            if (this.security.user.use_fckeditor)
            {
                var adjusted_body = "Subject: " + this.subject.Value + "<br><br>";
                adjusted_body += Util.strip_dangerous_tags(this.body.Value);

                this.sql = this.sql.Replace("$cm", adjusted_body.Replace("'", "&#39;"));
                this.sql = this.sql.Replace("$cs", adjusted_body.Replace("'", "''"));
                this.sql = this.sql.Replace("$ct", "text/html");
            }
            else
            {
                var adjusted_body = "Subject: " + this.subject.Value + "\n\n";
                adjusted_body += HttpUtility.HtmlDecode(this.body.Value);
                adjusted_body = adjusted_body.Replace("'", "''");

                this.sql = this.sql.Replace("$cm", adjusted_body);
                this.sql = this.sql.Replace("$cs", adjusted_body);
                this.sql = this.sql.Replace("$ct", "text/plain");
            }

            this.sql = this.sql.Replace("$fr", this.from.SelectedItem.Value.Replace("'", "''"));
            this.sql = this.sql.Replace("$to", this.to.Value.Replace("'", "''"));
            this.sql = this.sql.Replace("$cc", this.cc.Value.Replace("'", "''"));

            var comment_id = Convert.ToInt32(DbUtil.execute_scalar(this.sql));

            var attachments = handle_attachments(comment_id);

            string body_text;
            BtnetMailFormat format;
            BtnetMailPriority priority;

            switch (this.prior.SelectedItem.Value)
            {
                case "High":
                    priority = BtnetMailPriority.High;
                    break;
                case "Low":
                    priority = BtnetMailPriority.Low;
                    break;
                default:
                    priority = BtnetMailPriority.Normal;
                    break;
            }

            if (this.include_bug.Checked)
            {
                // white space isn't handled well, I guess.
                if (this.security.user.use_fckeditor)
                {
                    body_text = this.body.Value;
                    body_text += "<br><br>";
                }
                else
                {
                    body_text = this.body.Value.Replace("\n", "<br>");
                    body_text = body_text.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");
                    body_text = body_text.Replace("  ", "&nbsp; ");
                }

                body_text += "<hr>" + get_bug_text(Convert.ToInt32(this.bg_id.Value));

                format = BtnetMailFormat.Html;
            }
            else
            {
                if (this.security.user.use_fckeditor)
                {
                    body_text = this.body.Value;
                    format = BtnetMailFormat.Html;
                }
                else
                {
                    body_text = HttpUtility.HtmlDecode(this.body.Value);
                    //body_text = body_text.Replace("\n","\r\n");
                    format = BtnetMailFormat.Text;
                }
            }

            var result = Email.send_email( // 9 args
                this.to.Value, this.from.SelectedItem.Value, this.cc.Value, this.subject.Value,
                body_text,
                format,
                priority,
                attachments, this.return_receipt.Checked);

            Bug.send_notifications(Bug.UPDATE, Convert.ToInt32(this.bg_id.Value), this.security);
            WhatsNew.add_news(Convert.ToInt32(this.bg_id.Value), this.short_desc.Value, "email sent", this.security);

            if (result == "")
                Response.Redirect("edit_bug.aspx?id=" + this.bg_id.Value);
            else
                this.msg.InnerText = result;
        }

        public int[] handle_attachments(int comment_id)
        {
            var attachments = new ArrayList();

            var filename = Path.GetFileName(this.attached_file.PostedFile.FileName);
            if (filename != "")
            {
                //add attachment
                var max_upload_size = Convert.ToInt32(Util.get_setting("MaxUploadSize", "100000"));
                var content_length = this.attached_file.PostedFile.ContentLength;
                if (content_length > max_upload_size)
                {
                    this.msg.InnerText = "File exceeds maximum allowed length of "
                                         + Convert.ToString(max_upload_size)
                                         + ".";
                    return null;
                }

                if (content_length == 0)
                {
                    this.msg.InnerText = "No data was uploaded.";
                    return null;
                }

                var bp_id = Bug.insert_post_attachment(this.security,
                    Convert.ToInt32(this.bg_id.Value), this.attached_file.PostedFile.InputStream,
                    content_length,
                    filename,
                    "email attachment", this.attached_file.PostedFile.ContentType,
                    comment_id,
                    false, false);

                attachments.Add(bp_id);
            }

            //attachments to forward

            foreach (ListItem item_attachment in this.lstAttachments.Items)
                if (item_attachment.Selected)
                {
                    var bp_id = Convert.ToInt32(item_attachment.Value);

                    Bug.insert_post_attachment_copy(this.security, Convert.ToInt32(this.bg_id.Value), bp_id,
                        "email attachment", comment_id, false, false);
                    attachments.Add(bp_id);
                }

            return (int[]) attachments.ToArray(typeof(int));
        }
    }
}