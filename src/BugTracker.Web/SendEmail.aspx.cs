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

    public partial class SendEmail : Page
    {

        public IApplicationSettings ApplicationSettings { get; set; }
        public ISecurity Security { get; set; }

        public bool EnableInternalPosts;
        public int Project = -1;
        protected string Sql {get; set; }

        public void Page_Load(object sender, EventArgs e)
        {
            Util.DoNotCache(Response);

            Security.CheckSecurity(SecurityLevel.AnyUserOkExceptGuest);

            this.MainMenu.SelectedItem = ApplicationSettings.PluralBugLabel;

            Page.Title = $"{ApplicationSettings.AppTitle} - send email";

            this.msg.InnerText = string.Empty;

            var stringBpId = Request["bp_id"];
            var stringBgId = Request["bg_id"];
            var requestTo = Request["to"];
            var reply = Request["reply"];

            this.EnableInternalPosts = ApplicationSettings.EnableInternalOnlyPosts;

            if (!this.EnableInternalPosts)
            {
                this.include_internal_posts.Visible = false;
                this.include_internal_posts_label.Visible = false;
            }

            if (!IsPostBack)
            {
                Session["email_addresses"] = null;

                DataRow dr = null;

                if (stringBpId != null)
                {
                    stringBpId = Util.SanitizeInteger(stringBpId);

                    Sql = @"select
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

                    Sql = Sql.Replace("$id", stringBpId);
                    Sql = Sql.Replace("$us", Convert.ToString(Security.User.Usid));

                    var dv = DbUtil.GetDataView(Sql);
                    dr = null;
                    if (dv.Count > 0)
                    {
                        dv.RowFilter = "bp_id = " + stringBpId;
                        if (dv.Count > 0) dr = dv[0].Row;
                    }

                    var intBgId = (int) dr["bg_id"];
                    var permissionLevel = Bug.GetBugPermissionLevel(intBgId, Security);
                    if (permissionLevel == SecurityPermissionLevel.PermissionNone)
                    {
                        Response.Write("You are not allowed to view this item");
                        Response.End();
                    }

                    if ((int) dr["bp_hidden_from_external_users"] == 1)
                        if (Security.User.ExternalUser)
                        {
                            Response.Write("You are not allowed to view this post");
                            Response.End();
                        }

                    stringBgId = Convert.ToString(dr["bg_id"]);
                    this.back_href.HRef = ResolveUrl($"~/Bugs/Edit.aspx?id={stringBgId}");
                    this.bg_id.Value = stringBgId;

                    this.to.Value = dr["bp_email_from"].ToString();

                    // Work around for a mysterious bug:
                    // http://sourceforge.net/tracker/?func=detail&aid=2815733&group_id=66812&atid=515837
                    if (ApplicationSettings.StripDisplayNameFromEmailAddress)
                    {
                        this.to.Value = Email.SimplifyEmailAddress(this.to.Value);
                    }
                        

                    load_from_dropdown(dr, true); // list the project's email address first

                    if (reply != null && reply == "all")
                    {
                        var regex = new Regex("\n");
                        var lines = regex.Split((string) dr["bp_comment"]);
                        var ccAddrs = string.Empty;

                        var max = lines.Length < 5 ? lines.Length : 5;

                        // gather cc addresses, which might include the current user
                        for (var i = 0; i < max; i++)
                            if (lines[i].StartsWith("To:") || lines[i].StartsWith("Cc:"))
                            {
                                var ccAddr = lines[i].Substring(3, lines[i].Length - 3).Trim();

                                // don't cc yourself

                                if (ccAddr.IndexOf(this.from.SelectedItem.Value) == -1)
                                {
                                    if (!string.IsNullOrEmpty(ccAddrs)) ccAddrs += ",";

                                    ccAddrs += ccAddr;
                                }
                            }

                        this.cc.Value = ccAddrs;
                    }

                    if (!string.IsNullOrEmpty(dr["us_signature"].ToString()))
                    {
                        if (Security.User.UseFckeditor)
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
                            if (Security.User.UseFckeditor)
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

                        var nextLineIsDate = false;
                        for (var i = 0; i < lines.Length; i++)
                            if (i < 4 && (lines[i].IndexOf("To:") == 0 || lines[i].IndexOf("Cc:") == 0))
                            {
                                nextLineIsDate = true;
                                if (Security.User.UseFckeditor)
                                    this.body.Value +=
                                        "&#62;" + lines[i].Replace("<", "&#60;").Replace(">", "&#62;") + "<br>";
                                else
                                    this.body.Value += ">" + lines[i] + "\n";
                            }
                            else if (nextLineIsDate)
                            {
                                nextLineIsDate = false;
                                if (Security.User.UseFckeditor)
                                    this.body.Value +=
                                        "&#62;Date: " + Convert.ToString(dr["bp_date"]) + "<br>&#62;<br>";
                                else
                                    this.body.Value += ">Date: " + Convert.ToString(dr["bp_date"]) + "\n>\n";
                            }
                            else
                            {
                                if (Security.User.UseFckeditor)
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
                        this.to.Value = string.Empty;
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
                else if (stringBgId != null)
                {
                    stringBgId = Util.SanitizeInteger(stringBgId);

                    var permissionLevel = Bug.GetBugPermissionLevel(Convert.ToInt32(stringBgId), Security);
                    if (permissionLevel == SecurityPermissionLevel.PermissionNone
                        || permissionLevel == SecurityPermissionLevel.PermissionReadonly)
                    {
                        Response.Write("You are not allowed to edit this item");
                        Response.End();
                    }

                    Sql = @"select
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

                    Sql = Sql.Replace("$us", Convert.ToString(Security.User.Usid));
                    Sql = Sql.Replace("$bg", stringBgId);

                    dr = DbUtil.GetDataRow(Sql);

                    load_from_dropdown(dr, false); // list the user's email first, then the project

                    this.back_href.HRef = ResolveUrl($"~/Bugs/Edit.aspx?id={stringBgId}");
                    this.bg_id.Value = stringBgId;

                    if (requestTo != null) this.to.Value = requestTo;

                    // Work around for a mysterious bug:
                    // http://sourceforge.net/tracker/?func=detail&aid=2815733&group_id=66812&atid=515837
                    if (ApplicationSettings.StripDisplayNameFromEmailAddress) this.to.Value = Email.SimplifyEmailAddress(this.to.Value);

                    if (!string.IsNullOrEmpty(dr["us_signature"].ToString()))
                    {
                        if (Security.User.UseFckeditor)
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

                if (stringBpId != null || stringBgId != null)
                {
                    this.subject.Value = (string) dr["bg_short_desc"]
                                         + "  (" + ApplicationSettings.TrackingIdString
                                         + this.bg_id.Value
                                         + ")";

                    // for determining which users to show in "address book"
                    this.Project = (int) dr["bg_project"];
                }
            }
            else
            {
                on_update(Security);
            }
        }

        public void load_from_dropdown(DataRow dr, bool projectFirst)
        {
            // format from dropdown
            var projectEmail = dr["pj_pop3_email_from"].ToString();
            var usEmail = dr["us_email"].ToString();
            var usFirstname = dr["us_firstname"].ToString();
            var usLastname = dr["us_lastname"].ToString();

            if (projectFirst)
            {
                if (!string.IsNullOrEmpty(projectEmail))
                {
                    this.from.Items.Add(new ListItem(projectEmail));
                    if (!string.IsNullOrEmpty(usFirstname) && !string.IsNullOrEmpty(usLastname))
                        this.from.Items.Add(
                            new ListItem("\"" + usFirstname + " " + usLastname + "\" <" + projectEmail + ">"));
                }

                if (!string.IsNullOrEmpty(usEmail))
                {
                    this.from.Items.Add(new ListItem(usEmail));
                    if (!string.IsNullOrEmpty(usFirstname) && !string.IsNullOrEmpty(usLastname))
                        this.from.Items.Add(
                            new ListItem("\"" + usFirstname + " " + usLastname + "\" <" + usEmail + ">"));
                }
            }
            else
            {
                if (!string.IsNullOrEmpty(usEmail))
                {
                    this.from.Items.Add(new ListItem(usEmail));
                    if (!string.IsNullOrEmpty(usFirstname) && !string.IsNullOrEmpty(usLastname))
                        this.from.Items.Add(
                            new ListItem("\"" + usFirstname + " " + usLastname + "\" <" + usEmail + ">"));
                }

                if (!string.IsNullOrEmpty(projectEmail))
                {
                    this.from.Items.Add(new ListItem(projectEmail));
                    if (!string.IsNullOrEmpty(usFirstname) && !string.IsNullOrEmpty(usLastname))
                        this.from.Items.Add(
                            new ListItem("\"" + usFirstname + " " + usLastname + "\" <" + projectEmail + ">"));
                }
            }

            if (this.from.Items.Count == 0) this.from.Items.Add(new ListItem("[none]"));
        }

        public bool validate()
        {
            var good = true;

            if (string.IsNullOrEmpty(this.to.Value))
            {
                good = false;
                this.to_err.InnerText = "\"To\" is required.";
            }
            else
            {
                try
                {
                    var dummyMsg = new MailMessage();
                    Email.AddAddressesToEmail(dummyMsg, this.to.Value, Email.AddrType.To);
                    this.to_err.InnerText = string.Empty;
                }
                catch
                {
                    good = false;
                    this.to_err.InnerText = "\"To\" is not in a valid format. Separate multiple addresses with commas.";
                }
            }

            if (!string.IsNullOrEmpty(this.cc.Value))
                try
                {
                    var dummyMsg = new MailMessage();
                    Email.AddAddressesToEmail(dummyMsg, this.cc.Value, Email.AddrType.Cc);
                    this.cc_err.InnerText = string.Empty;
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
                this.from_err.InnerText = string.Empty;
            }

            if (string.IsNullOrEmpty(this.subject.Value))
            {
                good = false;
                this.subject_err.InnerText = "\"Subject\" is required.";
            }
            else
            {
                this.subject_err.InnerText = string.Empty;
            }

            this.msg.InnerText = "Email was not sent.";

            return good;
        }

        public string get_bug_text(int bugid, ISecurity security)
        {
            // Get bug html

            var bugDr = Bug.GetBugDataRow(bugid, security);

            // Create a fake response and let the code
            // write the html to that response
            var writer = new StringWriter();
            var myResponse = new HttpResponse(writer);
            var html = PrintBug.PrintBugNew(bugDr, security,
                true, // include style
                false, // images_inline
                true, // history_inline
                this.include_internal_posts.Checked); // internal_posts

            myResponse.Write(html);

            return writer.ToString();
        }

        public void on_update(ISecurity security)
        {
            if (!validate()) return;

            Sql = @"
insert into bug_posts
    (bp_bug, bp_user, bp_date, bp_comment, bp_comment_search, bp_email_from, bp_email_to, bp_type, bp_content_type, bp_email_cc)
    values($id, $us, getdate(), N'$cm', N'$cs', N'$fr',  N'$to', 'sent', N'$ct', N'$cc');
select scope_identity()
update bugs set
    bg_last_updated_user = $us,
    bg_last_updated_date = getdate()
    where bg_id = $id";

            Sql = Sql.Replace("$id", this.bg_id.Value);
            Sql = Sql.Replace("$us", Convert.ToString(Security.User.Usid));
            if (Security.User.UseFckeditor)
            {
                var adjustedBody = "Subject: " + this.subject.Value + "<br><br>";
                adjustedBody += Util.StripDangerousTags(this.body.Value);

                Sql = Sql.Replace("$cm", adjustedBody.Replace("'", "&#39;"));
                Sql = Sql.Replace("$cs", adjustedBody.Replace("'", "''"));
                Sql = Sql.Replace("$ct", "text/html");
            }
            else
            {
                var adjustedBody = "Subject: " + this.subject.Value + "\n\n";
                adjustedBody += HttpUtility.HtmlDecode(this.body.Value);
                adjustedBody = adjustedBody.Replace("'", "''");

                Sql = Sql.Replace("$cm", adjustedBody);
                Sql = Sql.Replace("$cs", adjustedBody);
                Sql = Sql.Replace("$ct", "text/plain");
            }

            Sql = Sql.Replace("$fr", this.from.SelectedItem.Value.Replace("'", "''"));
            Sql = Sql.Replace("$to", this.to.Value.Replace("'", "''"));
            Sql = Sql.Replace("$cc", this.cc.Value.Replace("'", "''"));

            var commentId = Convert.ToInt32(DbUtil.ExecuteScalar(Sql));

            var attachments = handle_attachments(commentId, security);

            string bodyText;
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
                if (Security.User.UseFckeditor)
                {
                    bodyText = this.body.Value;
                    bodyText += "<br><br>";
                }
                else
                {
                    bodyText = this.body.Value.Replace("\n", "<br>");
                    bodyText = bodyText.Replace("\t", "&nbsp;&nbsp;&nbsp;&nbsp;");
                    bodyText = bodyText.Replace("  ", "&nbsp; ");
                }

                bodyText += "<hr>" + get_bug_text(Convert.ToInt32(this.bg_id.Value), security);

                format = BtnetMailFormat.Html;
            }
            else
            {
                if (Security.User.UseFckeditor)
                {
                    bodyText = this.body.Value;
                    format = BtnetMailFormat.Html;
                }
                else
                {
                    bodyText = HttpUtility.HtmlDecode(this.body.Value);
                    //body_text = body_text.Replace("\n","\r\n");
                    format = BtnetMailFormat.Text;
                }
            }

            var result = Email.SendEmail( // 9 args
                this.to.Value, this.from.SelectedItem.Value, this.cc.Value, this.subject.Value,
                bodyText,
                format,
                priority,
                attachments, this.return_receipt.Checked);

            Bug.SendNotifications(Bug.Update, Convert.ToInt32(this.bg_id.Value), security);
            WhatsNew.AddNews(Convert.ToInt32(this.bg_id.Value), this.short_desc.Value, "email sent", security);

            if (string.IsNullOrEmpty(result))
                Response.Redirect($"~/Bugs/Edit.aspx?id={this.bg_id.Value}");
            else
                this.msg.InnerText = result;
        }

        public int[] handle_attachments(int commentId, ISecurity security)
        {
            var attachments = new ArrayList();

            var filename = Path.GetFileName(this.attached_file.PostedFile.FileName);
            if (!string.IsNullOrEmpty(filename))
            {
                //add attachment
                var maxUploadSize = ApplicationSettings.MaxUploadSize;
                var contentLength = this.attached_file.PostedFile.ContentLength;
                if (contentLength > maxUploadSize)
                {
                    this.msg.InnerText = "File exceeds maximum allowed length of "
                                         + Convert.ToString(maxUploadSize)
                                         + ".";
                    return null;
                }

                if (contentLength == 0)
                {
                    this.msg.InnerText = "No data was uploaded.";
                    return null;
                }

                var bpId = Bug.InsertPostAttachment(security,
                    Convert.ToInt32(this.bg_id.Value), this.attached_file.PostedFile.InputStream,
                    contentLength,
                    filename,
                    "email attachment", this.attached_file.PostedFile.ContentType,
                    commentId,
                    false, false);

                attachments.Add(bpId);
            }

            //attachments to forward

            foreach (ListItem itemAttachment in this.lstAttachments.Items)
                if (itemAttachment.Selected)
                {
                    var bpId = Convert.ToInt32(itemAttachment.Value);

                    Bug.InsertPostAttachmentCopy(security, Convert.ToInt32(this.bg_id.Value), bpId,
                        "email attachment", commentId, false, false);
                    attachments.Add(bpId);
                }

            return (int[]) attachments.ToArray(typeof(int));
        }
    }
}