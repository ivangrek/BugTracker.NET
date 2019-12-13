/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Projects.QueryHandlers
{
    using System.Linq;
    using BugTracker.Tracking.Changing.Projects;
    using BugTracker.Tracking.Querying.Projects;
    using Querying;
    using Utilities;

    internal sealed class ProjectDeletePreviewQueryHandler : IQueryHandler<
        IQuery<IProjectSource, IProjectDeletePreviewResult>, IProjectSource, IProjectDeletePreviewResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public ProjectDeletePreviewQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public IProjectDeletePreviewResult Handle(IQuery<IProjectSource, IProjectDeletePreviewResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<Project>()
                .AsNoTracking()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var result = dbQuery
                .Select(x => new ProjectDeletePreviewResult
                {
                    Id = x.Id,
                    Name = x.Name
                })
                .First();

            return result;
        }

        private sealed class ProjectDeletePreviewResult : IProjectDeletePreviewResult
        {
            public int Id { get; set; }

            public string Name { get; set; }
        }
    }
}