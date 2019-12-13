/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Querying
{
    public interface IQuerySorter<TSource>
        where TSource : class, ISource
    {
    }
}