namespace BugTracker.Web.Core.Persistence.Configurations
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Models;

    internal sealed class OrganizationConfiguration : IEntityTypeConfiguration<Organization>
    {
        public void Configure(EntityTypeBuilder<Organization> builder)
        {
            builder.ToTable("orgs")
                .HasKey(x => x.Id);

            builder.HasIndex(x => x.Name)
                .IsUnique();

            builder.Property(x => x.Id)
                .HasColumnName("og_id");

            builder.Property(x => x.Name)
                .HasColumnName("og_name")
                .IsRequired()
                .HasMaxLength(80);

            builder.Property(x => x.Domain)
                .HasColumnName("og_domain")
                .HasMaxLength(80);

            builder.Property(x => x.NonAdminsCanUse)
                .HasColumnName("og_non_admins_can_use")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.ExternalUser)
                .HasColumnName("og_external_user")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.CanBeAssignedTo)
                .HasColumnName("og_can_be_assigned_to")
                .IsRequired()
                .HasDefaultValue(1);

            builder.Property(x => x.CanOnlySeeOwnReported)
                .HasColumnName("og_can_only_see_own_reported")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.CanEditSql)
                .HasColumnName("og_can_edit_sql")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.CanDeleteBug)
                .HasColumnName("og_can_delete_bug")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.CanEditAndDeletePosts)
                .HasColumnName("og_can_edit_and_delete_posts")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.CanMergeBugs)
                .HasColumnName("og_can_merge_bugs")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.CanMassEditBugs)
                .HasColumnName("og_can_mass_edit_bugs")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.CanUseReports)
                .HasColumnName("og_can_use_reports")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.CanEditReports)
                .HasColumnName("og_can_edit_reports")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.CanViewTasks)
                .HasColumnName("og_can_view_tasks")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.CanEditTasks)
                .HasColumnName("og_can_edit_tasks")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.CanSearch)
                .HasColumnName("og_can_search")
                .IsRequired()
                .HasDefaultValue(1);

            builder.Property(x => x.OtherOrgsPermissionLevel)
                .HasColumnName("og_other_orgs_permission_level")
                .IsRequired()
                .HasDefaultValue(2);

            builder.Property(x => x.CanAssignToInternalUsers)
                .HasColumnName("og_can_assign_to_internal_users")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.CategoryFieldPermissionLevel)
                .HasColumnName("og_category_field_permission_level")
                .IsRequired()
                .HasDefaultValue(2);

            builder.Property(x => x.PriorityFieldPermissionLevel)
                .HasColumnName("og_priority_field_permission_level")
                .IsRequired()
                .HasDefaultValue(2);

            builder.Property(x => x.AssignedToFieldPermissionLevel)
                .HasColumnName("og_assigned_to_field_permission_level")
                .IsRequired()
                .HasDefaultValue(2);

            builder.Property(x => x.StatusFieldPermissionLevel)
                .HasColumnName("og_status_field_permission_level")
                .IsRequired()
                .HasDefaultValue(2);

            builder.Property(x => x.ProjectFieldPermissionLevel)
                .HasColumnName("og_project_field_permission_level")
                .IsRequired()
                .HasDefaultValue(2);

            builder.Property(x => x.OrgFieldPermissionLevel)
                .HasColumnName("og_org_field_permission_level")
                .IsRequired()
                .HasDefaultValue(2);

            builder.Property(x => x.UdfFieldPermissionLevel)
                .HasColumnName("og_udf_field_permission_level")
                .IsRequired()
                .HasDefaultValue(2);

            builder.Property(x => x.TagsFieldPermissionLevel)
                .HasColumnName("og_tags_field_permission_level")
                .IsRequired()
                .HasDefaultValue(2);

            builder.Property(x => x.Active)
                .HasColumnName("og_active")
                .IsRequired()
                .HasDefaultValue(1);
        }
    }
}
