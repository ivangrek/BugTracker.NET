/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.UserDefinedAttributes.Commands
{
    using BugTracker.Changing;

    public interface IUpdateCommand : ICommand
    {
        int Id { get; }

        string Name { get; }

        int SortSequence { get; }

        bool Default { get; }
    }
}