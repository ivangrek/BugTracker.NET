/*
    Copyright 2002-2011 Corey Trager
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Querying.Priorities
{
    using System.Collections.Generic;
    using BugTracker.Querying;

    public interface IPriorityListResult : IResult, IEnumerable<IPriorityListRow>
    {
    }

    public interface IPriorityListRow
    {
        int Id { get; }

        string Name { get; }

        string BackgroundColor { get; }

        int SortSequence { get; }

        string Style { get; }

        int Default { get; }
    }
}