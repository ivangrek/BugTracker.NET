/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Priorities.QueryHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using BugTracker.Tracking.Changing.Priorities;
    using BugTracker.Tracking.Querying.Priorities;
    using Querying;
    using Utilities;

    internal sealed class
        PriorityListQueryHandler : IQueryHandler<IQuery<IPrioritySource, IPriorityListResult>, IPrioritySource,
            IPriorityListResult> //IQueryHandler<IPriorityListQuery, IPrioritySource, IPriorityListResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public PriorityListQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public IPriorityListResult Handle(IQuery<IPrioritySource, IPriorityListResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<Priority>()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var rows = dbQuery
                .Select(x => new PriorityListRow
                {
                    Id = x.Id,
                    Name = x.Name,
                    BackgroundColor = x.BackgroundColor,
                    SortSequence = x.SortSequence,
                    Style = x.Style,
                    Default = x.Default
                })
                .ToArray();

            var result = new PriorityListResult(rows);

            return result;
        }

        private sealed class PriorityListResult : List<IPriorityListRow>, IPriorityListResult
        {
            public PriorityListResult(IEnumerable<IPriorityListRow> rows)
                : base(rows)
            {
            }
        }

        private sealed class PriorityListRow : IPriorityListRow
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