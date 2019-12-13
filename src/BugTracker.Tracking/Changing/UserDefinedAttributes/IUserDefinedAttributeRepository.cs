/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Changing.UserDefinedAttributes
{
    using BugTracker.Changing;

    public interface IUserDefinedAttributeRepository : IRepository<UserDefinedAttribute, int>
    {
    }
}