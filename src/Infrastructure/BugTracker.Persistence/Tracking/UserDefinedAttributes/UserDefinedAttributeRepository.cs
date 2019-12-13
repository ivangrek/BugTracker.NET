/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.UserDefinedAttributes
{
    using BugTracker.Tracking.Changing.UserDefinedAttributes;

    internal sealed class UserDefinedAttributeRepository : Repository<UserDefinedAttribute, int>,
        IUserDefinedAttributeRepository
    {
        public UserDefinedAttributeRepository(
            ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}