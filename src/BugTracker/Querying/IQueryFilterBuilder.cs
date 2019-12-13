/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Querying
{
    using System;
    using System.Linq.Expressions;

    public interface IQueryFilterBuilder<TSource, TResult> : ICanAddFilterComparisonOperation<TSource, TResult>
        where TSource : class, ISource
        where TResult : class, IResult
    {
        ICanAddFilterComparisonOperation<TSource, TResult> Scope();
    }

    public interface ICanAddQueryFilter<TSource, TResult>
        where TSource : class, ISource
        where TResult : class, IResult
    {
        IQueryFilterBuilder<TSource, TResult> Filter();
    }

    public interface ICanAddFilterComparisonOperation<TSource, TResult>
        where TSource : class, ISource
        where TResult : class, IResult
    {
        IFilteredQueryBuilder<TSource, TResult> Equal(string key, object value);

        IFilteredQueryBuilder<TSource, TResult> Equal<TValue>(Expression<Func<TSource, TValue>> key, TValue value);
    }

    public interface ICanAddFilterLogicalOperation<TSource, TResult>
        where TSource : class, ISource
        where TResult : class, IResult
    {
        IQueryFilterBuilder<TSource, TResult> And();

        IQueryFilterBuilder<TSource, TResult> Or();
    }
}