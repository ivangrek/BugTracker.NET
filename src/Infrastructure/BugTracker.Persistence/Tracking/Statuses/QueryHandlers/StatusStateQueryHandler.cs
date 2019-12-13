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

    internal sealed class StatusStateQueryHandler : IQueryHandler<IQuery<IStatusSource, IStatusStateResult>,
        IStatusSource, IStatusStateResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public StatusStateQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public IStatusStateResult Handle(IQuery<IStatusSource, IStatusStateResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<Status>()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var result = dbQuery
                .Select(x => new StatusStateResult
                {
                    Id = x.Id,
                    Name = x.Name,
                    SortSequence = x.SortSequence,
                    Style = x.Style,
                    Default = x.Default
                })
                .First();

            return result;
        }

        private sealed class StatusStateResult : IStatusStateResult
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public int SortSequence { get; set; }

            public string Style { get; set; }

            public int Default { get; set; }
        }
    }
}