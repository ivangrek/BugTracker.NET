/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.UserDefinedAttributes.QueryHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using BugTracker.Tracking.Changing.UserDefinedAttributes;
    using BugTracker.Tracking.Querying.UserDefinedAttributes;
    using Querying;
    using Utilities;

    internal sealed class UserDefinedAttributeListQueryHandler : IQueryHandler<
        IQuery<IUserDefinedAttributeSource, IUserDefinedAttributeListResult>, IUserDefinedAttributeSource,
        IUserDefinedAttributeListResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public UserDefinedAttributeListQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public IUserDefinedAttributeListResult Handle(
            IQuery<IUserDefinedAttributeSource, IUserDefinedAttributeListResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<UserDefinedAttribute>()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var rows = dbQuery
                .Select(x => new UserDefinedAttributeListRow
                {
                    Id = x.Id,
                    Name = x.Name,
                    SortSequence = x.SortSequence,
                    Default = x.Default
                })
                .ToArray();

            var result = new UserDefinedAttributeListResult(rows);

            return result;
        }

        private sealed class UserDefinedAttributeListResult : List<IUserDefinedAttributeListRow>,
            IUserDefinedAttributeListResult
        {
            public UserDefinedAttributeListResult(IEnumerable<IUserDefinedAttributeListRow> rows)
                : base(rows)
            {
            }
        }

        private sealed class UserDefinedAttributeListRow : IUserDefinedAttributeListRow
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public int SortSequence { get; set; }

            public int Default { get; set; }
        }
    }
}