/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core
{
    using System;
    using System.Data;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Web;

    public static class MyPop3
    {
        private static IApplicationSettings ApplicationSettings { get; set; } = new ApplicationSettings();

        public static int ErrorCount = 0;
        public static string Pop3Server = ApplicationSettings.Pop3Server;
        public static int Pop3Port = ApplicationSettings.Pop3Port;
        public static bool Pop3UseSsl = ApplicationSettings.Pop3UseSSL;
        public static string Pop3ServiceUsername = ApplicationSettings.Pop3ServiceUsername;
        public static int Pop3TotalErrorsAllowed = ApplicationSettings.Pop3TotalErrorsAllowed;

        public static bool Pop3ReadInputStreamCharByChar = ApplicationSettings.Pop3ReadInputStreamCharByChar;

        public static string Pop3SubjectMustContain = ApplicationSettings.Pop3SubjectMustContain;
        public static string Pop3SubjectCannotContain = ApplicationSettings.Pop3SubjectCannotContain;
        public static string Pop3FromMustContain = ApplicationSettings.Pop3FromMustContain;
        public static string Pop3FromCannotContain = ApplicationSettings.Pop3FromCannotContain;

        public static bool Pop3DeleteMessagesOnServer = ApplicationSettings.Pop3DeleteMessagesOnServer;
        public static bool Pop3WriteRawMessagesToLog = ApplicationSettings.Pop3WriteRawMessagesToLog;

        //*************************************************************
        public static void StartPop3(HttpApplicationState app)
        {
            var thread = new Thread(ThreadProcPop3);
            thread.Start(app);
        }

        //*************************************************************

        public static bool FetchMessages(string projectUser, string projectPassword, int projectid)
        {
            // experimental, under construction

            var client = new Pop3Client(Pop3ReadInputStreamCharByChar);

            var subjectCannotContainStrings = Util.RePipes.Split(Pop3SubjectCannotContain);
            var fromCannotContainStrings = Util.RePipes.Split(Pop3FromCannotContain);

            //try
            {
                var defaults = Bug.GetBugDefaults();

                //int projectid = (int)defaults["pj"];
                var categoryid = (int) defaults["ct"];
                var priorityid = (int) defaults["pr"];
                var statusid = (int) defaults["st"];
                var udfid = (int) defaults["udf"];

                Util.WriteToLog("pop3:" + client.Connect(Pop3Server, Pop3Port, Pop3UseSsl));

                Util.WriteToLog("pop3:sending POP3 command USER");
                Util.WriteToLog("pop3:" + client.User(projectUser));

                Util.WriteToLog("pop3:sending POP3 command PASS");
                Util.WriteToLog("pop3:" + client.Pass(projectPassword));

                Util.WriteToLog("pop3:sending POP3 command STAT");
                Util.WriteToLog("pop3:" + client.Stat());

                Util.WriteToLog("pop3:sending POP3 command LIST");
                string list;
                list = client.List();
                Util.WriteToLog("pop3:list follows:");
                Util.WriteToLog(list);

                string[] messages = null;
                var regex = new Regex("\r\n");
                messages = regex.Split(list);

                var end = messages.Length - 1;

                // loop through the messages
                for (var i = 1; i < end; i++)
                {
                    var spacePos = messages[i].IndexOf(" ");
                    var messageNumber = Convert.ToInt32(messages[i].Substring(0, spacePos));
                    var messageRawString = client.Retr(messageNumber);

                    if (Pop3WriteRawMessagesToLog)
                    {
                        Util.WriteToLog("raw email message:");
                        Util.WriteToLog(messageRawString);
                    }

                    var mimeMessage = MyMime.GetSharpMimeMessage(messageRawString);

                    var fromAddr = MyMime.GetFromAddr(mimeMessage);
                    var subject = MyMime.GetSubject(mimeMessage);

                    if (!string.IsNullOrEmpty(Pop3SubjectMustContain) && subject.IndexOf(Pop3SubjectMustContain) < 0)
                    {
                        Util.WriteToLog("skipping because subject does not contain: " + Pop3SubjectMustContain);
                        continue;
                    }

                    var bSkip = false;

                    for (var k = 0; k < subjectCannotContainStrings.Length; k++)
                        if (!string.IsNullOrEmpty(subjectCannotContainStrings[k]))
                            if (subject.IndexOf(subjectCannotContainStrings[k]) >= 0)
                            {
                                Util.WriteToLog("skipping because subject cannot contain: " +
                                                  subjectCannotContainStrings[k]);
                                bSkip = true;
                                break; // done checking, skip this message
                            }

                    if (bSkip) continue;

                    if (!string.IsNullOrEmpty(Pop3FromMustContain) && fromAddr.IndexOf(Pop3FromMustContain) < 0)
                    {
                        Util.WriteToLog("skipping because from does not contain: " + Pop3FromMustContain);
                        continue; // that is, skip to next message
                    }

                    for (var k = 0; k < fromCannotContainStrings.Length; k++)
                        if (!string.IsNullOrEmpty(fromCannotContainStrings[k]))
                            if (fromAddr.IndexOf(fromCannotContainStrings[k]) >= 0)
                            {
                                Util.WriteToLog(
                                    "skipping because from cannot contain: " + fromCannotContainStrings[k]);
                                bSkip = true;
                                break; // done checking, skip this message
                            }

                    if (bSkip) continue;

                    var bugid = MyMime.GetBugidFromSubject(ref subject);
                    var cc = MyMime.GetCc(mimeMessage);
                    var comment = MyMime.GetComment(mimeMessage);
                    var headers = MyMime.GetHeadersForComment(mimeMessage);
                    if (!string.IsNullOrEmpty(headers)) comment = headers + "\n" + comment;

                    var security = MyMime.GetSynthesizedSecurity(mimeMessage, fromAddr, Pop3ServiceUsername);
                    var orgid = security.User.Org;

                    if (bugid == 0)
                    {
                        if (security.User.ForcedProject != 0) projectid = security.User.ForcedProject;

                        if (subject.Length > 200) subject = subject.Substring(0, 200);

                        var newIds = Bug.InsertBug(
                            subject,
                            security,
                            "", // tags
                            projectid,
                            orgid,
                            categoryid,
                            priorityid,
                            statusid,
                            0, // assignedid,
                            udfid,
                            "", "", "", // project specific dropdown values
                            comment,
                            comment,
                            fromAddr,
                            cc,
                            "text/plain",
                            false, // internal only
                            null, // custom columns
                            false);

                        MyMime.AddAttachments(mimeMessage, newIds.Bugid, newIds.Postid, security);

                        // your customizations
                        Bug.ApplyPostInsertRules(newIds.Bugid);

                        Bug.SendNotifications(Bug.Insert, newIds.Bugid, security);
                        WhatsNew.AddNews(newIds.Bugid, subject, "added", security);

                        AutoReply(newIds.Bugid, fromAddr, subject, projectid);
                    }
                    else // update existing
                    {
                        var statusResultingFromIncomingEmail =
                            ApplicationSettings.StatusResultingFromIncomingEmail;

                        var sql = string.Empty;

                        if (statusResultingFromIncomingEmail != 0)
                        {
                            sql = @"update bugs
                                set bg_status = $st
                                where bg_id = $bg
                                ";

                            sql = sql.Replace("$st", statusResultingFromIncomingEmail.ToString());
                        }

                        sql += "select bg_short_desc from bugs where bg_id = $bg";
                        sql = sql.Replace("$bg", Convert.ToString(bugid));
                        var dr2 = DbUtil.GetDataRow(sql);

                        // Add a comment to existing bug.
                        var postid = Bug.InsertComment(
                            bugid,
                            security.User.Usid, // (int) dr["us_id"],
                            comment,
                            comment,
                            fromAddr,
                            cc,
                            "text/plain",
                            false); // internal only

                        MyMime.AddAttachments(mimeMessage, bugid, postid, security);
                        Bug.SendNotifications(Bug.Update, bugid, security);
                        WhatsNew.AddNews(bugid, (string) dr2["bg_short_desc"], "updated", security);
                    }

                    if (Pop3DeleteMessagesOnServer)
                    {
                        Util.WriteToLog("sending POP3 command DELE");
                        Util.WriteToLog(client.Dele(messageNumber));
                    }
                }
            }
            //catch (Exception ex)
            //{
            //    btnet.Util.WriteToLog("pop3:exception in fetch_messages: " + ex.Message);
            //    error_count++;
            //    if (error_count > Pop3TotalErrorsAllowed)
            //    {
            //        return false;
            //    }
            //}

            Util.WriteToLog("pop3:quit");
            Util.WriteToLog("pop3:" + client.Quit());
            return true;
        }

        //*************************************************************
        private static void ThreadProcPop3(object obj)
        {
            //System.Web.HttpApplication app = (System.Web.HttpApplication)obj;

            while (true)
            {
                var pop3FetchIntervalInMinutes = ApplicationSettings.Pop3FetchIntervalInMinutes;

                try
                {
                    // get all the projects that have been associated with pop3 usernames 
                    var sql =
                        @"select pj_id, pj_pop3_username, pj_pop3_password	from projects where pj_enable_pop3 = 1";

                    var ds = DbUtil.GetDataSet(sql);
                    foreach (DataRow dr in ds.Tables[0].Rows)
                    {
                        Util.WriteToLog("pop3:processing project " + Convert.ToString(dr["pj_id"]) +
                                          " using account " + dr["pj_pop3_username"]);

                        var result = FetchMessages(
                            (string) dr["pj_pop3_username"],
                            (string) dr["pj_pop3_password"],
                            (int) dr["pj_id"]);

                        if (!result)
                        {
                            Util.WriteToLog("pop3:exiting thread because error count has reached the limit");
                            return;
                        }
                    }
                }
                catch (Exception e)
                {
                    Util.WriteToLog("pop3:exception in threadproc_pop3:");
                    Util.WriteToLog(e.Message);
                    Util.WriteToLog(e.StackTrace);
                    return;
                }

                Thread.Sleep(pop3FetchIntervalInMinutes * 60 * 1000);
            }
        }

        //*************************************************************
        public static void AutoReply(int bugid, string fromAddr, string shortDesc, int projectid)
        {
            var autoReplyText = ApplicationSettings.AutoReplyText;
            if (string.IsNullOrEmpty(autoReplyText))
                return;

            autoReplyText = autoReplyText.Replace("$BUGID$", Convert.ToString(bugid));

            var sql = @"select
                        pj_pop3_email_from
                        from projects
                        where pj_id = $pj";

            sql = sql.Replace("$pj", Convert.ToString(projectid));

            var projectEmail = DbUtil.ExecuteScalar(sql);

            if (projectEmail == null)
            {
                Util.WriteToLog("skipping auto reply because project email is blank");
                return;
            }

            var projectEmailString = Convert.ToString(projectEmail);

            if (string.IsNullOrEmpty(projectEmailString))
            {
                Util.WriteToLog("skipping auto reply because project email is blank");
                return;
            }

            // To avoid an infinite loop of replying to emails and then having to reply to the replies!
            if (projectEmailString.ToLower() == fromAddr.ToLower())
            {
                Util.WriteToLog("skipping auto reply because from address is same as project email:" +
                                  projectEmailString);
                return;
            }

            var outgoingSubject = shortDesc + "  ("
                                              + ApplicationSettings.TrackingIdString
                                              + Convert.ToString(bugid) + ")";

            var useHtmlFormat = ApplicationSettings.AutoReplyUseHtmlEmailFormat;

            // commas cause trouble
            var cleanerFromAddr = fromAddr.Replace(",", " ");

            Email.SendEmail( // 4 args
                cleanerFromAddr, // we are responding TO the address we just received email FROM
                projectEmailString,
                "", // cc
                outgoingSubject,
                autoReplyText,
                useHtmlFormat ? BtnetMailFormat.Html : BtnetMailFormat.Text);
        }
    }
}