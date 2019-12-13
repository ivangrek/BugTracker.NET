/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Organizations.QueryHandlers
{
    using System.Linq;
    using BugTracker.Tracking.Changing.Organizations;
    using BugTracker.Tracking.Querying.Organizations;
    using Querying;
    using Utilities;

    internal sealed class OrganizationDeletePreviewQueryHandler : IQueryHandler<IQuery<IOrganizationSource, IOrganizationDeletePreviewResult>, IOrganizationSource,
        IOrganizationDeletePreviewResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public OrganizationDeletePreviewQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public IOrganizationDeletePreviewResult Handle(
            IQuery<IOrganizationSource, IOrganizationDeletePreviewResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<Organization>()
                .AsNoTracking()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var result = dbQuery
                .Select(x => new OrganizationDeletePreviewResult
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .First();

            return result;
        }

        private sealed class OrganizationDeletePreviewResult : IOrganizationDeletePreviewResult
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}