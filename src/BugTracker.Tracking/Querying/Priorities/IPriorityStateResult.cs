/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Querying.Priorities
{
    using BugTracker.Querying;

    public interface IPriorityStateResult : IResult
    {
        int Id { get; }

        string Name { get; }

        string BackgroundColor { get; }

        int SortSequence { get; }

        string Style { get; }

        int Default { get; }
    }
}