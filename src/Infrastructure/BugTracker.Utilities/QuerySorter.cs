/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Utilities
{
    using Querying;

    internal abstract class QuerySorter<TSource> : IQuerySorter<TSource>
        where TSource : class, ISource
    {
        public string Key { get; set; }

        public QuerySorter<TSource> ThenBy { get; set; }
    }

    internal sealed class QueryAscendingSorter<TSource> : QuerySorter<TSource>
        where TSource : class, ISource
    {
    }

    internal sealed class QueryDescendingSorter<TSource> : QuerySorter<TSource>
        where TSource : class, ISource
    {
    }
}