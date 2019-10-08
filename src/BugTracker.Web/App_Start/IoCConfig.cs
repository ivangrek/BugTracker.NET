/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using Autofac;
    using Autofac.Integration.Mvc;
    using BugTracker.Web.Core;
    using BugTracker.Web.Core.Administration;
    using BugTracker.Web.Core.Persistence;
    using System.Reflection;
    using System.Web.Mvc;

    internal static class IoCConfig
    {
        public static IContainer Configure()
        {
            var builder = new ContainerBuilder();

            // Modules

            // Services
            builder.RegisterType<ApplicationSettings>()
                .As<IApplicationSettings>();

            builder.RegisterType<Security>()
                .As<ISecurity>()
                .InstancePerRequest();

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

            builder.RegisterType<ReportService>()
                .As<IReportService>()
                .InstancePerRequest();

            builder.RegisterType<QueryService>()
                .As<IQueryService>()
                .InstancePerRequest();

            // WebControllers
            builder.RegisterControllers(Assembly.GetExecutingAssembly());

            // Init
            var container = builder.Build();
            var autofacWebDependencyResolver = new AutofacDependencyResolver(container);

            DependencyResolver.SetResolver(autofacWebDependencyResolver);

            return container;
        }
    }
}