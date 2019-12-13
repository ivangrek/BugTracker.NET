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

    internal sealed class UserDefinedAttributeDeletePreviewQueryHandler : IQueryHandler<
        IQuery<IUserDefinedAttributeSource, IUserDefinedAttributeDeletePreviewResult>, IUserDefinedAttributeSource,
        IUserDefinedAttributeDeletePreviewResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public UserDefinedAttributeDeletePreviewQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public IUserDefinedAttributeDeletePreviewResult Handle(
            IQuery<IUserDefinedAttributeSource, IUserDefinedAttributeDeletePreviewResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<UserDefinedAttribute>()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var result = dbQuery
                .Select(x => new UserDefinedAttributeDeletePreviewResult
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .First();

            return result;
        }

        private sealed class UserDefinedAttributeDeletePreviewResult : IUserDefinedAttributeDeletePreviewResult
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}