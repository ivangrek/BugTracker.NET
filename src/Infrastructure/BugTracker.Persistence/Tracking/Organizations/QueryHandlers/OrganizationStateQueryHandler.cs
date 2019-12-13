/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Organizations.QueryHandlers
{
    using System.Linq;
    using BugTracker.Tracking.Changing.Organizations;
    using BugTracker.Tracking.Querying.Organizations;
    using Querying;
    using Utilities;

    internal sealed class OrganizationStateQueryHandler : IQueryHandler<IQuery<IOrganizationSource, IOrganizationStateResult>, IOrganizationSource, IOrganizationStateResult>
    {
        private readonly ApplicationDbContext applicationDbContext;

        public OrganizationStateQueryHandler(
            ApplicationDbContext applicationDbContext)
        {
            this.applicationDbContext = applicationDbContext;
        }

        public IOrganizationStateResult Handle(IQuery<IOrganizationSource, IOrganizationStateResult> query)
        {
            var dbQuery = this.applicationDbContext
                .Set<Organization>()
                .AsNoTracking()
                .AsQueryable()
                .ApplyQueryFilter(query.Filter)
                .ApplyQuerySorter(query.Sorter)
                .ApplyQueryPager(query.Pager);

            var result = dbQuery
                .Select(x => new OrganizationStateResult
                {
                    Id = x.Id,
                    Name = x.Name,
                    Domain = x.Domain,
                    Active = x.Active,
                    NonAdminsCanUse = x.NonAdminsCanUse,
                    ExternalUser = x.ExternalUser,
                    CanBeAssignedTo = x.CanBeAssignedTo,
                    CanOnlySeeOwnReported = x.CanOnlySeeOwnReported,
                    CanEditSql = x.CanEditSql,
                    CanDeleteBug = x.CanDeleteBug,
                    CanEditAndDeletePosts = x.CanEditAndDeletePosts,
                    CanMergeBugs = x.CanMergeBugs,
                    CanMassEditBugs = x.CanMassEditBugs,
                    CanUseReports = x.CanUseReports,
                    CanEditReports = x.CanEditReports,
                    CanViewTasks = x.CanViewTasks,
                    CanEditTasks = x.CanEditTasks,
                    CanSearch = x.CanSearch,
                    OtherOrgsPermissionLevel = x.OtherOrgsPermissionLevel,
                    CanAssignToInternalUsers = x.CanAssignToInternalUsers,
                    CategoryFieldPermissionLevel = x.CategoryFieldPermissionLevel,
                    PriorityFieldPermissionLevel = x.PriorityFieldPermissionLevel,
                    AssignedToFieldPermissionLevel = x.AssignedToFieldPermissionLevel,
                    StatusFieldPermissionLevel = x.StatusFieldPermissionLevel,
                    ProjectFieldPermissionLevel = x.ProjectFieldPermissionLevel,
                    OrgFieldPermissionLevel = x.OrgFieldPermissionLevel,
                    UdfFieldPermissionLevel = x.UdfFieldPermissionLevel,
                    TagsFieldPermissionLevel = x.TagsFieldPermissionLevel,
                    FieldPermissionLevel = x.FieldPermissionLevel
                })
                .First();

            return result;
        }

        private sealed class OrganizationStateResult : IOrganizationStateResult
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string Domain { get; set; }

            public int Active { get; set; }

            public int NonAdminsCanUse { get; set; }

            public int ExternalUser { get; set; }

            public int CanBeAssignedTo { get; set; }

            public int CanOnlySeeOwnReported { get; set; }

            public int CanEditSql { get; set; }

            public int CanDeleteBug { get; set; }

            public int CanEditAndDeletePosts { get; set; }

            public int CanMergeBugs { get; set; }

            public int CanMassEditBugs { get; set; }

            public int CanUseReports { get; set; }

            public int CanEditReports { get; set; }

            public int CanViewTasks { get; set; }

            public int CanEditTasks { get; set; }

            public int CanSearch { get; set; }

            public int OtherOrgsPermissionLevel { get; set; }

            public int CanAssignToInternalUsers { get; set; }

            public int CategoryFieldPermissionLevel { get; set; }

            public int PriorityFieldPermissionLevel { get; set; }

            public int AssignedToFieldPermissionLevel { get; set; }

            public int StatusFieldPermissionLevel { get; set; }

            public int ProjectFieldPermissionLevel { get; set; }

            public int OrgFieldPermissionLevel { get; set; }

            public int UdfFieldPermissionLevel { get; set; }

            public int TagsFieldPermissionLevel { get; set; }

            public int? FieldPermissionLevel { get; set; }
        }
    }
}