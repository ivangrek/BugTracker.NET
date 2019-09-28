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
    using Autofac;
    using Autofac.Integration.Web;
    using Core;
    using Core.Administration;
    using Core.Persistence;

    public class Global : HttpApplication, IContainerProviderAccessor
    {
        // Provider that holds the application container.
        static IContainerProvider _containerProvider;

        // Instance property that will be used by Autofac HttpModules
        // to resolve and inject dependencies.
        public IContainerProvider ContainerProvider
        {
            get { return _containerProvider; }
        }

        protected void Application_Start(object sender, EventArgs e)
        {
            var builder = new ContainerBuilder();

            builder.RegisterType<ApplicationSettings>()
                .As<IApplicationSettings>();

            builder.RegisterType<Authenticate>()
                .As<IAuthenticate>()
                .InstancePerRequest();

            builder.RegisterType<ApplicationContext>()
                .InstancePerRequest();

            builder.RegisterType<CategoryService>()
                .As<ICategoryService>()
                .InstancePerRequest();

            builder.RegisterType<PriorityService>()
                .As<IPriorityService>()
                .InstancePerRequest();

            builder.RegisterType<StatusService>()
                .As<IStatusService>()
                .InstancePerRequest();

            builder.RegisterType<UserDefinedAttributeService>()
                .As<IUserDefinedAttributeService>()
                .InstancePerRequest();

            _containerProvider = new ContainerProvider(builder.Build());

            var path = HttpContext.Current.Server.MapPath("~/");

            HttpRuntime.Cache.Add("MapPath", path, null, Cache.NoAbsoluteExpiration, Cache.NoSlidingExpiration,
                CacheItemPriority.NotRemovable, null);
            HttpRuntime.Cache.Add("Application", Application, null, Cache.NoAbsoluteExpiration,
                Cache.NoSlidingExpiration, CacheItemPriority.NotRemovable, null);

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

            Util.SetContext(HttpContext.Current); // required for map path calls to work in util.cs

            var sr = File.OpenText(Path.Combine(path, @"Content\custom\custom_header.html"));
            Application["custom_header"] = sr.ReadToEnd();
            sr.Close();

            sr = File.OpenText(Path.Combine(path, @"Content\custom\custom_footer.html"));
            Application["custom_footer"] = sr.ReadToEnd();
            sr.Close();

            sr = File.OpenText(Path.Combine(path, @"Content\custom\custom_logo.html"));
            Application["custom_logo"] = sr.ReadToEnd();
            sr.Close();

            sr = File.OpenText(Path.Combine(path, @"Content\custom\custom_welcome.html"));
            Application["custom_welcome"] = sr.ReadToEnd();
            sr.Close();

            var applicationSettings = ContainerProvider.ApplicationContainer
                .Resolve<IApplicationSettings>();

            if (applicationSettings.EnableVotes)
            {
                Core.Tags.CountVotes(Application); // in tags file for convenience for me....
            }

            if (applicationSettings.EnableTags)
            {
                Core.Tags.BuildTagIndex(Application);
            }

            if (applicationSettings.EnableLucene)
            {
                MyLucene.BuildLuceneIndex(Application);
            }

            if (applicationSettings.EnablePop3)
            {
                MyPop3.StartPop3(Application);
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

            var serverVarsString = new StringBuilder();
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
                    serverVarsString.Append("\n");
                    serverVarsString.Append(key);
                    serverVarsString.Append("=");
                    serverVarsString.Append(val);
                }
            }

            var exc = Server.GetLastError().GetBaseException();

            var applicationSettings = ContainerProvider.ApplicationContainer
                .Resolve<IApplicationSettings>();

            var logEnabled = applicationSettings.LogEnabled;
            if (logEnabled)
            {
                var path = Util.GetLogFilePath();

                // open file
                var w = File.AppendText(path);

                w.WriteLine("\nTIME: " + DateTime.Now.ToLongTimeString());
                w.WriteLine("MSG: " + exc.Message);
                w.WriteLine("URL: " + Request.Url);
                w.WriteLine("EXCEPTION: " + exc);
                w.WriteLine(serverVarsString.ToString());
                w.Close();
            }

            var errorEmailEnabled = applicationSettings.ErrorEmailEnabled;
            if (errorEmailEnabled)
            {
                if (exc.Message == "Expected integer.  Possible SQL injection attempt?")
                {
                    // don't bother sending email.  Too many automated attackers
                }
                else if (exc.Message.Contains("Invalid postback or callback argument"))
                {
                    // don't bother sending email.  Too many automated attackers
                }
                else
                {
                    var to = applicationSettings.ErrorEmailTo;
                    var from = applicationSettings.ErrorEmailFrom;
                    var subject = "Error: " + exc.Message;

                    var body = new StringBuilder();

                    body.Append("\nTIME: ");
                    body.Append(DateTime.Now.ToLongTimeString());
                    body.Append("\nURL: ");
                    body.Append(Request.Url);
                    body.Append("\nException: ");
                    body.Append(exc);
                    body.Append(serverVarsString);

                    Email.SendEmail(to, from, "", subject, body.ToString()); // 5 args				
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