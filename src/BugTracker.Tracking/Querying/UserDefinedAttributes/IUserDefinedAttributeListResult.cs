/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Querying.UserDefinedAttributes
{
    using System.Collections.Generic;
    using BugTracker.Querying;

    public interface IUserDefinedAttributeListResult : IResult, IEnumerable<IUserDefinedAttributeListRow>
    {
    }

    public interface IUserDefinedAttributeListRow
    {
        int Id { get; }

        string Name { get; }

        int SortSequence { get; }

        int Default { get; }
    }
}