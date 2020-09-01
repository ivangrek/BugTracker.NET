namespace BugTracker.Web.Core.Persistence.Configurations
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Models;

    internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder.ToTable("users")
                .HasKey(x => x.Id);

            builder.HasIndex(x => x.Username)
                .IsUnique();

            builder.Property(x => x.Id)
                .HasColumnName("us_id");

            builder.Property(x => x.Username)
                .HasColumnName("us_username")
                .IsRequired()
                .HasMaxLength(40);

            builder.Property(x => x.Salt)
                .HasColumnName("us_salt")
                .HasMaxLength(200);

            builder.Property(x => x.Password)
                .HasColumnName("us_password")
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.PasswordResetKey)
                .HasColumnName("password_reset_key")
                .HasMaxLength(200);

            builder.Property(x => x.FirstName)
                .HasColumnName("us_firstname")
                .HasMaxLength(60);

            builder.Property(x => x.LastName)
                .HasColumnName("us_lastname")
                .HasMaxLength(60);

            builder.Property(x => x.Email)
                .HasColumnName("us_email")
                .HasMaxLength(120);

            builder.Property(x => x.Admin)
                .HasColumnName("us_admin")
                .HasDefaultValue(0);

            builder.Property(x => x.DefaultQueryId)
                .HasColumnName("us_default_query")
                .HasDefaultValue(0);

            builder.Property(x => x.EnableNotifications)
               .HasColumnName("us_enable_notifications")
               .HasDefaultValue(1);

            builder.Property(x => x.AutoSubscribe)
               .HasColumnName("us_auto_subscribe")
               .HasDefaultValue(0);

            builder.Property(x => x.AutoSubscribeOwnBugs)
              .HasColumnName("us_auto_subscribe_own_bugs")
              .HasDefaultValue(0);

            builder.Property(x => x.AutoSubscribeReportedBugs)
              .HasColumnName("us_auto_subscribe_reported_bugs")
              .HasDefaultValue(0);

            builder.Property(x => x.SendNotificationsToSelf)
              .HasColumnName("us_send_notifications_to_self")
              .HasDefaultValue(0);

            builder.Property(x => x.Active)
              .HasColumnName("us_active")
              .IsRequired()
              .HasDefaultValue(1);

            builder.Property(x => x.BugsPerPage)
              .HasColumnName("us_bugs_per_page");

            builder.Property(x => x.ForcedProjectId)
              .HasColumnName("us_forced_project");

            builder.Property(x => x.ReportedNotifications)
              .HasColumnName("us_reported_notifications")
              .IsRequired()
              .HasDefaultValue(4);

            builder.Property(x => x.AssignedNotifications)
              .HasColumnName("us_assigned_notifications")
              .IsRequired()
              .HasDefaultValue(4);

            builder.Property(x => x.SubscribedNotifications)
              .HasColumnName("us_subscribed_notifications")
              .IsRequired()
              .HasDefaultValue(4);

            builder.Property(x => x.Signature)
              .HasColumnName("us_signature")
              .HasMaxLength(1000);

            builder.Property(x => x.UseFckeditor)
              .HasColumnName("us_use_fckeditor")
              .IsRequired()
              .HasDefaultValue(0);

            builder.Property(x => x.EnableBugListPopups)
              .HasColumnName("us_enable_bug_list_popups")
              .IsRequired()
              .HasDefaultValue(1);

            builder.Property(x => x.CreatedUserId)
              .HasColumnName("us_created_user")
              .IsRequired()
              .HasDefaultValue(1);

            builder.Property(x => x.OrganizationId)
              .HasColumnName("us_org")
              .IsRequired()
              .HasDefaultValue(0);

            builder.Property(x => x.MostRecentLoginDateTime)
              .HasColumnName("us_most_recent_login_datetime");
        }
    }
}