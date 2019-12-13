/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Organizations
{
    using BugTracker.Tracking.Changing.Organizations;

    internal sealed class OrganizationRepository : Repository<Organization, int>, IOrganizationRepository
    {
        public OrganizationRepository(
            ApplicationDbContext applicationDbContext) : base(applicationDbContext)
        {
        }
    }
}