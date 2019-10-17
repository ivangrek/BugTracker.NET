﻿/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.IO;
    using System.Text;
    using System.Web;
    using System.Web.Caching;
    using System.Web.Mvc;
    using System.Web.Optimization;
    using System.Web.Routing;
    using Autofac;
    using Autofac.Integration.Web;
    using Core;

    [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "This is an infrastructure")]
    public class Global : HttpApplication, IContainerProviderAccessor
    {
        // Provider that holds the application container.
        private static IContainerProvider _containerProvider;

        // Instance property that will be used by Autofac HttpModules
        // to resolve and inject dependencies.
        public IContainerProvider ContainerProvider => _containerProvider;

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "This is an infrastructure")]
        protected void Application_Start(object sender, EventArgs e)
        {
            var container = IoCConfig.Configure();

            AreaRegistration.RegisterAllAreas();
            //GlobalConfiguration.Configure(WebApiConfig.Register);
            //FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            _containerProvider = new ContainerProvider(container);

            var path = HttpContext.Current.Server.MapPath("~/");

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

            var applicationSettings = container
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

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "This is an infrastructure")]
        protected void Application_Error(object sender, EventArgs e)
        {
            // Put the server vars into a string
            var serverVarsString = new StringBuilder();

            // Load ServerVariable collection into NameValueCollection object.
            var coll = Request.ServerVariables;

            // Get names of all keys into a string array.
            var arr1 = coll.AllKeys;

            for (var loop1 = 0; loop1 < arr1.Length; loop1++)
            {
                var key = arr1[loop1];

                if (key.StartsWith("AUTH_PASSWORD", StringComparison.InvariantCulture))
                {
                    continue;
                }

                var arr2 = coll.GetValues(key);

                for (var loop2 = 0; loop2 < 1; loop2++)
                {
                    var val = arr2[loop2];

                    if (string.IsNullOrEmpty(val))
                    {
                        break;
                    }

                    serverVarsString.Append("\n");
                    serverVarsString.Append(key);
                    serverVarsString.Append("=");
                    serverVarsString.Append(val);
                }
            }

            var exc = Server.GetLastError()
                .GetBaseException();

            var applicationSettings = ContainerProvider.ApplicationContainer
                .Resolve<IApplicationSettings>();

            var logEnabled = applicationSettings.LogEnabled;

            if (logEnabled)
            {
                var path = Util.GetLogFilePath();

                // open file
                var w = File.AppendText(path);

                w.WriteLine($"\nTIME: {DateTime.Now.ToLongTimeString()}");
                w.WriteLine($"MSG: {exc.Message}");
                w.WriteLine($"URL: {Request.Url}");
                w.WriteLine($"EXCEPTION: {exc}");
                w.WriteLine(serverVarsString.ToString());
                w.Close();
            }

            var errorEmailEnabled = applicationSettings.ErrorEmailEnabled;

            if (errorEmailEnabled)
            {
                if (exc.Message == "Expected integer. Possible SQL injection attempt?")
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
                    var subject = $"Error: {exc.Message}";
                    var body = new StringBuilder();

                    body.Append("\nTIME: ");
                    body.Append(DateTime.Now.ToLongTimeString());
                    body.Append("\nURL: ");
                    body.Append(Request.Url);
                    body.Append("\nException: ");
                    body.Append(exc);
                    body.Append(serverVarsString);

                    Email.SendEmail(to, from, string.Empty, subject, body.ToString()); // 5 args
                }
            }
        }
    }
}