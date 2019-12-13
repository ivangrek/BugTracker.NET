/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Querying
{
    public interface IQueryHandler<in TQuery, out TResult>
        where TQuery : class, IQuery<TResult>
        where TResult : class, IResult
    {
        TResult Handle(TQuery query);
    }

    public interface IQueryHandler<in TQuery, TSource, out TResult>
        where TQuery : class, IQuery<TSource, TResult>
        where TSource : class, ISource
        where TResult : class, IResult
    {
        TResult Handle(TQuery query);
    }
}