/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Statuses
{
    using BugTracker.Tracking.Changing.Statuses;

    internal sealed class StatusRepository : Repository<Status, int>, IStatusRepository
    {
        public StatusRepository(
            ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}