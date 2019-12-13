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

    internal sealed class PriorityStateQueryHandler : IQueryHandler<IQuery<IPrioritySource, IPriorityStateResult>,
        IPrioritySource, IPriorityStateResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public PriorityStateQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public IPriorityStateResult Handle(IQuery<IPrioritySource, IPriorityStateResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<Priority>()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var result = dbQuery
                .Select(x => new PriorityStateResult
                {
                    Id = x.Id,
                    Name = x.Name,
                    BackgroundColor = x.BackgroundColor,
                    SortSequence = x.SortSequence,
                    Style = x.Style,
                    Default = x.Default
                })
                .First();

            return result;
        }

        private sealed class PriorityStateResult : IPriorityStateResult
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string BackgroundColor { get; set; }

            public int SortSequence { get; set; }

            public string Style { get; set; }

            public int Default { get; set; }
        }
    }
}