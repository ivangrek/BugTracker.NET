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

    internal sealed class ProjectStateQueryHandler : IQueryHandler<IQuery<IProjectSource, IProjectStateResult>,
        IProjectSource, IProjectStateResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public ProjectStateQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public IProjectStateResult Handle(IQuery<IProjectSource, IProjectStateResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<Project>()
                .AsNoTracking()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var result = dbQuery
                .Select(x => new ProjectStateResult
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    DefaultUserId = x.DefaultUserId,
                    AutoAssignDefaultUser = x.AutoAssignDefaultUser,
                    AutoSubscribeDefaultUser = x.AutoSubscribeDefaultUser,
                    EnablePop3 = x.EnablePop3,
                    Pop3Username = x.Pop3Username,
                    Pop3Password = x.Pop3Password,
                    Pop3EmailFrom = x.Pop3EmailFrom,
                    Active = x.Active,
                    Default = x.Default,
                    EnableCustomDropdown1 = x.EnableCustomDropdown1,
                    CustomDropdown1Label = x.CustomDropdown1Label,
                    CustomDropdown1Values = x.CustomDropdown1Values,
                    EnableCustomDropdown2 = x.EnableCustomDropdown2,
                    CustomDropdown2Label = x.CustomDropdown2Label,
                    CustomDropdown2Values = x.CustomDropdown2Values,
                    EnableCustomDropdown3 = x.EnableCustomDropdown3,
                    CustomDropdown3Label = x.CustomDropdown3Label,
                    CustomDropdown3Values = x.CustomDropdown3Values
                })
                .First();

            return result;
        }

        private sealed class ProjectStateResult : IProjectStateResult
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string Description { get; set; }

            public int? DefaultUserId { get; set; }

            public int? AutoAssignDefaultUser { get; set; }

            public int? AutoSubscribeDefaultUser { get; set; }

            public int? EnablePop3 { get; set; }

            public string Pop3Username { get; set; }

            public string Pop3Password { get; set; }

            public string Pop3EmailFrom { get; set; }

            public int Active { get; set; }

            public int Default { get; set; }

            public int? EnableCustomDropdown1 { get; set; }

            public string CustomDropdown1Label { get; set; }

            public string CustomDropdown1Values { get; set; }

            public int? EnableCustomDropdown2 { get; set; }

            public string CustomDropdown2Label { get; set; }

            public string CustomDropdown2Values { get; set; }

            public int? EnableCustomDropdown3 { get; set; }

            public string CustomDropdown3Label { get; set; }

            public string CustomDropdown3Values { get; set; }
        }
    }
}