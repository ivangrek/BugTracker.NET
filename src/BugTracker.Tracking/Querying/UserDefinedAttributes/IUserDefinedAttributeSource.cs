/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Querying.UserDefinedAttributes
{
    using BugTracker.Querying;

    public interface IUserDefinedAttributeSource : ISource
    {
        int Id { get; }

        string Name { get; }

        int SortSequence { get; }

        int Default { get; }
    }
}