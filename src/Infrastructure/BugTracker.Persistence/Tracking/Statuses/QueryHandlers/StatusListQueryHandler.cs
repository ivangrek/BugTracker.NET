/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Statuses.QueryHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using BugTracker.Tracking.Changing.Statuses;
    using BugTracker.Tracking.Querying.Statuses;
    using Querying;
    using Utilities;

    internal sealed class
        StatusListQueryHandler : IQueryHandler<IQuery<IStatusSource, IStatusListResult>, IStatusSource,
            IStatusListResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public StatusListQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public IStatusListResult Handle(IQuery<IStatusSource, IStatusListResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<Status>()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var rows = dbQuery
                .Select(x => new StatusListRow
                {
                    Id = x.Id,
                    Name = x.Name,
                    SortSequence = x.SortSequence,
                    Style = x.Style,
                    Default = x.Default
                })
                .ToArray();

            var result = new StatusListResult(rows);

            return result;
        }

        private sealed class StatusListResult : List<IStatusListRow>, IStatusListResult
        {
            public StatusListResult(IEnumerable<IStatusListRow> rows)
                : base(rows)
            {
            }
        }

        private sealed class StatusListRow : IStatusListRow
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public int SortSequence { get; set; }

            public string Style { get; set; }

            public int Default { get; set; }
        }
    }
}