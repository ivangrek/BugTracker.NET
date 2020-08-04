/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Models.Organization
{
    using System.ComponentModel.DataAnnotations;
    using Core;
    using Core.Identification;

    public sealed class EditModel
    {
        public int Id { get; set; }

        [Display(Name = "Organization Name")]
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; }

        [Display(Name = "Domain (like, \"example.com\")")]
        public string Domain { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; }

        [Display(Name = "Permission level for bugs associated with other (or no) organizations")]
        public SecurityPermissionLevel OtherOrgsPermissionLevel { get; set; }

        [Display(Name = "Can search")]
        public bool CanSearch { get; set; }

        [Display(Name = "External users")]
        public bool ExternalUser { get; set; }

        [Display(Name = "Can see only own reported")]
        public bool CanOnlySeeOwnReported { get; set; }

        [Display(Name = "Members of this org appear in \"assigned to\" dropdown in edit bug page")]
        public bool CanBeAssignedTo { get; set; }

        [Display(Name = "Non-admin with permission to add users can add users to this org")]
        public bool NonAdminsCanUse { get; set; }

        [Display(Name = "\"Project\" field permission")]
        public SecurityPermissionLevel ProjectFieldPermissionLevel { get; set; }

        [Display(Name = "\"Organization\" field permission")]
        public SecurityPermissionLevel OrgFieldPermissionLevel { get; set; }

        [Display(Name = "\"Category\" field permission")]
        public SecurityPermissionLevel CategoryFieldPermissionLevel { get; set; }

        [Display(Name = "\"Priority\" field permission")]
        public SecurityPermissionLevel PriorityFieldPermissionLevel { get; set; }

        [Display(Name = "\"Status\" field permission")]
        public SecurityPermissionLevel StatusFieldPermissionLevel { get; set; }

        [Display(Name = "\"Assigned To\" field permission")]
        public SecurityPermissionLevel AssignedToFieldPermissionLevel { get; set; }

        [Display(Name = "\"User Defined Attribute\" field permission")]
        public SecurityPermissionLevel UdfFieldPermissionLevel { get; set; }

        [Display(Name = "\"Tags\" field permission")]
        public SecurityPermissionLevel TagsFieldPermissionLevel { get; set; }

        [Display(Name = "Can edit sql and create/edit queries for everybody")]
        public bool CanEditSql { get; set; }

        [Display(Name = "Can delete bugs")]
        public bool CanDeleteBug { get; set; }

        [Display(Name = "Can edit and delete comments and attachments")]
        public bool CanEditAndDeletePosts { get; set; }

        [Display(Name = "Can merge two bugs into one")]
        public bool CanMergeBugs { get; set; }

        [Display(Name = "Can mass edit bugs on search page")]
        public bool CanMassEditBugs { get; set; }

        [Display(Name = "Can use reports")] public bool CanUseReports { get; set; }

        [Display(Name = "Can create/edit reports")]
        public bool CanEditReports { get; set; }

        [Display(Name = "Can view tasks/time")]
        public bool CanViewTasks { get; set; }

        [Display(Name = "Can edit tasks/time")]
        public bool CanEditTasks { get; set; }

        [Display(Name = "Can assign to internal users (even if external org)")]
        public bool CanAssignToInternalUsers { get; set; }
    }
}