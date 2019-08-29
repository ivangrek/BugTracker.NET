/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.Collections.Specialized;
    using System.IO;
    using System.Text;
    using System.Web;
    using System.Web.Caching;
    using Core;

    public class Global : HttpApplication
    {

        protected void Application_Start(object sender, EventArgs e)
        {
            /*
        System.Threading.Thread thread = new System.Threading.Thread(my_threadproc);
        thread.Start(null);
    */

            var path = HttpContext.Current.Server.MapPath(null);
            //    HttpRuntime.Cache["MapPath"] = path;
            //    HttpRuntime.Cache["Application"] = Application;
            HttpRuntime.Cache.Add("MapPath", path, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.NotRemovable, null);
            HttpRuntime.Cache.Add("Application", Application, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration, CacheItemPriority.NotRemovable, null);

            var dir = path + "\\App_Data";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            dir = path + "\\App_Data\\logs";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            dir = path + "\\App_Data\\uploads";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            dir = path + "\\App_Data\\lucene_index";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            Util.set_context(HttpContext.Current); // required for map path calls to work in util.cs

            var sr = File.OpenText(path + "\\custom\\custom_header.html");
            Application["custom_header"] = sr.ReadToEnd();
            sr.Close();

            sr = File.OpenText(path + "\\custom\\custom_footer.html");
            Application["custom_footer"] = sr.ReadToEnd();
            sr.Close();

            sr = File.OpenText(path + "\\custom\\custom_logo.html");
            Application["custom_logo"] = sr.ReadToEnd();
            sr.Close();

            sr = File.OpenText(path + "\\custom\\custom_welcome.html");
            Application["custom_welcome"] = sr.ReadToEnd();
            sr.Close();

            if (Util.get_setting("EnableVotes", "0") == "1")
            {
                Tags.count_votes(this.Application); // in tags file for convenience for me....
            }

            if (Util.get_setting("EnableTags", "0") == "1")
            {
                Tags.build_tag_index(this.Application);
            }

            if (Util.get_setting("EnableLucene", "1") == "1")
            {
                MyLucene.build_lucene_index(this.Application);
            }

            if (Util.get_setting("EnablePop3", "0") == "1")
            {
                MyPop3.start_pop3(this.Application);
            }
        }

        protected void Session_Start(object sender, EventArgs e)
        {

        }

        protected void Application_BeginRequest(object sender, EventArgs e)
        {

        }

        protected void Application_AuthenticateRequest(object sender, EventArgs e)
        {

        }

        protected void Application_Error(object sender, EventArgs e)
        {
            // Put the server vars into a string

            var server_vars_string = new StringBuilder();
            /*
                    var varnames = Request.ServerVariables.AllKeys.Where(x => !x.StartsWith("AUTH_PASSWORD"));

                    foreach (string varname in varnames)
                    {
                        string varval = Request.ServerVariables[varname];
                        if (!string.IsNullOrEmpty(varval))
                        {
                            server_vars_string.Append("\n");
                            server_vars_string.Append(varname);
                            server_vars_string.Append("=");
                            server_vars_string.Append(varval);
                        }
                    }
            */


            int loop1, loop2;
            NameValueCollection coll;

            // Load ServerVariable collection into NameValueCollection object.
            coll = Request.ServerVariables;
            // Get names of all keys into a string array.
            var arr1 = coll.AllKeys;
            for (loop1 = 0; loop1 < arr1.Length; loop1++)
            {
                var key = arr1[loop1];
                if (key.StartsWith("AUTH_PASSWORD"))
                    continue;

                var arr2 = coll.GetValues(key);

                for (loop2 = 0; loop2 < 1; loop2++)
                {
                    var val = arr2[loop2];
                    if (string.IsNullOrEmpty(val))
                        break;
                    server_vars_string.Append("\n");
                    server_vars_string.Append(key);
                    server_vars_string.Append("=");
                    server_vars_string.Append(val);
                }
            }


            var exc = Server.GetLastError().GetBaseException();

            var log_enabled = (Util.get_setting("LogEnabled", "1") == "1");
            if (log_enabled)
            {

                var path = Util.get_log_file_path();

                // open file
                var w = File.AppendText(path);

                w.WriteLine("\nTIME: " + DateTime.Now.ToLongTimeString());
                w.WriteLine("MSG: " + exc.Message.ToString());
                w.WriteLine("URL: " + Request.Url.ToString());
                w.WriteLine("EXCEPTION: " + exc.ToString());
                w.WriteLine(server_vars_string.ToString());
                w.Close();
            }

            var error_email_enabled = (Util.get_setting("ErrorEmailEnabled", "1") == "1");
            if (error_email_enabled)
            {

                if (exc.Message.ToString() == "Expected integer.  Possible SQL injection attempt?")
                {
                    // don't bother sending email.  Too many automated attackers
                }
                else if (exc.Message.ToString().Contains("Invalid postback or callback argument"))
                {
                    // don't bother sending email.  Too many automated attackers
                }
                else
                {
                    var to = Util.get_setting("ErrorEmailTo", "");
                    var from = Util.get_setting("ErrorEmailFrom", "");
                    var subject = "Error: " + exc.Message.ToString();

                    var body = new StringBuilder();


                    body.Append("\nTIME: ");
                    body.Append(DateTime.Now.ToLongTimeString());
                    body.Append("\nURL: ");
                    body.Append(Request.Url.ToString());
                    body.Append("\nException: ");
                    body.Append(exc.ToString());
                    body.Append(server_vars_string.ToString());

                    Email.send_email(to, from, "", subject, body.ToString()); // 5 args				
                }
            }
        }

        protected void Session_End(object sender, EventArgs e)
        {

        }

        protected void Application_End(object sender, EventArgs e)
        {

        }
    }
}
