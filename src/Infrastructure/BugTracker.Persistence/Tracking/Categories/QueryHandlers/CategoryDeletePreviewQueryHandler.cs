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

    internal sealed class CategoryDeletePreviewQueryHandler : IQueryHandler<
        IQuery<ICategorySource, ICategoryDeletePreviewResult>, ICategorySource, ICategoryDeletePreviewResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public CategoryDeletePreviewQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public ICategoryDeletePreviewResult Handle(IQuery<ICategorySource, ICategoryDeletePreviewResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<Category>()
                .AsNoTracking()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var result = dbQuery
                .Select(x => new CategoryDeletePreviewResult
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .First();

            return result;
        }

        private sealed class CategoryDeletePreviewResult : ICategoryDeletePreviewResult
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}