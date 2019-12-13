/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Utilities
{
    using Querying;

    internal abstract class QueryComparisonFilter<TSource> : IQueryFilter<TSource>
        where TSource : class, ISource
    {
        public string Key { get; set; }

        public object Value { get; set; }
    }

    internal sealed class QueryEqualComparisonFilter<TSource> : QueryComparisonFilter<TSource>
        where TSource : class, ISource
    {
    }

    internal sealed class QueryNotEqualComparisonFilter<TSource> : QueryComparisonFilter<TSource>
        where TSource : class, ISource
    {
    }

    internal abstract class QueryLogicalFilter<TSource> : IQueryFilter<TSource>
        where TSource : class, ISource
    {
        public IQueryFilter<TSource> Left { get; set; }

        public IQueryFilter<TSource> Right { get; set; }
    }

    internal sealed class QueryAndLogicalFilter<TSource> : QueryLogicalFilter<TSource>
        where TSource : class, ISource
    {
    }

    internal sealed class QueryOrLogicalFilter<TSource> : QueryLogicalFilter<TSource>
        where TSource : class, ISource
    {
    }
}