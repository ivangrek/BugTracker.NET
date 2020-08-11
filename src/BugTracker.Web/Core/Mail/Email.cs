/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

// disable System.Net.Mail warnings
//#pragma warning disable 618
//#warning System.Web.Mail is deprecated, but it doesn't work yet with "explicit" SSL, so keeping it for now - corey

namespace BugTracker.Web.Core.Mail
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

    public static class Email
    {
        private static IApplicationSettings ApplicationSettings { get; } = new ApplicationSettings();

        public enum AddrType
        {
            To,
            Cc
        }

        public static string SendEmail( // 5 args
            string to,
            string from,
            string cc,
            string subject,
            string body)
        {
            return SendEmail(
                to,
                from,
                cc,
                subject,
                body,
                MailFormat.Text,
                MailPriority.Normal,
                null,
                false);
        }

        public static string SendEmail( // 6 args
            string to,
            string from,
            string cc,
            string subject,
            string body,
            MailFormat bodyFormat)
        {
            return SendEmail(
                to,
                from,
                cc,
                subject,
                body,
                bodyFormat,
                MailPriority.Normal,
                null,
                false);
        }

        private static string ConvertUploadedBlobToFlatFile(string uploadFolder, int attachmentBpid, Dictionary<string, int> filesToDelete)
        {
            var buffer = new byte[16 * 1024];
            string destPathAndFilename;
            var bpa = Bug.get_bug_post_attachment(attachmentBpid);
            using (bpa.Content)
            {
                destPathAndFilename = Path.Combine(uploadFolder, bpa.File);

                // logic to rename in case of dupes.  MS Outlook embeds images all with the same filename
                var suffix = 0;
                var renamedToPreventDupe = destPathAndFilename;
                while (filesToDelete.ContainsKey(renamedToPreventDupe))
                {
                    suffix++;
                    renamedToPreventDupe = Path.Combine(uploadFolder,
                        Path.GetFileNameWithoutExtension(bpa.File)
                        + Convert.ToString(suffix)
                        + Path.GetExtension(bpa.File));
                }

                destPathAndFilename = renamedToPreventDupe;

                // Save to disk
                using (var outStream = new FileStream(
                    destPathAndFilename,
                    FileMode.CreateNew,
                    FileAccess.Write,
                    FileShare.Delete))
                {
                    var bytesRead = bpa.Content.Read(buffer, 0, buffer.Length);
                    while (bytesRead != 0)
                    {
                        outStream.Write(buffer, 0, bytesRead);

                        bytesRead = bpa.Content.Read(buffer, 0, buffer.Length);
                    }

                    outStream.Close();
                }
            }

            filesToDelete[destPathAndFilename] = 1;

            return destPathAndFilename;
        }

        public static string SendEmail(
            string to,
            string from,
            string cc,
            string subject,
            string body,
            MailFormat bodyFormat,
            MailPriority priority,
            int[] attachmentBpids,
            bool returnReceipt)
        {
            var msg = new MailMessage();

            msg.From = new MailAddress(from);

            AddAddressesToEmail(msg, to, AddrType.To);

            if (!string.IsNullOrEmpty(cc.Trim())) AddAddressesToEmail(msg, cc, AddrType.Cc);

            msg.Subject = subject;

            if (priority == MailPriority.Normal)
                msg.Priority = System.Net.Mail.MailPriority.Normal;
            else if (priority == MailPriority.High)
                msg.Priority = System.Net.Mail.MailPriority.High;
            else
                priority = MailPriority.Low;

            // This fixes a bug for a couple people, but make it configurable, just in case.
            if (ApplicationSettings.BodyEncodingUTF8) msg.BodyEncoding = Encoding.UTF8;

            if (returnReceipt) msg.Headers.Add("Disposition-Notification-To", from);

            // workaround for a bug I don't understand...
            if (ApplicationSettings.SmtpForceReplaceOfBareLineFeeds) body = body.Replace("\n", "\r\n");

            msg.Body = body;
            msg.IsBodyHtml = bodyFormat == MailFormat.Html;

            StuffToDelete stuffToDelete = null;

            if (attachmentBpids != null && attachmentBpids.Length > 0)
            {
                stuffToDelete = new StuffToDelete();

                var uploadFolder = Util.GetUploadFolder();

                if (string.IsNullOrEmpty(uploadFolder))
                {
                    uploadFolder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
                    Directory.CreateDirectory(uploadFolder);
                    stuffToDelete.DirectoriesToDelete.Add(uploadFolder);
                }

                foreach (var attachmentBpid in attachmentBpids)
                {
                    var destPathAndFilename = ConvertUploadedBlobToFlatFile(uploadFolder, attachmentBpid,
                        stuffToDelete.FilesToDelete);

                    // Add saved file as attachment
                    var mailAttachment = new Attachment(
                        destPathAndFilename);

                    msg.Attachments.Add(mailAttachment);
                }
            }

            try
            {
                // This fixes a bug for some people.  Not sure how it happens....
                msg.Body = msg.Body.Replace(Convert.ToChar(0), ' ').Trim();

                var smtpClient = new SmtpClient();

                // SSL or not
                var forceSsl = ApplicationSettings.SmtpForceSsl;

                if (string.IsNullOrEmpty(forceSsl))
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
                    if (forceSsl == "1")
                        smtpClient.EnableSsl = true;
                    else
                        smtpClient.EnableSsl = false;
                }

                // Ignore certificate errors
                if (smtpClient.EnableSsl)
                    ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };

                smtpClient.Send(msg);

                if (stuffToDelete != null)
                {
                    stuffToDelete.Msg = msg;
                    DeleteStuff(stuffToDelete);
                }

                return "";
            }
            catch (Exception e)
            {
                Util.WriteToLog("There was a problem sending email.   Check settings in Web.config.");
                Util.WriteToLog("TO:" + to);
                Util.WriteToLog("FROM:" + from);
                Util.WriteToLog("SUBJECT:" + subject);
                Util.WriteToLog(e.GetBaseException().Message);

                DeleteStuff(stuffToDelete);

                return e.GetBaseException().Message;
            }
        }

        private static void DeleteStuff(StuffToDelete stuffToDelete)
        {
            var thread = new Thread(ThreadProcDeleteStuff);
            thread.Start(stuffToDelete);
        }

        private static void ActuallyDeleteStuff(StuffToDelete stuffToDelete)
        {
            if (stuffToDelete == null) // not sure how this could happen, but it fixed a bug for one guy
                return;

            stuffToDelete.Msg.Dispose(); // if we don't do this, the delete tends not to work.

            foreach (var file in stuffToDelete.FilesToDelete.Keys) File.Delete(file);

            foreach (string directory in stuffToDelete.DirectoriesToDelete) Directory.Delete(directory);
        }

        public static void ThreadProcDeleteStuff(object obj)
        {
            // Allow time for SMTP to be done with these files.
            try
            {
                Thread.Sleep(60 * 1000);
                ActuallyDeleteStuff((StuffToDelete)obj);
            }
            catch (ThreadAbortException)
            {
                ActuallyDeleteStuff((StuffToDelete)obj);
            }
        }

        public static void AddAddressesToEmail(MailMessage msg, string addrs, AddrType addrType)
        {
            Util.WriteToLog("to email addr: " + addrs);

            var separatorChar = ApplicationSettings.EmailAddressSeparatorCharacter;

            var addrArray = addrs.Replace(separatorChar + " ", separatorChar).Split(separatorChar[0]);

            for (var i = 0; i < addrArray.Length; i++)
            {
                var justAddress = SimplifyEmailAddress(addrArray[i]);
                var justDisplayName = addrArray[i].Replace(justAddress, "").Replace("<>", "");
                if (addrType == AddrType.To)
                    msg.To.Add(new MailAddress(justAddress, justDisplayName, Encoding.UTF8));
                else
                    msg.CC.Add(new MailAddress(justAddress, justDisplayName, Encoding.UTF8));
            }
        }

        public static string SimplifyEmailAddress(string email)
        {
            // convert "Corey Trager <ctrager@yahoo.com>" to just "ctrager@yahoo.com"

            var pos1 = email.IndexOf("<");
            var pos2 = email.IndexOf(">");

            if (pos1 >= 0 && pos2 > pos1)
                return email.Substring(pos1 + 1, pos2 - pos1 - 1);
            return email;
        }

        public static void AutoReply(int bugId, string fromAddr, string shortDesc, int projectId)
        {
            var autoReplyText = ApplicationSettings.AutoReplyText;

            if (autoReplyText == "")
            {
                return;
            }

            autoReplyText = autoReplyText.Replace("$BUGID$", Convert.ToString(bugId));

            var sql = new SqlString(@"select
                        pj_pop3_email_from
                        from projects
                        where pj_id = 2pj");

            sql = sql.AddParameterWithValue("pj", projectId);

            var projectEmail = DbUtil.ExecuteScalar(sql);

            if (projectEmail == null)
            {
                Util.WriteToLog("skipping auto reply because project email is blank");
                return;
            }

            var projectEmailString = Convert.ToString(projectEmail);

            if (projectEmailString == "")
            {
                Util.WriteToLog("skipping auto reply because project email is blank");
                return;
            }

            // To avoid an infinite loop of replying to emails and then having to reply to the replies!
            if (projectEmailString.ToLower() == fromAddr.ToLower())
            {
                Util.WriteToLog("skipping auto reply because from address is same as project email:" + projectEmailString);
                return;
            }

            var outgoingSubject = shortDesc + "  ("
                                              + ApplicationSettings.TrackingIdString
                                              + Convert.ToString(bugId) + ")";

            var useHtmlFormat = ApplicationSettings.AutoReplyUseHtmlEmailFormat;

            // commas cause trouble
            var cleanerFromAddr = fromAddr.Replace(",", " ");

            Email.SendEmail(// 4 args
                cleanerFromAddr, // we are responding TO the address we just received email FROM
                projectEmailString,
                string.Empty, // cc
                outgoingSubject,
                autoReplyText,
                useHtmlFormat ? MailFormat.Html : MailFormat.Text);
        }
    }

    public class StuffToDelete
    {
        public ArrayList DirectoriesToDelete = new ArrayList();
        public Dictionary<string, int> FilesToDelete = new Dictionary<string, int>();
        public MailMessage Msg;
    }
}
