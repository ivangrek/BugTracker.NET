/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Organizations.QueryHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using BugTracker.Tracking.Changing.Organizations;
    using BugTracker.Tracking.Querying.Organizations;
    using Querying;
    using Querying.Results;
    using Utilities;

    internal sealed class OrganizationComboBoxQueryHandler : IQueryHandler<IQuery<IOrganizationSource, IOrganizationComboBoxResult>, IOrganizationSource, IOrganizationComboBoxResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public OrganizationComboBoxQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public IOrganizationComboBoxResult Handle(IQuery<IOrganizationSource, IOrganizationComboBoxResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<Organization>()
                .AsNoTracking()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var rows = dbQuery
                .Select(x => new IdName
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .ToArray();

            var result = new OrganizationComboBoxResult(rows);

            return result;
        }

        private sealed class OrganizationComboBoxResult : List<IIdName>, IOrganizationComboBoxResult
        {
            public OrganizationComboBoxResult(IEnumerable<IIdName> rows)
                : base(rows)
            {
            }
        }

        private sealed class IdName : IIdName
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}