/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Querying.Statuses
{
    using BugTracker.Querying;

    public interface IStatusSource : ISource
    {
        int Id { get; }

        string Name { get; }

        int SortSequence { get; }

        string Style { get; }

        int Default { get; }
    }
}