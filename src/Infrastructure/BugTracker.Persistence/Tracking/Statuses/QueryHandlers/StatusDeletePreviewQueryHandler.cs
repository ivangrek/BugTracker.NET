/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Statuses.QueryHandlers
{
    using System.Linq;
    using BugTracker.Tracking.Changing.Statuses;
    using BugTracker.Tracking.Querying.Statuses;
    using Querying;
    using Utilities;

    internal sealed class StatusDeletePreviewQueryHandler : IQueryHandler<
        IQuery<IStatusSource, IStatusDeletePreviewResult>, IStatusSource, IStatusDeletePreviewResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public StatusDeletePreviewQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public IStatusDeletePreviewResult Handle(IQuery<IStatusSource, IStatusDeletePreviewResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<Status>()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var result = dbQuery
                .Select(x => new StatusDeletePreviewResult
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .First();

            return result;
        }

        private sealed class StatusDeletePreviewResult : IStatusDeletePreviewResult
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}