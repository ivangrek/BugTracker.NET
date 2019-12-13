/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Bugs
{
    using BugTracker.Tracking.Changing.Bugs;

    internal sealed class BugRepository : Repository<Bug, int>, IBugRepository
    {
        public BugRepository(
            ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}