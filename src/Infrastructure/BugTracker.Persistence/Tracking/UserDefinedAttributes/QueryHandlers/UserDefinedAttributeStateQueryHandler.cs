/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.UserDefinedAttributes.QueryHandlers
{
    using System.Linq;
    using BugTracker.Tracking.Changing.UserDefinedAttributes;
    using BugTracker.Tracking.Querying.UserDefinedAttributes;
    using Querying;
    using Utilities;

    internal sealed class UserDefinedAttributeStateQueryHandler : IQueryHandler<
        IQuery<IUserDefinedAttributeSource, IUserDefinedAttributeStateResult>, IUserDefinedAttributeSource,
        IUserDefinedAttributeStateResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public UserDefinedAttributeStateQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public IUserDefinedAttributeStateResult Handle(
            IQuery<IUserDefinedAttributeSource, IUserDefinedAttributeStateResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<UserDefinedAttribute>()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var result = dbQuery
                .Select(x => new UserDefinedAttributeStateResult
                {
                    Id = x.Id,
                    Name = x.Name,
                    SortSequence = x.SortSequence,
                    Default = x.Default
                })
                .First();

            return result;
        }

        private sealed class UserDefinedAttributeStateResult : IUserDefinedAttributeStateResult
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public int SortSequence { get; set; }

            public int Default { get; set; }
        }
    }
}