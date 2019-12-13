/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Identification
{
    using BugTracker.Identification;
    using BugTracker.Identification.Changing;

    internal sealed class UserRepository : Repository<User, int>, IUserRepository
    {
        public UserRepository(
            ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}