/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Querying
{
    public interface IQueryBuilder
    {
        IQueryBuilder<TSource> From<TSource>()
            where TSource : class, ISource;
    }

    public interface IQueryBuilder<TSource>
        where TSource : class, ISource
    {
        IQueryBuilder<TSource, TResult> To<TResult>()
            where TResult : class, IResult;
    }

    public interface IQueryBuilder<TSource, TResult> : ICanAddQueryFilter<TSource, TResult>, ICanAddQuerySorter<TSource, TResult>, ICanBuildQuery<TSource, TResult>
        where TSource : class, ISource
        where TResult : class, IResult
    {
    }

    public interface IFilteredQueryBuilder<TSource, TResult> : ICanAddFilterLogicalOperation<TSource, TResult>,
        ICanAddQuerySorter<TSource, TResult>, ICanBuildQuery<TSource, TResult>
        where TSource : class, ISource
        where TResult : class, IResult
    {
    }

    public interface ISortedQueryBuilder<TSource, TResult> : ICanAddSortOrder<TSource, TResult>, ICanAddPaging<TSource, TResult>, ICanBuildQuery<TSource, TResult>
        where TSource : class, ISource
        where TResult : class, IResult
    {
    }

    public interface IPagedQueryBuilder<TSource, TResult> : ICanBuildQuery<TSource, TResult>
        where TSource : class, ISource
        where TResult : class, IResult
    {
    }

    public interface ICanAddPaging<TSource, TResult>
        where TSource : class, ISource
        where TResult : class, IResult
    {
        IPagedQueryBuilder<TSource, TResult> Paginate(int page, int pageSize);
    }

    public interface ICanBuildQuery<TSource, TResult>
        where TSource : class, ISource
        where TResult : class, IResult
    {
        IQuery<TSource, TResult> Build();
    }
}