/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Categories.QueryHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using BugTracker.Tracking.Changing.Categories;
    using BugTracker.Tracking.Querying.Categories;
    using Querying;
    using Utilities;

    internal sealed class CategoryListQueryHandler : IQueryHandler<IQuery<ICategorySource, ICategoryListResult>, ICategorySource, ICategoryListResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public CategoryListQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public ICategoryListResult Handle(IQuery<ICategorySource, ICategoryListResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<Category>()
                .AsNoTracking()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var rows = dbQuery
                .Select(x => new CategoryListRow
                {
                    Id = x.Id,
                    Name = x.Name,
                    SortSequence = x.SortSequence,
                    Default = x.Default
                })
                .ToArray();

            var result = new CategoryListResult(rows);

            return result;
        }

        private sealed class CategoryListResult : List<ICategoryListRow>, ICategoryListResult
        {
            public CategoryListResult(IEnumerable<ICategoryListRow> rows)
                : base(rows)
            {
            }
        }

        private sealed class CategoryListRow : ICategoryListRow
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public int SortSequence { get; set; }

            public int Default { get; set; }
        }
    }
}