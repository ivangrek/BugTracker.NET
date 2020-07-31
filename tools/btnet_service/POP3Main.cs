using System;
using System.Collections;
using System.Collections.Specialized;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Net;
using System.Xml;
using POP3Client;
using Timer = System.Timers.Timer;

//using anmar.SharpMimeTools;

public class POP3Main
{
    public static bool verbose = true;
    public static string LogFileFolder;
    public static int LogEnabled = 1;

    private static readonly object dummy = new object();

    private static readonly Regex rePipes = new Regex("\\|");

    public static DateTime heartbeat_datetime = DateTime.Now;
    private readonly Thread watchdog_thread;

    protected string config_file;
    protected string ConnectionString;

    protected string DeleteMessagesOnServer;
    protected int EnableWatchdogThread = 1;

    private Thread fetching_thread;
    protected int FetchIntervalInMinutes = 15;
    protected string FromCannotContain;
    protected string[] FromCannotContainStrings;

    protected string FromMustContain;
    protected string InsertBugUrl;
    protected string MessageInputFile;
    protected string MessageOutputFile;
    protected string Pop3Port;
    protected string Pop3Server;
    protected string Pop3UseSSL;
    protected int ReadInputStreamCharByChar;
    protected int RespawnFetchingThreadAfterNSecondsOfInactivity = 60 * 60 * 2; // 6 hours
    protected string ServicePassword;
    protected string ServiceUsername;
    protected service_state state = service_state.STARTED;
    protected string SubjectCannotContain;
    protected string[] SubjectCannotContainStrings;

    protected string SubjectMustContain;
    protected bool suspended = false;

    protected Timer timer;
    protected int total_error_count;
    protected int TotalErrorsAllowed = 999999;
    protected string TrackingIdString;
    protected ArrayList websites;

    ///////////////////////////////////////////////////////////////////
    public POP3Main(string config_file, bool verbose)
    {
        var this_exe = Process.GetCurrentProcess().MainModule.FileName;
        LogFileFolder = Path.GetDirectoryName(this_exe);

        this.config_file = config_file;
        POP3Main.verbose = verbose;

        ServicePointManager.CertificatePolicy = new AcceptAllCertificatePolicy();

        get_settings();
        write_line("creating");

        this.fetching_thread = new Thread(fetching_thread_proc);
        this.fetching_thread.Start();

        if (this.EnableWatchdogThread == 1)
        {
            this.watchdog_thread = new Thread(watchdog_thread_proc);
            this.watchdog_thread.Start();
        }
    }

    ///////////////////////////////////////////////////////////////////
    public void start()
    {
        // call do_work()
        write_line("starting");
        this.state = service_state.STARTED;
    }

    ///////////////////////////////////////////////////////////////////
    public void pause()
    {
        write_line("pausing");
        this.state = service_state.PAUSED;
    }

    ///////////////////////////////////////////////////////////////////
    public void stop()
    {
        write_line("stopping");
        this.state = service_state.STOPPED;
    }


    ///////////////////////////////////////////////////////////////////////
    public static string get_log_file_path()
    {
        // determine log file name

        var now = DateTime.Now;
        var now_string =
            now.Year
            + "_" +
            now.Month.ToString("0#")
            + "_" +
            now.Day.ToString("0#");

        var path = LogFileFolder
                   + "\\"
                   + "btnet_service_log_"
                   + now_string
                   + ".txt";

        return path;
    }

    ///////////////////////////////////////////////////////////////////////
    public static void write_to_log(string s)
    {
        var path = get_log_file_path();

        lock (dummy)
        {
            var w = File.AppendText(path);

            w.WriteLine(DateTime.Now.ToLongTimeString()
                        + " "
                        + s);

            w.Close();
        }
    }

    ///////////////////////////////////////////////////////////////////
    public static void write_line(object o)
    {
        if (LogEnabled == 1) write_to_log(Convert.ToString(o));

        if (verbose) Console.WriteLine(o);
    }


    ///////////////////////////////////////////////////////////////////
    public void fetching_thread_proc()
    {
        write_line("entering fetching thread");

        do_work(null, null);

        while (true)
        {
            Thread.Sleep(2000);
            if (this.state == service_state.STOPPED)
            {
                this.timer.Enabled = false;
                break;
            }
        }

        write_line("exiting fetching thread");
    }


    ///////////////////////////////////////////////////////////////////
    public void watchdog_thread_proc()
    {
        write_line("entering watchdog thread");

        while (true)
        {
            Thread.Sleep(2000);

            if (this.state == service_state.STOPPED) break;

            var timespan = DateTime.Now.Subtract(heartbeat_datetime);

            if (timespan.TotalSeconds > this.RespawnFetchingThreadAfterNSecondsOfInactivity)
            {
                write_line("WARNING - watchdog thread is killing fetching thread");
                this.fetching_thread.Abort();
                this.fetching_thread = new Thread(fetching_thread_proc);
                write_line("WARNING - watchdog thread is starting new fetching thread");
                this.fetching_thread.Start();
            }
        }

        write_line("exiting watchdog thread");
    }


    ///////////////////////////////////////////////////////////////////
    public void do_work(object source, ElapsedEventArgs eea)
    {
        heartbeat_datetime = DateTime.Now;
        write_line("doing work, updating heartbeat to " + heartbeat_datetime.ToString("yyyy-MM-dd h:mm tt"));

        if (this.state != service_state.STARTED)
        {
            write_line("not in STARTED state");
        }
        else
        {
            get_settings();


            for (var i = 0; i < this.websites.Count; i++)
            {
                if (this.state != service_state.STARTED) break;

                var settings = (StringDictionary) this.websites[i];

                this.MessageInputFile = settings["MessageInputFile"];
                this.MessageOutputFile = settings["MessageOutputFile"];
                this.ConnectionString = settings["ConnectionString"];
                this.Pop3Server = settings["Pop3Server"];
                this.Pop3Port = settings["Pop3Port"];
                this.Pop3UseSSL = settings["Pop3UseSSL"];
                this.SubjectMustContain = settings["SubjectMustContain"];

                this.SubjectCannotContain = settings["SubjectCannotContain"];
                this.SubjectCannotContainStrings = rePipes.Split(this.SubjectCannotContain);

                this.FromMustContain = settings["FromMustContain"];

                this.FromCannotContain = settings["FromCannotContain"];
                this.FromCannotContainStrings = rePipes.Split(this.FromCannotContain);

                this.DeleteMessagesOnServer = settings["DeleteMessagesOnServer"];
                this.InsertBugUrl = settings["InsertBugUrl"];
                this.ServiceUsername = settings["ServiceUsername"];
                this.ServicePassword = settings["ServicePassword"];
                this.TrackingIdString = settings["TrackingIdString"];

                write_line("*** fetching messages for website " + Convert.ToString(i + 1) + " " + this.InsertBugUrl);

                fetch_messages_for_projects();
            }
        }

        resume(); // reset the timer
    }


    ///////////////////////////////////////////////////////////////////
    public void resume()
    {
        // Set up a timer so that we keep fetching messages
        if (this.timer != null)
        {
            this.timer.Stop();
            this.timer.Dispose();
        }

        this.timer = new Timer();
        this.timer.AutoReset = false;
        this.timer.Elapsed += do_work;

        // Set the timer interval
        this.timer.Interval = 60 * 1000 * this.FetchIntervalInMinutes;
        this.timer.Enabled = true;
    }

    ///////////////////////////////////////////////////////////////////
    protected void get_settings()
    {
        write_line("get_settings");

        this.websites = new ArrayList();
        StringDictionary settings = null;

        var filename = this.config_file;
        XmlTextReader tr = null;

        try
        {
            tr = new XmlTextReader(filename);
            while (tr.Read())
                //continue;
                if (tr.Name == "add")
                {
                    var key = tr["key"];

                    if (key == "FetchIntervalInMinutes")
                    {
                        write_line(key + "=" + tr["value"]);
                        this.FetchIntervalInMinutes = Convert.ToInt32(tr["value"]);
                    }
                    else if (key == "TotalErrorsAllowed")
                    {
                        write_line(key + "=" + tr["value"]);
                        this.TotalErrorsAllowed = Convert.ToInt32(tr["value"]);
                    }
                    else if (key == "ReadInputStreamCharByChar")
                    {
                        write_line(key + "=" + tr["value"]);
                        this.ReadInputStreamCharByChar = Convert.ToInt32(tr["value"]);
                    }
                    else if (key == "LogFileFolder")
                    {
                        write_line(key + "=" + tr["value"]);
                        LogFileFolder = Convert.ToString(tr["value"]);
                    }
                    else if (key == "LogEnabled")
                    {
                        write_line(key + "=" + tr["value"]);
                        LogEnabled = Convert.ToInt32(tr["value"]);
                    }
                    else if (key == "EnableWatchdogThread")
                    {
                        write_line(key + "=" + tr["value"]);
                        this.EnableWatchdogThread = Convert.ToInt32(tr["value"]);
                    }
                    else if (key == "RespawnFetchingThreadAfterNSecondsOfInactivity")
                    {
                        write_line(key + "=" + tr["value"]);
                        this.RespawnFetchingThreadAfterNSecondsOfInactivity = Convert.ToInt32(tr["value"]);
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
                            write_line(key + "=" + tr["value"]);
                            if (settings != null) settings[key] = tr["value"];
                        }
                    }

                    // else an uninteresting setting
                }
                else
                {
                    // create a new dictionary of settings each time we encounter a new Website section
                    if (tr.Name.ToLower() == "website" && tr.NodeType == XmlNodeType.Element)
                    {
                        settings = new StringDictionary();
                        settings["MessageInputFile"] = "";
                        settings["MessageOutputFile"] = "";
                        settings["ConnectionString"] = "";
                        settings["Pop3Server"] = "";
                        settings["Pop3Port"] = "";
                        settings["Pop3UseSSL"] = "";
                        settings["SubjectMustContain"] = "";
                        settings["SubjectCannotContain"] = "";
                        settings["FromMustContain"] = "";
                        settings["FromCannotContain"] = "";
                        settings["DeleteMessagesOnServer"] = "";
                        settings["InsertBugUrl"] = "";
                        settings["ServiceUsername"] = "";
                        settings["ServicePassword"] = "";
                        settings["TrackingIdString"] = "";
                        this.websites.Add(settings);
                        write_line("*** loading settings for website " + Convert.ToString(this.websites.Count));
                    }
                }
        }
        catch (Exception e)
        {
            write_line("Error trying to read file: " + filename);
            write_line(e);
        }

        tr.Close();
    }

    ///////////////////////////////////////////////////////////////////////
    protected void fetch_messages_for_projects()
    {
        // Get the list of accounts to read

        try
        {
            var sql = @"select
				pj_id, pj_pop3_username, pj_pop3_password
				from projects
				where pj_enable_pop3 = 1";

            var ds = get_dataset(sql);
            foreach (DataRow dr in ds.Tables[0].Rows)
            {
                if (this.state != service_state.STARTED) break;


                write_line("processing project " + Convert.ToString(dr["pj_id"]) + " using account " +
                           dr["pj_pop3_username"]);

                fetch_messages(
                    (string) dr["pj_pop3_username"],
                    (string) dr["pj_pop3_password"],
                    (int) dr["pj_id"]);
            }
        }
        catch (Exception e)
        {
            write_line("Error trying to process messages");
            write_line(e);
        }
    }

    ///////////////////////////////////////////////////////////////////////
    protected string maybe_append_next_line(string[] lines, int j)
    {
        var s = "";
        if (j + 1 < lines.Length)
        {
            var pos = -1;

            // find first non space, non tab
            for (var i = 0; i < lines[j + 1].Length; i++)
            {
                var c = lines[j + 1].Substring(i, 1);
                if (c == "\t" || c == " ")
                {
                }
                else
                {
                    pos = i;
                    break;
                }
            }

            // this line is part of the previous header, so return it
            if (pos > 0)
            {
                s = " ";
                s = lines[j + 1].Substring(pos);
            }
        }

        return s;
    }

    ///////////////////////////////////////////////////////////////////////
    protected void fetch_messages(string user, string password, int projectid)
    {
        string[] messages = null;
        var regex = new Regex("\r\n");
        var test_message_text = new string[100];
        POP3client client = null;

        if (this.MessageInputFile == "")
        {
            try
            {
                client = new POP3client(this.ReadInputStreamCharByChar);

                write_line("****connecting to server:");
                var port = 110;
                if (this.Pop3Port != "") port = Convert.ToInt32(this.Pop3Port);

                var use_ssl = false;
                if (this.Pop3UseSSL != "") use_ssl = this.Pop3UseSSL == "1" ? true : false;

                write_line(client.connect(this.Pop3Server, port, use_ssl));

                write_line("sending POP3 command USER");
                write_line(client.USER(user));

                write_line("sending POP3 command PASS");
                write_line(client.PASS(password));

                write_line("sending POP3 command STAT");
                write_line(client.STAT());

                write_line("sending POP3 command LIST");
                string list;
                list = client.LIST();
                write_line("list follows:");
                write_line(list);
                messages = regex.Split(list);
            }
            catch (Exception e)
            {
                write_line("Exception trying to talk to pop3server");
                write_line(e);
                return;
            }
        }
        else
        {
            var builder = new StringBuilder(4096);
            write_line("opening test input file " + this.MessageInputFile);
            using (var fs = File.OpenRead(this.MessageInputFile))
            {
                var b = new byte[4096];
                //UTF8Encoding encoding = new UTF8Encoding(true);  // Does not work...

                var bytes_read = fs.Read(b, 0, b.Length);

                while (bytes_read > 0)
                {
                    //test_messages += encoding.GetString(b); // Does not work....

                    for (var i = 0; i < bytes_read; i++) builder.Append(Convert.ToChar(b[i])); // Does work

                    bytes_read = fs.Read(b, 0, b.Length);
                }
            }

            var test_messages = builder.ToString();
            var test_regex = new Regex("Q6Q6\r\n");
            test_message_text = test_regex.Split(test_messages);
        }


        string message;
        var message_number = 0;
        int start;
        int end;

        if (this.MessageInputFile == "")
        {
            start = 1;
            end = messages.Length - 1;
        }
        else
        {
            start = 0;
            end = test_message_text.Length;
            if (end > 99) end = 99;
        }

        // loop through the messages
        for (var i = start; i < end; i++)
        {
            heartbeat_datetime = DateTime.Now; // because the watchdog is watching

            if (this.state != service_state.STARTED) break;

            // fetch the message

            write_line("i:" + Convert.ToString(i));
            if (this.MessageInputFile == "")
            {
                var space_pos = messages[i].IndexOf(" ");
                message_number = Convert.ToInt32(messages[i].Substring(0, space_pos));
                message = client.RETR(message_number);
            }
            else
            {
                message = test_message_text[message_number++];
            }

            // for diagnosing problems
            if (this.MessageOutputFile != "")
            {
                var w = File.AppendText(this.MessageOutputFile);
                w.WriteLine(message);
                w.Flush();
                w.Close();
            }

            // break the message up into lines
            var lines = regex.Split(message);

            var from = "";
            var subject = "";

            var encountered_subject = false;
            var encountered_from = false;


            // Loop through the lines of a message.
            // Pick out the subject and body
            for (var j = 0; j < lines.Length; j++)
            {
                if (this.state != service_state.STARTED) break;

                // We know from
                // http://www.devnewsgroups.net/group/microsoft.public.dotnet.framework/topic62515.aspx
                // that headers can be lowercase too.

                if ((lines[j].IndexOf("Subject: ") == 0 || lines[j].IndexOf("subject: ") == 0)
                    && !encountered_subject)
                {
                    subject = lines[j].Replace("Subject: ", "");
                    subject = subject.Replace("subject: ", ""); // try lowercase too
                    subject += maybe_append_next_line(lines, j);

                    encountered_subject = true;
                }
                else if (lines[j].IndexOf("From: ") == 0 && !encountered_from)
                {
                    from = lines[j].Replace("From: ", "");
                    encountered_from = true;
                    from += maybe_append_next_line(lines, j);
                }
                else if (lines[j].IndexOf("from: ") == 0 && !encountered_from)
                {
                    from = lines[j].Replace("from: ", "");
                    encountered_from = true;
                    from += maybe_append_next_line(lines, j);
                }
            } // end for each line

            write_line("\nFrom: " + from);

            write_line("Subject: " + subject);

            if (this.SubjectMustContain != "" && subject.IndexOf(this.SubjectMustContain) < 0)
            {
                write_line("skipping because subject does not contain: " + this.SubjectMustContain);
                continue;
            }

            var bSkip = false;
            for (var k = 0; k < this.SubjectCannotContainStrings.Length; k++)
                if (this.SubjectCannotContainStrings[k] != "")
                    if (subject.IndexOf(this.SubjectCannotContainStrings[k]) >= 0)
                    {
                        write_line("skipping because subject cannot contain: " + this.SubjectCannotContainStrings[k]);
                        bSkip = true;
                        break; // done checking, skip this message
                    }

            if (bSkip) continue;

            if (this.FromMustContain != "" && from.IndexOf(this.FromMustContain) < 0)
            {
                write_line("skipping because from does not contain: " + this.FromMustContain);
                continue; // that is, skip to next message
            }

            for (var k = 0; k < this.FromCannotContainStrings.Length; k++)
                if (this.FromCannotContainStrings[k] != "")
                    if (from.IndexOf(this.FromCannotContainStrings[k]) >= 0)
                    {
                        write_line("skipping because from cannot contain: " + this.FromCannotContainStrings[k]);
                        bSkip = true;
                        break; // done checking, skip this message
                    }

            if (bSkip) continue;

            write_line("calling insert_bug.aspx");
            var Url = this.InsertBugUrl;

            // Try to parse out the bugid from the subject line
            var bugidString = this.TrackingIdString;
            if (this.TrackingIdString == "") bugidString = "DO NOT EDIT THIS:";

            var pos = subject.IndexOf(bugidString);

            if (pos >= 0)
            {
                // position of colon
                pos = subject.IndexOf(":", pos);
                pos++;
                // position of close paren
                var pos2 = subject.IndexOf(")", pos);
                if (pos2 > pos)
                {
                    var bugid_string = subject.Substring(pos, pos2 - pos);
                    write_line("BUGID=" + bugid_string);
                    try
                    {
                        var bugid = int.Parse(bugid_string);
                        Url += "?bugid=" + Convert.ToString(bugid);
                        write_line("updating existing bug " + Convert.ToString(bugid));
                    }
                    catch (Exception e)
                    {
                        write_line("bugid not numeric " + e.Message);
                    }
                }
            }

            var post_data = "username=" + WebUtility.UrlEncode(this.ServiceUsername)
                                        + "&password=" + WebUtility.UrlEncode(this.ServicePassword)
                                        + "&projectid=" + Convert.ToString(projectid)
                                        + "&from=" + WebUtility.UrlEncode(from)
                                        + "&short_desc=" + WebUtility.UrlEncode(subject)
                                        + "&message=" + WebUtility.UrlEncode(message);

            var bytes = Encoding.UTF8.GetBytes(post_data);


            // send request to web server
            HttpWebResponse res = null;
            try
            {
                var req = (HttpWebRequest) WebRequest.Create(Url);


                req.Credentials = CredentialCache.DefaultCredentials;
                req.PreAuthenticate = true;

                //req.Timeout = 200; // maybe?
                //req.KeepAlive = false; // maybe?

                req.Method = "POST";
                req.ContentType = "application/x-www-form-urlencoded";
                req.ContentLength = bytes.Length;
                var request_stream = req.GetRequestStream();
                request_stream.Write(bytes, 0, bytes.Length);
                request_stream.Close();
                res = (HttpWebResponse) req.GetResponse();
            }
            catch (Exception e)
            {
                write_line("HttpWebRequest error url=" + Url);
                write_line(e);
            }

            // examine response

            if (res != null)
            {
                var http_status = (int) res.StatusCode;
                write_line(Convert.ToString(http_status));

                var http_response_header = res.Headers["BTNET"];
                res.Close();

                if (http_response_header != null)
                {
                    write_line(http_response_header);

                    // only delete message from pop3 server if we
                    // know we stored in on the web server ok
                    if (this.MessageInputFile == ""
                        && http_status == 200
                        && this.DeleteMessagesOnServer == "1"
                        && http_response_header.IndexOf("OK") == 0)
                    {
                        write_line("sending POP3 command DELE");
                        write_line(client.DELE(message_number));
                    }
                }
                else
                {
                    write_line("BTNET HTTP header not found.  Skipping the delete of the email from the server.");
                    write_line("Incrementing total error count");
                    this.total_error_count++;
                }
            }
            else
            {
                write_line("No response from web server.  Skipping the delete of the email from the server.");
                write_line("Incrementing total error count");
                this.total_error_count++;
            }

            if (this.total_error_count > this.TotalErrorsAllowed)
            {
                write_line("Stopping because total error count > TotalErrorsAllowed");
                stop();
            }
        } // end for each message


        if (this.MessageInputFile == "")
        {
            write_line("\nsending POP3 command QUIT");
            write_line(client.QUIT());
        }
        else
        {
            write_line("\nclosing input file " + this.MessageInputFile);
        }
    }

    ///////////////////////////////////////////////////////////////////////
    protected DataSet get_dataset(string sql)
    {
        var ds = new DataSet();
        var conn = new SqlConnection(this.ConnectionString);
        conn.Open();
        var da = new SqlDataAdapter(sql, conn);
        da.Fill(ds);
        return ds;
    }

    protected enum service_state
    {
        STARTED,
        PAUSED,
        STOPPED
    }
}


internal class AcceptAllCertificatePolicy : ICertificatePolicy
{
    public bool CheckValidationResult(
        ServicePoint service_point,
        X509Certificate cert,
        WebRequest web_request,
        int certificate_problem)
    {
        // Always accept
        return true;
    }
}