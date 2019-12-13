/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Utilities
{
    using System;
    using System.Linq;
    using System.Linq.Expressions;
    using Querying;

    public static class QueryableExtensions
    {
        public static IQueryable<TSource> ApplyQueryFilter<TSource, TFilterSource>(this IQueryable<TSource> source,
            IQueryFilter<TFilterSource> filter)
            where TFilterSource : class, ISource
        {
            if (filter == null) return source;

            var parameterExpression = Expression.Parameter(typeof(TSource), "x");
            var filterExpression = BuildExpression<TSource, TFilterSource>(parameterExpression, filter);

            return source.Where(filterExpression);
        }

        public static IQueryable<TSource> ApplyQuerySorter<TSource, TSorterSource>(this IQueryable<TSource> source,
            IQuerySorter<TSorterSource> sorter)
            where TSorterSource : class, ISource
        {
            if (!(sorter is QuerySorter<TSorterSource> querySorter)) return source;

            var first = true;

            do
            {
                string methodName;

                switch (querySorter)
                {
                    case QueryAscendingSorter<TSorterSource> _:
                        methodName = first
                            ? "OrderBy"
                            : "ThenBy";
                        break;
                    case QueryDescendingSorter<TSorterSource> _:
                        methodName = first
                            ? "OrderByDescending"
                            : "ThenByDescending";
                        break;
                    default:
                        return source;
                }

                var sourceType = typeof(TSource);
                var parameterExpression = Expression.Parameter(sourceType, "x");
                var property = sourceType.GetProperty(querySorter.Key);
                var propertyAccess = Expression.MakeMemberAccess(parameterExpression, property);
                var sorterExpression = Expression.Lambda(propertyAccess, parameterExpression);
                var resultExpression = Expression.Call(typeof(Queryable),
                    methodName,
                    new[] { sourceType, property.PropertyType },
                    source.Expression,
                    Expression.Quote(sorterExpression));

                source = source.Provider.CreateQuery<TSource>(resultExpression);
                querySorter = querySorter.ThenBy;
                first = false;
            } while (querySorter != null);

            return source;
        }

        public static IQueryable<TSource> ApplyQueryPager<TSource>(this IQueryable<TSource> source, IQueryPager pager)
        {
            if (pager == null)
            {
                return source;
            }

            return source.Skip((pager.Page - 1) * pager.PageSize)
                .Take(pager.PageSize);
        }

        private static Expression<Func<TSource, bool>> BuildExpression<TSource, TFilterSource>(
            ParameterExpression parameterExpression, IQueryFilter<TFilterSource> filter)
            where TFilterSource : class, ISource
        {
            switch (filter)
            {
                case QueryComparisonFilter<TFilterSource> comparisonFilter:
                    return BuildComparisonExpression<TSource, TFilterSource>(parameterExpression, comparisonFilter);
                case QueryLogicalFilter<TFilterSource> logicalFilter:
                    return BuildLogicalExpression<TSource, TFilterSource>(parameterExpression, logicalFilter);
                default:
                    throw new InvalidOperationException("Unknown Filter");
            }
        }

        private static Expression<Func<TSource, bool>> BuildComparisonExpression<TSource, TFilterSource>(
            ParameterExpression parameterExpression, QueryComparisonFilter<TFilterSource> filter)
            where TFilterSource : class, ISource
        {
            var propertyExpression = Expression.Property(parameterExpression, filter.Key);

            switch (filter)
            {
                case QueryEqualComparisonFilter<TFilterSource> _:
                    var equalExpression = Expression.Equal(propertyExpression, Expression.Constant(filter.Value));

                    return Expression.Lambda<Func<TSource, bool>>(equalExpression, parameterExpression);
                case QueryNotEqualComparisonFilter<TFilterSource> _:
                    var notEqualExpression = Expression.NotEqual(propertyExpression, Expression.Constant(filter.Value));

                    return Expression.Lambda<Func<TSource, bool>>(notEqualExpression, parameterExpression);
                default:
                    throw new InvalidOperationException("Unknown Comparison Filter");
            }
        }

        private static Expression<Func<TSource, bool>> BuildLogicalExpression<TSource, TFilterSource>(
            ParameterExpression parameterExpression, QueryLogicalFilter<TFilterSource> filter)
            where TFilterSource : class, ISource
        {
            var leftExpression = BuildExpression<TSource, TFilterSource>(parameterExpression, filter.Left);
            var rightExpression = BuildExpression<TSource, TFilterSource>(parameterExpression, filter.Right);

            switch (filter)
            {
                case QueryAndLogicalFilter<TFilterSource> _:
                    var andExpression = Expression.AndAlso(leftExpression.Body, rightExpression.Body);

                    return Expression.Lambda<Func<TSource, bool>>(andExpression, parameterExpression);
                case QueryOrLogicalFilter<TFilterSource> _:
                    var orExpression = Expression.OrElse(leftExpression.Body, rightExpression.Body);

                    return Expression.Lambda<Func<TSource, bool>>(orExpression, parameterExpression);
                default:
                    throw new InvalidOperationException("Unknown Logical Filter");
            }
        }
    }
}