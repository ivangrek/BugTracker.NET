/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Querying
{
    public interface IQuery<TResult>
        where TResult : class, IResult
    {
    }

    public interface IQuery<TSource, TResult>
        where TSource : class, ISource
        where TResult : class, IResult
    {
        IQueryFilter<TSource> Filter { get; }

        IQuerySorter<TSource> Sorter { get; }

        IQueryPager Pager { get; }
    }
}