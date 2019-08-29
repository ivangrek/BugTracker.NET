/*
    Copyright 2002-2011 Corey Trager

    Distributed under the terms of the GNU General Public License
*/

// disable System.Net.Mail warnings
//#pragma warning disable 618
//#warning System.Web.Mail is deprecated, but it doesn't work yet with "explicit" SSL, so keeping it for now - corey

namespace BugTracker.Web.Core
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Configuration;
    using System.IO;
    using System.Net;
    using System.Net.Configuration;
    using System.Net.Mail;
    using System.Text;
    using System.Threading;

    public enum BtnetMailFormat
    {
        Text,
        Html
    }

    public enum BtnetMailPriority
    {
        Normal,
        Low,
        High
    }

    public class Email
    {
        public enum AddrType
        {
            to,
            cc
        }

        public static string send_email( // 5 args
            string to,
            string from,
            string cc,
            string subject,
            string body)
        {
            return send_email(
                to,
                from,
                cc,
                subject,
                body,
                BtnetMailFormat.Text,
                BtnetMailPriority.Normal,
                null,
                false);
        }

        public static string send_email( // 6 args
            string to,
            string from,
            string cc,
            string subject,
            string body,
            BtnetMailFormat body_format)
        {
            return send_email(
                to,
                from,
                cc,
                subject,
                body,
                body_format,
                BtnetMailPriority.Normal,
                null,
                false);
        }

        private static string convert_uploaded_blob_to_flat_file(string upload_folder, int attachment_bpid,
            Dictionary<string, int> files_to_delete)
        {
            var buffer = new byte[16 * 1024];
            string dest_path_and_filename;
            var bpa = Bug.get_bug_post_attachment(attachment_bpid);
            using (bpa.content)
            {
                dest_path_and_filename = Path.Combine(upload_folder, bpa.file);

                // logic to rename in case of dupes.  MS Outlook embeds images all with the same filename
                var suffix = 0;
                var renamed_to_prevent_dupe = dest_path_and_filename;
                while (files_to_delete.ContainsKey(renamed_to_prevent_dupe))
                {
                    suffix++;
                    renamed_to_prevent_dupe = Path.Combine(upload_folder,
                        Path.GetFileNameWithoutExtension(bpa.file)
                        + Convert.ToString(suffix)
                        + Path.GetExtension(bpa.file));
                }

                dest_path_and_filename = renamed_to_prevent_dupe;

                // Save to disk
                using (var out_stream = new FileStream(
                    dest_path_and_filename,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.Delete))
                {
                    var bytes_read = bpa.content.Read(buffer, 0, buffer.Length);
                    while (bytes_read != 0)
                    {
                        out_stream.Write(buffer, 0, bytes_read);

                        bytes_read = bpa.content.Read(buffer, 0, buffer.Length);
                    }

                    out_stream.Close();
                }
            }

            files_to_delete[dest_path_and_filename] = 1;

            return dest_path_and_filename;
        }

        public static string send_email(
            string to,
            string from,
            string cc,
            string subject,
            string body,
            BtnetMailFormat body_format,
            BtnetMailPriority priority,
            int[] attachment_bpids,
            bool return_receipt)
        {
            var msg = new MailMessage();

            msg.From = new MailAddress(from);

            add_addresses_to_email(msg, to, AddrType.to);

            if (!string.IsNullOrEmpty(cc.Trim())) add_addresses_to_email(msg, cc, AddrType.cc);

            msg.Subject = subject;

            if (priority == BtnetMailPriority.Normal)
                msg.Priority = MailPriority.Normal;
            else if (priority == BtnetMailPriority.High)
                msg.Priority = MailPriority.High;
            else
                priority = BtnetMailPriority.Low;

            // This fixes a bug for a couple people, but make it configurable, just in case.
            if (Util.get_setting("BodyEncodingUTF8", "1") == "1") msg.BodyEncoding = Encoding.UTF8;

            if (return_receipt) msg.Headers.Add("Disposition-Notification-To", from);

            // workaround for a bug I don't understand...
            if (Util.get_setting("SmtpForceReplaceOfBareLineFeeds", "0") == "1") body = body.Replace("\n", "\r\n");

            msg.Body = body;
            msg.IsBodyHtml = body_format == BtnetMailFormat.Html;

            StuffToDelete stuff_to_delete = null;

            if (attachment_bpids != null && attachment_bpids.Length > 0)
            {
                stuff_to_delete = new StuffToDelete();

                var upload_folder = Util.get_upload_folder();

                if (string.IsNullOrEmpty(upload_folder))
                {
                    upload_folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    Directory.CreateDirectory(upload_folder);
                    stuff_to_delete.directories_to_delete.Add(upload_folder);
                }

                foreach (var attachment_bpid in attachment_bpids)
                {
                    var dest_path_and_filename = convert_uploaded_blob_to_flat_file(upload_folder, attachment_bpid,
                        stuff_to_delete.files_to_delete);

                    // Add saved file as attachment
                    var mail_attachment = new Attachment(
                        dest_path_and_filename);

                    msg.Attachments.Add(mail_attachment);
                }
            }

            try
            {
                // This fixes a bug for some people.  Not sure how it happens....
                msg.Body = msg.Body.Replace(Convert.ToChar(0), ' ').Trim();

                var smtpClient = new SmtpClient();

                // SSL or not
                var force_ssl = Util.get_setting("SmtpForceSsl", "");

                if (force_ssl == "")
                {
                    // get the port so that we can guess whether SSL or not
                    var smtpSec = (SmtpSection)
                        ConfigurationManager.GetSection("system.net/mailSettings/smtp");

                    if (smtpSec.Network.Port == 465
                        || smtpSec.Network.Port == 587)
                        smtpClient.EnableSsl = true;
                    else
                        smtpClient.EnableSsl = false;
                }
                else
                {
                    if (force_ssl == "1")
                        smtpClient.EnableSsl = true;
                    else
                        smtpClient.EnableSsl = false;
                }

                // Ignore certificate errors
                if (smtpClient.EnableSsl)
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                smtpClient.Send(msg);

                if (stuff_to_delete != null)
                {
                    stuff_to_delete.msg = msg;
                    delete_stuff(stuff_to_delete);
                }

                return "";
            }
            catch (Exception e)
            {
                Util.write_to_log("There was a problem sending email.   Check settings in Web.config.");
                Util.write_to_log("TO:" + to);
                Util.write_to_log("FROM:" + from);
                Util.write_to_log("SUBJECT:" + subject);
                Util.write_to_log(e.GetBaseException().Message);

                delete_stuff(stuff_to_delete);

                return e.GetBaseException().Message;
            }
        }

        protected static void delete_stuff(StuffToDelete stuff_to_delete)
        {
            var thread = new Thread(threadproc_delete_stuff);
            thread.Start(stuff_to_delete);
        }

        protected static void actually_delete_stuff(StuffToDelete stuff_to_delete)
        {
            if (stuff_to_delete == null) // not sure how this could happen, but it fixed a bug for one guy
                return;

            stuff_to_delete.msg.Dispose(); // if we don't do this, the delete tends not to work.

            foreach (var file in stuff_to_delete.files_to_delete.Keys) File.Delete(file);

            foreach (string directory in stuff_to_delete.directories_to_delete) Directory.Delete(directory);
        }

        public static void threadproc_delete_stuff(object obj)
        {
            // Allow time for SMTP to be done with these files.
            try
            {
                Thread.Sleep(60 * 1000);
                actually_delete_stuff((StuffToDelete) obj);
            }
            catch (ThreadAbortException)
            {
                actually_delete_stuff((StuffToDelete) obj);
            }
        }

        public static void add_addresses_to_email(MailMessage msg, string addrs, AddrType addr_type)
        {
            Util.write_to_log("to email addr: " + addrs);

            var separator_char = Util.get_setting("EmailAddressSeparatorCharacter", ",");

            var addr_array = addrs.Replace(separator_char + " ", separator_char).Split(separator_char[0]);

            for (var i = 0; i < addr_array.Length; i++)
            {
                var just_address = simplify_email_address(addr_array[i]);
                var just_display_name = addr_array[i].Replace(just_address, "").Replace("<>", "");
                if (addr_type == AddrType.to)
                    msg.To.Add(new MailAddress(just_address, just_display_name, Encoding.UTF8));
                else
                    msg.CC.Add(new MailAddress(just_address, just_display_name, Encoding.UTF8));
            }
        }

        public static string simplify_email_address(string email)
        {
            // convert "Corey Trager <ctrager@yahoo.com>" to just "ctrager@yahoo.com"

            var pos1 = email.IndexOf("<");
            var pos2 = email.IndexOf(">");

            if (pos1 >= 0 && pos2 > pos1)
                return email.Substring(pos1 + 1, pos2 - pos1 - 1);
            return email;
        }

        public class StuffToDelete
        {
            public ArrayList directories_to_delete = new ArrayList();
            public Dictionary<string, int> files_to_delete = new Dictionary<string, int>();
            public MailMessage msg;
        }
    } // end Email
} // end namespace