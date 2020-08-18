/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System;
    using System.IO;
    using System.Web;
    using System.Web.Http;
    using System.Web.Mvc;
    using System.Web.Optimization;
    using System.Web.Routing;
    using Autofac;
    using Autofac.Integration.Web;
    using Core;
    using NLog;

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
            Util.ServerRootForlder = AppDomain.CurrentDomain.BaseDirectory;

            var container = IoCConfig.Configure();

            LoggingConfig.Configure();
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            //FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);

            _containerProvider = new ContainerProvider(container);

            CreateRequiredDirectories();
            LoadConfiguration();
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Naming", "CA1707:Identifiers should not contain underscores", Justification = "This is an infrastructure")]
        protected void Application_Error(object sender, EventArgs e)
        {
            var exc = Server.GetLastError().GetBaseException();
            var logger = LogManager.GetCurrentClassLogger();

            logger.Fatal(exc);
        }

        private static void CreateRequiredDirectories()
        {
            var dir = Path.Combine(Util.ServerRootForlder, "App_Data");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            dir = Path.Combine(Util.ServerRootForlder, "App_Data", "logs");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            dir = Path.Combine(Util.ServerRootForlder, "App_Data", "uploads");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            dir = Path.Combine(Util.ServerRootForlder, "App_Data", "lucene_index");

            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
        }

        private void LoadConfiguration()
        {
            using (var streamReader = File.OpenText(Path.Combine(Util.ServerRootForlder, @"Content\custom\custom_header.html")))
            {
                Util.CustomHeaderHtml = streamReader.ReadToEnd();
            }

            using (var streamReader = File.OpenText(Path.Combine(Util.ServerRootForlder, @"Content\custom\custom_footer.html")))
            {
                Util.CustomFooterHtml = streamReader.ReadToEnd();
            }

            using (var streamReader = File.OpenText(Path.Combine(Util.ServerRootForlder, @"Content\custom\custom_logo.html")))
            {
                Util.CustomLogoHtml = streamReader.ReadToEnd();
            }

            using (var streamReader = File.OpenText(Path.Combine(Util.ServerRootForlder, @"Content\custom\custom_welcome.html")))
            {
                Util.CustomWelcomeHtml = streamReader.ReadToEnd();
            }

            var applicationSettings = _containerProvider.ApplicationContainer
                .Resolve<IApplicationSettings>();

            if (applicationSettings.EnableVotes)
            {
                Tags.CountVotes(Application); // in tags file for convenience for me....
            }

            if (applicationSettings.EnableTags)
            {
                Tags.BuildTagIndex();
            }

            if (applicationSettings.EnableLucene)
            {
                MyLucene.BuildLuceneIndex();
            }
        }
    }
}