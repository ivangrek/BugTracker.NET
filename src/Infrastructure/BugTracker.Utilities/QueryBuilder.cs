/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Utilities
{
    using System;
    using System.Linq.Expressions;
    using Querying;

    internal sealed class QueryBuilder : IQueryBuilder
    {
        public IQueryBuilder<TSource> From<TSource>()
            where TSource : class, ISource
        {
            return new QueryBuilder<TSource>();
        }
    }

    internal sealed class QueryBuilder<TSource> : IQueryBuilder<TSource>
        where TSource : class, ISource
    {
        public IQueryBuilder<TSource, TResult> To<TResult>()
            where TResult : class, IResult
        {
            return new QueryBuilder<TSource, TResult>();
        }
    }

    internal sealed class QueryBuilder<TSource, TResult> : IQueryBuilder<TSource, TResult>,
        IQueryFilterBuilder<TSource, TResult>, IFilteredQueryBuilder<TSource, TResult>,
        IQuerySorterBuilder<TSource, TResult>, ISortedQueryBuilder<TSource, TResult>,
        IPagedQueryBuilder<TSource, TResult>
        where TSource : class, ISource
        where TResult : class, IResult
    {
        private bool andOperation = true;
        private QuerySorter<TSource> current;
        private IQueryFilter<TSource> filter;

        private int? page;
        private int? pageSize;
        private IQuerySorter<TSource> sorter;

        public IQueryFilterBuilder<TSource, TResult> And()
        {
            this.andOperation = true;

            return this;
        }

        public IQueryFilterBuilder<TSource, TResult> Or()
        {
            this.andOperation = false;

            return this;
        }

        public IQuery<TSource, TResult> Build()
        {
            IQueryPager pager = null;

            if (this.page.HasValue)
            {
                pager = new QueryPager
                {
                    Page = this.page.Value,
                    PageSize = this.pageSize.Value
                };
            }

            var query = new Query
            {
                Filter = this.filter,
                Sorter = this.sorter,
                Pager = pager
            };

            return query;
        }

        public IQueryFilterBuilder<TSource, TResult> Filter()
        {
            return this;
        }

        public IQuerySorterBuilder<TSource, TResult> Sort()
        {
            return this;
        }

        public ICanAddFilterComparisonOperation<TSource, TResult> Scope()
        {
            //if (this.result == null)
            //{
            //    this.result = filter;
            //}
            //else
            //{
            //    if (this.andOperation)
            //    {
            //        this.result = new QueryAndLogicalFilter<TSource>()
            //        {
            //            Left = this.result,
            //            Right = filter
            //        };
            //    }
            //    else
            //    {
            //        this.result = new QueryOrLogicalFilter<TSource>()
            //        {
            //            Left = this.result,
            //            Right = filter
            //        };
            //    }
            //}

            return this;
        }

        public IFilteredQueryBuilder<TSource, TResult> Equal(string key, object value)
        {
            var equal = new QueryEqualComparisonFilter<TSource>
            {
                Key = key,
                Value = value
            };

            if (this.filter == null)
            {
                this.filter = equal;
            }
            else
            {
                if (this.andOperation)
                    this.filter = new QueryAndLogicalFilter<TSource>
                    {
                        Left = this.filter,
                        Right = equal
                    };
                else
                    this.filter = new QueryOrLogicalFilter<TSource>
                    {
                        Left = this.filter,
                        Right = equal
                    };
            }

            return this;
        }

        public IFilteredQueryBuilder<TSource, TResult> Equal<TValue>(Expression<Func<TSource, TValue>> key,
            TValue value)
        {
            switch (key.Body)
            {
                case MemberExpression memberExpression:
                    return Equal(memberExpression.Member.Name, value);
                case UnaryExpression unaryExpression:
                    return Equal((unaryExpression.Operand as MemberExpression).Member.Name, value);
            }

            return this;
        }

        public ISortedQueryBuilder<TSource, TResult> AscendingBy(string key)
        {
            var sorter = new QueryAscendingSorter<TSource>
            {
                Key = key
            };

            if (this.sorter == null)
            {
                this.sorter = sorter;
                this.current = sorter;
            }
            else
            {
                this.current.ThenBy = sorter;
                this.current = sorter;
            }

            return this;
        }

        public ISortedQueryBuilder<TSource, TResult> AscendingBy(Expression<Func<TSource, object>> key)
        {
            switch (key.Body)
            {
                case MemberExpression memberExpression:
                    return AscendingBy(memberExpression.Member.Name);
                case UnaryExpression unaryExpression:
                    return AscendingBy((unaryExpression.Operand as MemberExpression).Member.Name);
            }

            return this;
        }

        public ISortedQueryBuilder<TSource, TResult> DescendingBy(string key)
        {
            var sorter = new QueryDescendingSorter<TSource>
            {
                Key = key
            };

            if (this.sorter == null)
            {
                this.sorter = sorter;
                this.current = sorter;
            }
            else
            {
                this.current.ThenBy = sorter;
                this.current = sorter;
            }

            return this;
        }

        public ISortedQueryBuilder<TSource, TResult> DescendingBy(Expression<Func<TSource, object>> key)
        {
            switch (key.Body)
            {
                case MemberExpression memberExpression:
                    return DescendingBy(memberExpression.Member.Name);
                case UnaryExpression unaryExpression:
                    return DescendingBy((unaryExpression.Operand as MemberExpression).Member.Name);
            }

            return this;
        }

        public IPagedQueryBuilder<TSource, TResult> Paginate(int page, int pageSize)
        {
            this.page = page;
            this.pageSize = pageSize;

            return this;
        }

        private sealed class Query : IQuery<TSource, TResult>
        {
            public IQueryFilter<TSource> Filter { get; set; }

            public IQuerySorter<TSource> Sorter { get; set; }

            public IQueryPager Pager { get; set; }
        }

        private sealed class QueryPager : IQueryPager
        {
            public int Page { get; set; }

            public int PageSize { get; set; }
        }
    }
}