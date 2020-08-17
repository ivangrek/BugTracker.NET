namespace BugTracker.MailService
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Data;
    using System.Data.SqlClient;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Timers;
    using System.Xml;
    using OpenPop.Mime;
    using OpenPop.Pop3;
    using Timer = System.Timers.Timer;

    internal sealed class Pop3Main
    {
        private enum ServiceState { Started, Paused, Stopped };

        private ServiceState State = ServiceState.Started;

        public static bool Verbose = true;
        public static string LogFileFolder;
        public static int LogEnabled = 1;
        private bool Suspended = false;

        private static readonly object Dummy = new object();

        private Timer Timer;
        private int FetchIntervalInMinutes = 15;
        private ArrayList Websites;
        private string MessageInputFile;
        private string MessageOutputFile;
        private string ConnectionString;
        private string Pop3Server;
        private string Pop3Port;
        private string Pop3UseSsl;

        private string SubjectMustContain;
        private string SubjectCannotContain;
        private string[] SubjectCannotContainStrings;

        private string FromMustContain;
        private string FromCannotContain;
        private string[] FromCannotContainStrings;

        private string DeleteMessagesOnServer;
        private string InsertBugUrl;
        private string LoginUrl;
        private string ServiceUsername;
        private string ServicePassword;
        private string TrackingIdString;
        private int TotalErrorsAllowed = 999999;
        private int TotalErrorCount;
        private int ReadInputStreamCharByChar;
        private int EnableWatchdogThread = 1;
        private int RespawnFetchingThreadAfterNSecondsOfInactivity = 60 * 60 * 2; // 6 hours

        private static readonly Regex RePipes = new Regex("\\|");

        private Thread fetchingThread;
        private readonly Thread watchDogThread;

        public static DateTime HeartbeatDatetime = DateTime.Now;

        public Pop3Main(bool verbose)
        {
            Thread watchdogThread;
            var thisExe = Assembly.GetExecutingAssembly().Location;

            LogFileFolder = Path.GetDirectoryName(thisExe);

            Verbose = verbose;

            //ServicePointManager.CertificatePolicy = new AcceptAllCertificatePolicy();

            GetSettings();
            WriteToLog("creating");

            fetchingThread = new Thread(FetchingThreadProc);
            fetchingThread.Start();

            if (EnableWatchdogThread == 1)
            {
                watchDogThread = new Thread(WatchdogThreadProc);
                watchDogThread.Start();
            }
        }

        public void Start()
        {
            // call DoWork()
            WriteToLog("starting");
            State = ServiceState.Started;
        }

        public void Pause()
        {
            WriteToLog("pausing");
            State = ServiceState.Paused;
        }

        public void Stop()
        {
            WriteToLog("stopping");
            State = ServiceState.Stopped;
        }

        private void FetchingThreadProc()
        {
            WriteToLog("entering fetching thread");

            DoWork(null, null);

            while (true)
            {
                Thread.Sleep(2000);

                if (State == ServiceState.Stopped)
                {
                    Timer.Enabled = false;
                    break;
                }
            }

            WriteToLog("exiting fetching thread");
        }

        private void WatchdogThreadProc()
        {
            WriteToLog("entering watchdog thread");

            while (true)
            {
                Thread.Sleep(2000);

                if (State == ServiceState.Stopped)
                {
                    break;
                }

                var timespan = DateTime.Now.Subtract(HeartbeatDatetime);

                if (timespan.TotalSeconds > RespawnFetchingThreadAfterNSecondsOfInactivity)
                {
                    WriteToLog("WARNING - watchdog thread is killing fetching thread");
                    fetchingThread.Abort();
                    fetchingThread = new Thread(FetchingThreadProc);
                    WriteToLog("WARNING - watchdog thread is starting new fetching thread");
                    fetchingThread.Start();
                }
            }

            WriteToLog("exiting watchdog thread");
        }

        private void DoWork(object source, ElapsedEventArgs eea)
        {
            HeartbeatDatetime = DateTime.Now;
            WriteToLog($"doing work, updating heartbeat to {HeartbeatDatetime:yyyy-MM-dd h:mm tt}");

            if (State != ServiceState.Started)
            {
                WriteToLog("not in STARTED state");
            }
            else
            {
                GetSettings();

                for (var i = 0; i < Websites.Count; i++)
                {
                    if (State != ServiceState.Started)
                    {
                        break;
                    }

                    var settings = (StringDictionary)Websites[i];

                    MessageInputFile = settings["MessageInputFile"];
                    MessageOutputFile = settings["MessageOutputFile"];
                    ConnectionString = settings["ConnectionString"];
                    Pop3Server = settings["Pop3Server"];
                    Pop3Port = settings["Pop3Port"];
                    Pop3UseSsl = settings["Pop3UseSSL"];
                    SubjectMustContain = settings["SubjectMustContain"];

                    SubjectCannotContain = settings["SubjectCannotContain"];
                    SubjectCannotContainStrings = RePipes.Split(SubjectCannotContain);

                    FromMustContain = settings["FromMustContain"];

                    FromCannotContain = settings["FromCannotContain"];
                    FromCannotContainStrings = RePipes.Split(FromCannotContain);

                    DeleteMessagesOnServer = settings["DeleteMessagesOnServer"];
                    LoginUrl = settings["LoginUrl"];
                    InsertBugUrl = settings["InsertBugUrl"];
                    ServiceUsername = settings["ServiceUsername"];
                    ServicePassword = settings["ServicePassword"];
                    TrackingIdString = settings["TrackingIdString"];

                    WriteToLog($"*** fetching messages for website {i + 1} {InsertBugUrl}");

                    FetchMessagesForProjects();
                }
            }

            Resume(); // reset the timer
        }

        private void Resume()
        {
            // Set up a timer so that we keep fetching messages
            if (Timer != null)
            {
                Timer.Stop();
                Timer.Dispose();
            }

            Timer = new Timer
            {
                AutoReset = false
            };

            Timer.Elapsed += DoWork;

            // Set the timer interval
            Timer.Interval = 60 * 1000 * FetchIntervalInMinutes;
            Timer.Enabled = true;
        }

        private void GetSettings()
        {
            WriteToLog("GetSettings");

            Websites = new ArrayList();
            StringDictionary settings = null;

            var configuration = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
            var section = configuration.GetSection("btnetSettings");

            try
            {
                using (var stringReader = new StringReader(section.SectionInformation.GetRawXml()))
                using (var xmlReader = new XmlTextReader(stringReader))
                {
                    while (xmlReader.Read())
                    {
                        //continue;
                        if (xmlReader.Name == "add")
                        {
                            var key = xmlReader["key"];

                            if (key == "FetchIntervalInMinutes")
                            {
                                WriteToLog(key + "=" + xmlReader["value"]);
                                FetchIntervalInMinutes = Convert.ToInt32(xmlReader["value"]);
                            }
                            else if (key == "TotalErrorsAllowed")
                            {
                                WriteToLog(key + "=" + xmlReader["value"]);
                                TotalErrorsAllowed = Convert.ToInt32(xmlReader["value"]);
                            }
                            else if (key == "ReadInputStreamCharByChar")
                            {
                                WriteToLog(key + "=" + xmlReader["value"]);
                                ReadInputStreamCharByChar = Convert.ToInt32(xmlReader["value"]);
                            }
                            else if (key == "LogFileFolder")
                            {
                                WriteToLog(key + "=" + xmlReader["value"]);
                                LogFileFolder = Convert.ToString(xmlReader["value"]);
                            }
                            else if (key == "LogEnabled")
                            {
                                WriteToLog(key + "=" + xmlReader["value"]);
                                LogEnabled = Convert.ToInt32(xmlReader["value"]);
                            }
                            else if (key == "EnableWatchdogThread")
                            {
                                WriteToLog(key + "=" + xmlReader["value"]);
                                EnableWatchdogThread = Convert.ToInt32(xmlReader["value"]);
                            }
                            else if (key == "RespawnFetchingThreadAfterNSecondsOfInactivity")
                            {
                                WriteToLog(key + "=" + xmlReader["value"]);
                                RespawnFetchingThreadAfterNSecondsOfInactivity = Convert.ToInt32(xmlReader["value"]);
                            }
                            else
                            {
                                if (key == "ConnectionString"
                                    || key == "Pop3Server"
                                    || key == "Pop3Port"
                                    || key == "Pop3UseSSL"
                                    || key == "SubjectMustContain"
                                    || key == "SubjectCannotContain"
                                    || key == "FromMustContain"
                                    || key == "FromCannotContain"
                                    || key == "DeleteMessagesOnServer"
                                    || key == "FetchIntervalInMinutes"
                                    || key == "InsertBugUrl"
                                    || key == "ServiceUsername"
                                    || key == "ServicePassword"
                                    || key == "TrackingIdString"
                                    || key == "MessageInputFile"
                                    || key == "MessageOutputFile")
                                {
                                    WriteToLog(key + "=" + xmlReader["value"]);
                                }

                                if (settings != null)
                                {
                                    settings[key] = xmlReader["value"];
                                }
                            }
                            // else an uninteresting setting
                        }
                        else
                        {
                            // create a new dictionary of settings each time we encounter a new Website section
                            if (xmlReader.Name.ToLower() == "website" && xmlReader.NodeType == XmlNodeType.Element)
                            {
                                settings = new StringDictionary
                                {
                                    ["MessageInputFile"] = string.Empty,
                                    ["MessageOutputFile"] = string.Empty,
                                    ["ConnectionString"] = string.Empty,
                                    ["Pop3Server"] = string.Empty,
                                    ["Pop3Port"] = string.Empty,
                                    ["Pop3UseSSL"] = string.Empty,
                                    ["SubjectMustContain"] = string.Empty,
                                    ["SubjectCannotContain"] = string.Empty,
                                    ["FromMustContain"] = string.Empty,
                                    ["FromCannotContain"] = string.Empty,
                                    ["DeleteMessagesOnServer"] = string.Empty,
                                    ["InsertBugUrl"] = string.Empty,
                                    ["ServiceUsername"] = string.Empty,
                                    ["ServicePassword"] = string.Empty,
                                    ["TrackingIdString"] = string.Empty
                                };

                                Websites.Add(settings);
                                WriteToLog($"*** loading settings for website {Websites.Count}");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                WriteToLog($"Error trying to read configuration: {configuration.FilePath}");
                WriteToLog(e.ToString());
            }
        }

        private void FetchMessagesForProjects()
        {
            // Get the list of accounts to read

            try
            {
                var sql = @"select
                pj_id, pj_pop3_username, pj_pop3_password
                from projects
                where pj_enable_pop3 = 1";

                var ds = GetDataSet(sql);

                foreach (DataRow dr in ds.Tables[0].Rows)
                {
                    if (State != ServiceState.Started)
                    {
                        break;
                    }

                    WriteToLog("processing project " + Convert.ToString(dr["pj_id"]) + " using account " + dr["pj_pop3_username"]);

                    FetchMessages(
                        (string)dr["pj_pop3_username"],
                        (string)dr["pj_pop3_password"],
                        (int)dr["pj_id"]);
                }
            }
            catch (Exception e)
            {
                WriteToLog("Error trying to process messages");
                WriteToLog(e.ToString());
            }
        }

        private async void FetchMessages(string user, string password, int projectId)
        {
            var regex = new Regex("\r\n");

            using (var client = new Pop3Client())
            {
                List<string> messages = null;
                var messageCount = 0;

                try
                {
                    WriteToLog("****connecting to server:");

                    var port = 110;

                    if (Pop3Port != string.Empty)
                    {
                        port = Convert.ToInt32(Pop3Port);
                    }

                    var useSsl = false;

                    if (Pop3UseSsl != string.Empty)
                    {
                        useSsl = Pop3UseSsl == "1";
                    }

                    WriteToLog("Connecting to pop3 server");
                    client.Connect(Pop3Server, port, useSsl);
                    WriteToLog("Autenticating");
                    client.Authenticate(user, password);

                    //WriteToLog("Getting list of documents");
                    //messages = client.GetMessageUids();
                    messageCount = client.GetMessageCount();

                    WriteToLog($"Found {messageCount} messages");
                }
                catch (Exception e)
                {
                    WriteToLog("Exception trying to talk to pop3 server");
                    WriteToLog(e.ToString());
                    return;
                }

                var messageNumber = 0;

                // loop through the messages
                for (var i = 0; i < messageCount/*messages.Count*/; i++)
                {
                    HeartbeatDatetime = DateTime.Now; // because the watchdog is watching

                    if (State != ServiceState.Started)
                    {
                        break;
                    }

                    // fetch the message
                    //WriteToLog("Getting Message:" + messages[i]);
                    //messageNumber = Convert.ToInt32(messages[i]);
                    messageNumber = i + 1;
                    Message mimeMessage = null;

                    try
                    {
                        mimeMessage = client.GetMessage(messageNumber);
                    }
                    catch (Exception exception)
                    {
                        WriteToLog("Error getting message");
                        WriteToLog(exception.ToString());
                        continue;
                    }

                    // for diagnosing problems
                    if (MessageOutputFile != string.Empty)
                    {
                        File.WriteAllBytes(MessageOutputFile, mimeMessage.RawMessage);
                    }

                    // break the message up into lines

                    var from = mimeMessage.Headers.From.Address;
                    var subject = mimeMessage.Headers.Subject;

                    WriteToLog("\nFrom: " + from);
                    WriteToLog("Subject: " + subject);

                    if (!string.IsNullOrEmpty(SubjectMustContain) && subject.IndexOf(SubjectMustContain, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        WriteToLog("skipping because subject does not contain: " + SubjectMustContain);
                        continue;
                    }

                    var bSkip = false;

                    foreach (var subjectCannotContainString in SubjectCannotContainStrings)
                    {
                        if (!string.IsNullOrEmpty(subjectCannotContainString))
                        {
                            if (subject.IndexOf(subjectCannotContainString, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                WriteToLog("skipping because subject cannot contain: " + subjectCannotContainString);
                                bSkip = true;
                                break;  // done checking, skip this message
                            }
                        }
                    }

                    if (bSkip)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(FromMustContain) && from.IndexOf(FromMustContain, StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        WriteToLog("skipping because from does not contain: " + FromMustContain);
                        continue; // that is, skip to next message
                    }

                    foreach (var fromCannotContainStrings in FromCannotContainStrings)
                    {
                        if (!string.IsNullOrEmpty(fromCannotContainStrings))
                        {
                            if (from.IndexOf(fromCannotContainStrings, StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                WriteToLog("skipping because from cannot contain: " + fromCannotContainStrings);
                                bSkip = true;
                                break; // done checking, skip this message
                            }
                        }
                    }

                    if (bSkip)
                    {
                        continue;
                    }

                    WriteToLog($"calling {InsertBugUrl}");

                    var useBugId = false;

                    // Try to parse out the bugid from the subject line
                    var bugidString = TrackingIdString;

                    if (string.IsNullOrEmpty(TrackingIdString))
                    {
                        bugidString = "DO NOT EDIT THIS:";
                    }

                    var pos = subject.IndexOf(bugidString, StringComparison.OrdinalIgnoreCase);

                    if (pos >= 0)
                    {
                        // position of colon
                        pos = subject.IndexOf(":", pos);
                        pos++;
                        // position of close paren
                        var pos2 = subject.IndexOf(")", pos);

                        if (pos2 > pos)
                        {
                            var bugIdString = subject.Substring(pos, pos2 - pos);

                            WriteToLog($"BUGID={bugIdString}");

                            try
                            {
                                var bugid = Int32.Parse(bugIdString);
                                useBugId = true;
                                WriteToLog("updating existing bug " + Convert.ToString(bugid));
                            }
                            catch (Exception e)
                            {
                                WriteToLog("bugid not numeric " + e.Message);
                            }
                        }
                    }

                    // send request to web server
                    try
                    {
                        var handler = new HttpClientHandler
                        {
                            AllowAutoRedirect = true,
                            UseCookies = true,
                            CookieContainer = new CookieContainer()
                        };

                        using (var httpClient = new HttpClient(handler))
                        {
                            var loginParameters = new Dictionary<string, string>
                            {
                                { "Login", ServiceUsername },
                                { "Password", ServicePassword }
                            };

                            var loginContent = new FormUrlEncodedContent(loginParameters);
                            var loginResponse = await httpClient.PostAsync(LoginUrl, loginContent);

                            loginResponse.EnsureSuccessStatusCode();

                            var rawMessage = Encoding.Default.GetString(mimeMessage.RawMessage);
                            var postBugParameters = new Dictionary<string, string>
                            {
                                { "projectId", Convert.ToString(projectId) },
                                { "fromAddress", from },
                                { "shortDescription", subject},
                                { "message", rawMessage}
                                //Any other paramters go here
                            };

                            if (useBugId)
                            {
                                postBugParameters.Add("bugId", bugidString);
                            }

                            HttpContent bugContent = new FormUrlEncodedContent(postBugParameters);
                            var postBugResponse = await httpClient.PostAsync(InsertBugUrl, bugContent);

                            postBugResponse.EnsureSuccessStatusCode();
                        }

                        if (MessageInputFile == string.Empty && DeleteMessagesOnServer == "1")
                        {
                            WriteToLog("sending POP3 command DELE");
                            client.DeleteMessage(messageNumber);
                        }
                    }
                    catch (Exception e)
                    {
                        WriteToLog("HttpWebRequest error url=" + InsertBugUrl);
                        WriteToLog(e.ToString());
                        WriteToLog("Incrementing total error count");
                        TotalErrorCount++;
                    }

                    // examine response
                    if (TotalErrorCount > TotalErrorsAllowed)
                    {
                        WriteToLog("Stopping because total error count > TotalErrorsAllowed");
                        Stop();
                    }
                }  // end for each message

                if (MessageInputFile == string.Empty)
                {
                    WriteToLog("\nsending POP3 command QUIT");
                    client.Disconnect();
                }
                else
                {
                    WriteToLog("\nclosing input file " + MessageInputFile);
                }
            }
        }

        private DataSet GetDataSet(string sql)
        {
            var ds = new DataSet();

            using (var conn = new SqlConnection(ConnectionString))
            {
                conn.Open();

                using (var da = new SqlDataAdapter(sql, conn))
                {
                    da.Fill(ds);
                }
            }

            return ds;
        }

        private static string GetLogFilePath()
        {
            var now = DateTime.Now;
            var nowString = $"{now.Year}_{now.Month:0#}_{now.Day:0#}";

            return Path.Combine(LogFileFolder, $"btnet_service_log_{nowString}.txt");

        }

        private static void WriteToLog(string message)
        {
            message = $"{DateTime.Now.ToLongTimeString()} {message}";

            if (LogEnabled == 1)
            {
                var path = GetLogFilePath();

                lock (Dummy)
                {
                    using (var w = File.AppendText(path))
                    {
                        w.WriteLine(message);
                    }
                }
            }

            if (Verbose)
            {
                Console.WriteLine(message);
            }
        }
    }

    internal class AcceptAllCertificatePolicy : ICertificatePolicy
    {
        public bool CheckValidationResult(ServicePoint servicePoint, X509Certificate cert, WebRequest webRequest, int certificateProblem)
        {
            // Always accept
            return true;
        }
    }
}