/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Priorities.QueryHandlers
{
    using System.Linq;
    using BugTracker.Tracking.Changing.Priorities;
    using BugTracker.Tracking.Querying.Priorities;
    using Querying;
    using Utilities;

    internal sealed class PriorityDeletePreviewQueryHandler : IQueryHandler<
        IQuery<IPrioritySource, IPriorityDeletePreviewResult>, IPrioritySource, IPriorityDeletePreviewResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public PriorityDeletePreviewQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public IPriorityDeletePreviewResult Handle(IQuery<IPrioritySource, IPriorityDeletePreviewResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<Priority>()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var result = dbQuery
                .Select(x => new PriorityDeletePreviewResult
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .First();

            return result;
        }

        private sealed class PriorityDeletePreviewResult : IPriorityDeletePreviewResult
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}