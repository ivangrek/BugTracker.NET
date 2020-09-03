namespace BugTracker.Web.Core.Persistence.Configurations
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Models;

    internal sealed class BugConfiguration : IEntityTypeConfiguration<Bug>
    {
        public void Configure(EntityTypeBuilder<Bug> builder)
        {
            builder.ToTable("bugs")
                .HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasColumnName("bg_id");

            builder.Property(x => x.Name)
                .HasColumnName("bg_short_desc")
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.ReportedUserId)
                .HasColumnName("bg_reported_user")
                .IsRequired();

            builder.Property(x => x.ReportedOn)
                .HasColumnName("bg_reported_date")
                .IsRequired();

            builder.Property(x => x.OrganizationId)
                .HasColumnName("bg_org")
                .IsRequired();

            builder.Property(x => x.CategoryId)
                .HasColumnName("bg_category")
                .IsRequired();

            builder.Property(x => x.ProjectId)
                .HasColumnName("bg_project")
                .IsRequired();

            builder.Property(x => x.PriorityId)
                .HasColumnName("bg_priority")
                .IsRequired();

            builder.Property(x => x.StatusId)
                .HasColumnName("bg_status")
                .IsRequired();

            builder.Property(x => x.UserDefinedAttributeId)
                .HasColumnName("bg_user_defined_attribute");

            builder.Property(x => x.AssignedToUserId)
                .HasColumnName("bg_assigned_to_user");

            builder.Property(x => x.UpdatedUserId)
                .HasColumnName("bg_last_updated_user")
                .IsRequired();

            builder.Property(x => x.UpdatedOn)
                .HasColumnName("bg_last_updated_date")
                .IsRequired();

            builder.Property(x => x.UpdatedOn)
                .HasColumnName("bg_project_custom_dropdown_value1")
                .HasMaxLength(120);

            builder.Property(x => x.UpdatedOn)
                .HasColumnName("bg_project_custom_dropdown_value2")
                .HasMaxLength(120);

            builder.Property(x => x.UpdatedOn)
                .HasColumnName("bg_project_custom_dropdown_value3")
                .HasMaxLength(120);

            builder.Property(x => x.Tags)
                .HasColumnName("bg_tags")
                .HasMaxLength(200);
        }
    }
}