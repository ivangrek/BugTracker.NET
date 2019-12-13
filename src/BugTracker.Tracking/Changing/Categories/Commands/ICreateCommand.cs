/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.Categories.Commands
{
    using BugTracker.Changing;

    public interface ICreateCommand : ICommand
    {
        string Name { get; }

        int SortSequence { get; }

        bool Default { get; }
    }
}