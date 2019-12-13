/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker
{
    using Changing;
    using Changing.Results;
    using Querying;

    public interface IApplicationFacade
    {
        void Run<TCommand>(TCommand command, out ICommandResult commandResult)
            where TCommand : ICommand;

        TResult Run<TResult>(IQuery<TResult> query)
            where TResult : class, IResult;

        TResult Run<TSource, TResult>(IQuery<TSource, TResult> query)
            where TSource : class, ISource
            where TResult : class, IResult;
    }
}