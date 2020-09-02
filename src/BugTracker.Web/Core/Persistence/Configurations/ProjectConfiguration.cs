namespace BugTracker.Web.Core.Persistence.Configurations
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Models;

    internal sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
    {
        public void Configure(EntityTypeBuilder<Project> builder)
        {
            builder.ToTable("projects")
                .HasKey(x => x.Id);

            builder.HasIndex(x => x.Name)
                .IsUnique();

            builder.Property(x => x.Id)
                .HasColumnName("pj_id");

            builder.Property(x => x.Name)
                .HasColumnName("pj_name")
                .IsRequired()
                .HasMaxLength(80);

            builder.Property(x => x.Active)
                .HasColumnName("pj_active")
                .IsRequired()
                .HasDefaultValue(1);

            builder.Property(x => x.DefaultUserId)
                .HasColumnName("pj_default_user");

            builder.Property(x => x.AutoAssignDefaultUser)
                .HasColumnName("pj_auto_assign_default_user");

            builder.Property(x => x.AutoSubscribeDefaultUser)
                .HasColumnName("pj_auto_subscribe_default_user");

            builder.Property(x => x.EnablePop3)
                .HasColumnName("pj_enable_pop3");

            builder.Property(x => x.Pop3Username)
                .HasColumnName("pj_pop3_username")
                .HasMaxLength(50);

            builder.Property(x => x.Pop3Password)
                .HasColumnName("pj_pop3_password")
                .HasMaxLength(20);

            builder.Property(x => x.Pop3EmailFrom)
                .HasColumnName("pj_pop3_email_from")
                .HasMaxLength(120);

            builder.Property(x => x.EnableCustomDropdown1)
                .HasColumnName("pj_enable_custom_dropdown1")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.EnableCustomDropdown2)
                .HasColumnName("pj_enable_custom_dropdown2")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.EnableCustomDropdown3)
                .HasColumnName("pj_enable_custom_dropdown3")
                .IsRequired()
                .HasDefaultValue(0);

            builder.Property(x => x.CustomDropdown1Label)
                .HasColumnName("pj_custom_dropdown_label1")
                .HasMaxLength(80);

            builder.Property(x => x.CustomDropdown2Label)
                .HasColumnName("pj_custom_dropdown_label2")
                .HasMaxLength(80);

            builder.Property(x => x.CustomDropdown3Label)
                .HasColumnName("pj_custom_dropdown_label3")
                .HasMaxLength(80);

            builder.Property(x => x.CustomDropdown1Values)
                .HasColumnName("pj_custom_dropdown_values1")
                .HasMaxLength(800);

            builder.Property(x => x.CustomDropdown2Values)
                .HasColumnName("pj_custom_dropdown_values2")
                .HasMaxLength(800);

            builder.Property(x => x.CustomDropdown3Values)
                .HasColumnName("pj_custom_dropdown_values3")
                .HasMaxLength(800);

            builder.Property(x => x.Default)
               .HasColumnName("pj_default")
               .IsRequired()
               .HasDefaultValue(0);

            builder.Property(x => x.Description)
                .HasColumnName("pj_description")
                .HasMaxLength(200);
        }
    }
}