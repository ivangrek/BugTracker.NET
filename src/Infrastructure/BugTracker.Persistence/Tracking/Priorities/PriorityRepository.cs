/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Priorities
{
    using BugTracker.Tracking.Changing.Priorities;

    internal sealed class PriorityRepository : Repository<Priority, int>, IPriorityRepository
    {
        public PriorityRepository(
            ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}