/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Categories.QueryHandlers
{
    using System.Linq;
    using BugTracker.Tracking.Changing.Categories;
    using BugTracker.Tracking.Querying.Categories;
    using Querying;
    using Utilities;

    internal sealed class
        CategoryStateQueryHandler : IQueryHandler<IQuery<ICategorySource, ICategoryStateResult>, ICategorySource, ICategoryStateResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public CategoryStateQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public ICategoryStateResult Handle(IQuery<ICategorySource, ICategoryStateResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<Category>()
                .AsNoTracking()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var result = dbQuery
                .Select(x => new CategoryStateResult
                {
                    Id = x.Id,
                    Name = x.Name,
                    SortSequence = x.SortSequence,
                    Default = x.Default
                })
                .First();

            return result;
        }

        private sealed class CategoryStateResult : ICategoryStateResult
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public int SortSequence { get; set; }

            public int Default { get; set; }
        }
    }
}