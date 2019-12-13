/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Identification.QueryHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using BugTracker.Identification;
    using BugTracker.Identification.Querying;
    using Querying;
    using Querying.Results;
    using Utilities;

    internal sealed class UserComboBoxQueryHandler : IQueryHandler<IQuery<IUserSource, IUserComboBoxResult>, IUserSource, IUserComboBoxResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public UserComboBoxQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public IUserComboBoxResult Handle(IQuery<IUserSource, IUserComboBoxResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<User>()
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

            var result = new UserComboBoxResult(rows);

            return result;
        }

        private sealed class UserComboBoxResult : List<IIdName>, IUserComboBoxResult
        {
            public UserComboBoxResult(IEnumerable<IIdName> rows)
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