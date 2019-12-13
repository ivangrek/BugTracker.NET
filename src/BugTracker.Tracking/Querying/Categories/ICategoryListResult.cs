/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Querying.Categories
{
    using System.Collections.Generic;
    using BugTracker.Querying;

    public interface ICategoryListResult : IResult, IEnumerable<ICategoryListRow>
    {
    }

    public interface ICategoryListRow
    {
        int Id { get; }

        string Name { get; }

        int SortSequence { get; }

        int Default { get; }
    }
}