/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Utilities
{
    using Autofac;
    using Querying;

    public sealed class IoCModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ApplicationFacade>()
                .As<IApplicationFacade>();

            builder.RegisterType<QueryBuilder>()
                .As<IQueryBuilder>();
        }
    }
}