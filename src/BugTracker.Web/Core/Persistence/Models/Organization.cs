/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Persistence.Models
{
    public class Organization
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
    }
}
