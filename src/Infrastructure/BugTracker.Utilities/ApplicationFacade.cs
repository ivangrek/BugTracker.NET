/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Utilities
{
    using Autofac;
    using Changing;
    using Changing.Results;
    using Querying;

    internal sealed class ApplicationFacade : IApplicationFacade
    {
        private readonly IComponentContext componentContext;

        public ApplicationFacade(IComponentContext componentContext)
        {
            this.componentContext = componentContext;
        }

        public void Run<TCommand>(TCommand command, out ICommandResult commandResult)
            where TCommand : ICommand
        {
            this.componentContext
                .Resolve<ICommandHandler<TCommand>>()
                .Handle(command, out commandResult);
        }

        public TResult Run<TResult>(IQuery<TResult> query)
            where TResult : class, IResult
        {
            return this.componentContext
                .Resolve<IQueryHandler<IQuery<TResult>, TResult>>()
                .Handle(query);
        }

        public TResult Run<TSource, TResult>(IQuery<TSource, TResult> query)
            where TSource : class, ISource
            where TResult : class, IResult
        {
            return this.componentContext
                .Resolve<IQueryHandler<IQuery<TSource, TResult>, TSource, TResult>>()
                .Handle(query);
        }
    }
}