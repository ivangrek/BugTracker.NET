/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Projects.QueryHandlers
{
    using System.Collections.Generic;
    using System.Linq;
    using BugTracker.Identification.Querying;
    using BugTracker.Tracking.Changing.Projects;
    using BugTracker.Tracking.Querying.Projects;
    using Querying;
    using Utilities;

    internal sealed class ProjectListQueryHandler : IQueryHandler<IQuery<IProjectSource, IProjectListResult>, IProjectSource, IProjectListResult>
    {
        private readonly ApplicationDbContext applicationDbContext;
        private readonly IApplicationFacade applicationFacade;
        private readonly IQueryBuilder queryBuilder;

        public ProjectListQueryHandler(
            ApplicationDbContext applicationDbContext,
            IApplicationFacade applicationFacade,
            IQueryBuilder queryBuilder)
        {
            this.applicationDbContext = applicationDbContext;
            this.applicationFacade = applicationFacade;
            this.queryBuilder = queryBuilder;
        }

        public IProjectListResult Handle(IQuery<IProjectSource, IProjectListResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<Project>()
                .AsNoTracking()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var userIds = dbQuery
                .Where(x => x.DefaultUserId != null && x.DefaultUserId > 0)
                .Select(x => x.DefaultUserId.Value)
                .ToArray();

            var userResult = new Dictionary<int, string>();

            if (userIds.Length > 0)
            {
                var userQuery = this.queryBuilder
                    .From<IUserSource>()
                    .To<IUserComboBoxResult>()
                    .Filter()
                        .Equal(x => x.Id, userIds[0]) // TODO
                    .Build();

                userResult = this.applicationFacade
                    .Run(userQuery)
                    .ToDictionary(x => x.Id, x => x.Name);
            }

            var rows = dbQuery
                .ToArray()
                .Select(x => new ProjectListRow
                {
                    Id = x.Id,
                    Name = x.Name,
                    Description = x.Description,
                    DefaultUserName = UserName(x.DefaultUserId, userResult),
                    AutoAssignDefaultUser = x.AutoAssignDefaultUser,
                    AutoSubscribeDefaultUser = x.AutoSubscribeDefaultUser,
                    EnablePop3 = x.EnablePop3,
                    Pop3Username = x.Pop3Username,
                    Pop3EmailFrom = x.Pop3EmailFrom,
                    Active = x.Active,
                    Default = x.Default
                });

            var result = new ProjectListResult(rows);

            return result;
        }

        private static string UserName(int? userId, IReadOnlyDictionary<int, string> users)
        {
            if (userId == null)
            {
                return string.Empty;
            }

            if (users.ContainsKey(userId.Value))
            {
                return users[userId.Value];
            }

            return string.Empty;
        }

        private sealed class ProjectListResult : List<IProjectListRow>, IProjectListResult
        {
            public ProjectListResult(IEnumerable<IProjectListRow> rows)
                : base(rows)
            {
            }
        }

        private sealed class ProjectListRow : IProjectListRow
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string Description { get; set; }

            public string DefaultUserName { get; set; }

            public int? AutoAssignDefaultUser { get; set; }

            public int? AutoSubscribeDefaultUser { get; set; }

            public int? EnablePop3 { get; set; }

            public string Pop3Username { get; set; }

            public string Pop3EmailFrom { get; set; }

            public int Active { get; set; }

            public int Default { get; set; }
        }
    }
}