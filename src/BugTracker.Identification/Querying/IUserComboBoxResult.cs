/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Identification.Querying
{
    using System.Collections.Generic;
    using BugTracker.Querying;
    using BugTracker.Querying.Results;

    public interface IUserComboBoxResult : IResult, IEnumerable<IIdName>
    {
    }
}