/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Tracking.Querying.Organizations
{
    using System.Collections.Generic;
    using BugTracker.Querying;

    public interface IOrganizationListResult : IResult, IEnumerable<IOrganizationListRow>
    {
    }

    public interface IOrganizationListRow
    {
        int Id { get; }

        string Name { get; }

        string Domain { get; }

        int Active { get; }

        int NonAdminsCanUse { get; }

        int ExternalUser { get; }

        int CanBeAssignedTo { get; }

        int CanOnlySeeOwnReported { get; }

        int CanEditSql { get; }

        int CanDeleteBug { get; }

        int CanEditAndDeletePosts { get; }

        int CanMergeBugs { get; }

        int CanMassEditBugs { get; }

        int CanUseReports { get; }

        int CanEditReports { get; }

        int CanViewTasks { get; }

        int CanEditTasks { get; }

        int CanSearch { get; }

        int OtherOrgsPermissionLevel { get; }

        int CanAssignToInternalUsers { get; }

        int CategoryFieldPermissionLevel { get; }

        int PriorityFieldPermissionLevel { get; }

        int AssignedToFieldPermissionLevel { get; }

        int StatusFieldPermissionLevel { get; }

        int ProjectFieldPermissionLevel { get; }

        int OrgFieldPermissionLevel { get; }

        int UdfFieldPermissionLevel { get; }

        int TagsFieldPermissionLevel { get; }
    }
}