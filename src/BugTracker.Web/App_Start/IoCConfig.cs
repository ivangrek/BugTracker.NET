/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web
{
    using System.Reflection;
    using System.Web.Mvc;
    using Autofac;
    using Autofac.Integration.Mvc;
    using Changing;
    using Core;
    using Core.Persistence;
    using Identification;

    internal static class IoCConfig
    {
        public static IContainer Configure()
        {
            var builder = new ContainerBuilder();

            // Modules
            builder.RegisterModule<IoCModule>();
            builder.RegisterModule<Tracking.IoCModule>();
            builder.RegisterModule<Persistence.IoCModule>();
            builder.RegisterModule<Utilities.IoCModule>();

            builder.RegisterGenericDecorator(typeof(ValidationCommandHandlerDecorator<>), typeof(ICommandHandler<>));
            builder.RegisterGenericDecorator(typeof(TransactionCommandHandlerDecorator<>), typeof(ICommandHandler<>));
            builder.RegisterGenericDecorator(typeof(LoggingCommandHandlerDecorator<>), typeof(ICommandHandler<>));

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