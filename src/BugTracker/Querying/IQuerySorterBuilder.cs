/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Querying
{
    using System;
    using System.Linq.Expressions;

    public interface IQuerySorterBuilder<TSource, TResult> : ICanAddSortOrder<TSource, TResult>
        where TSource : class, ISource
        where TResult : class, IResult
    {
    }

    public interface ICanAddQuerySorter<TSource, TResult>
        where TSource : class, ISource
        where TResult : class, IResult
    {
        IQuerySorterBuilder<TSource, TResult> Sort();
    }

    public interface ICanAddSortOrder<TSource, TResult>
        where TSource : class, ISource
        where TResult : class, IResult
    {
        ISortedQueryBuilder<TSource, TResult> AscendingBy(string key);

        ISortedQueryBuilder<TSource, TResult> AscendingBy(Expression<Func<TSource, object>> key);

        ISortedQueryBuilder<TSource, TResult> DescendingBy(string key);

        ISortedQueryBuilder<TSource, TResult> DescendingBy(Expression<Func<TSource, object>> key);
    }
}