/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Organizations
{
    using System.Data.Entity.ModelConfiguration;
    using BugTracker.Tracking.Changing.Organizations;

    internal sealed class OrganizationConfiguration : EntityTypeConfiguration<Organization>
    {
        public OrganizationConfiguration()
        {
            ToTable("orgs")
                .HasKey(x => x.Id);

            Property(x => x.Id)
                .HasColumnName("og_id");

            Property(x => x.Name)
                .HasColumnName("og_name")
                .HasMaxLength(80);

            Property(x => x.Domain)
                .HasColumnName("og_domain")
                .HasMaxLength(80);

            Property(x => x.Active)
                .HasColumnName("og_active");

            Property(x => x.NonAdminsCanUse)
                .HasColumnName("og_non_admins_can_use");

            Property(x => x.ExternalUser)
                .HasColumnName("og_external_user");

            Property(x => x.CanBeAssignedTo)
                .HasColumnName("og_can_be_assigned_to");

            Property(x => x.CanOnlySeeOwnReported)
                .HasColumnName("og_can_only_see_own_reported");

            Property(x => x.CanEditSql)
                .HasColumnName("og_can_edit_sql");

            Property(x => x.CanDeleteBug)
                .HasColumnName("og_can_delete_bug");

            Property(x => x.CanEditAndDeletePosts)
                .HasColumnName("og_can_edit_and_delete_posts");

            Property(x => x.CanMergeBugs)
                .HasColumnName("og_can_merge_bugs");

            Property(x => x.CanMassEditBugs)
                .HasColumnName("og_can_mass_edit_bugs");

            Property(x => x.CanUseReports)
                .HasColumnName("og_can_use_reports");

            Property(x => x.CanEditReports)
                .HasColumnName("og_can_edit_reports");

            Property(x => x.CanViewTasks)
                .HasColumnName("og_can_view_tasks");

            Property(x => x.CanEditTasks)
                .HasColumnName("og_can_edit_tasks");

            Property(x => x.CanSearch)
                .HasColumnName("og_can_search");

            Property(x => x.OtherOrgsPermissionLevel)
                .HasColumnName("og_other_orgs_permission_level");

            Property(x => x.CanAssignToInternalUsers)
                .HasColumnName("og_can_assign_to_internal_users");

            Property(x => x.CategoryFieldPermissionLevel)
                .HasColumnName("og_category_field_permission_level");

            Property(x => x.PriorityFieldPermissionLevel)
                .HasColumnName("og_priority_field_permission_level");

            Property(x => x.AssignedToFieldPermissionLevel)
                .HasColumnName("og_assigned_to_field_permission_level");

            Property(x => x.StatusFieldPermissionLevel)
                .HasColumnName("og_status_field_permission_level");

            Property(x => x.ProjectFieldPermissionLevel)
                .HasColumnName("og_project_field_permission_level");

            Property(x => x.OrgFieldPermissionLevel)
                .HasColumnName("og_org_field_permission_level");

            Property(x => x.UdfFieldPermissionLevel)
                .HasColumnName("og_udf_field_permission_level");

            Property(x => x.TagsFieldPermissionLevel)
                .HasColumnName("og_tags_field_permission_level");
        }
    }
}