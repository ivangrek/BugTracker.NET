/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Priorities.Commands
{
    using BugTracker.Changing;

    public interface ICreateCommand : ICommand
    {
        string Name { get; }

        int SortSequence { get; }

        string BackgroundColor { get; }

        string Style { get; }

        bool Default { get; }
    }
}