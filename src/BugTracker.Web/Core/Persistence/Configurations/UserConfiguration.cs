/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Persistence.Configurations
{
    using System.Data.Entity.ModelConfiguration;
    using Models;

    internal sealed class UserConfiguration : EntityTypeConfiguration<User>
    {
        public UserConfiguration()
        {
            ToTable("users")
                .HasKey(x => x.Id);

            Property(x => x.Id)
                .HasColumnName("us_id");

            Property(x => x.Name)
                .HasColumnName("us_username")
                .HasMaxLength(40);

            Property(x => x.Password)
                .HasColumnName("us_password")
                .HasMaxLength(64);

            Property(x => x.Salt)
                .HasColumnName("us_salt");

            Property(x => x.FirstName)
                .HasColumnName("us_firstname")
                .HasMaxLength(60);

            Property(x => x.LastName)
                .HasColumnName("us_lastname")
                .HasMaxLength(60);

            Property(x => x.Email)
                .HasColumnName("us_email")
                .HasMaxLength(120);

            Property(x => x.Admin)
                .HasColumnName("us_admin");

            Property(x => x.DefaultQueryId)
                .HasColumnName("us_default_query");

            Property(x => x.EnableNotifications)
               .HasColumnName("us_enable_notifications");

            Property(x => x.AutoSubscribe)
               .HasColumnName("us_auto_subscribe");

            Property(x => x.AutoSubscribeOwnBugs)
              .HasColumnName("us_auto_subscribe_own_bugs");

            Property(x => x.AutoSubscribeReportedBugs)
              .HasColumnName("us_auto_subscribe_reported_bugs");

            Property(x => x.SendNotificationsToSelf)
              .HasColumnName("us_send_notifications_to_self");

            Property(x => x.Active)
              .HasColumnName("us_active");

            Property(x => x.BugsPerPage)
              .HasColumnName("us_bugs_per_page");

            Property(x => x.ForcedProject)
              .HasColumnName("us_forced_project");

            Property(x => x.ReportedNotifications)
              .HasColumnName("us_reported_notifications");

            Property(x => x.AssignedNotifications)
              .HasColumnName("us_assigned_notifications");

            Property(x => x.SubscribedNotifications)
              .HasColumnName("us_subscribed_notifications");

            Property(x => x.Signature)
              .HasColumnName("us_signature")
              .HasMaxLength(1000);

            Property(x => x.UseFckeditor)
              .HasColumnName("us_use_fckeditor");

            Property(x => x.EnableBugListPopups)
              .HasColumnName("us_enable_bug_list_popups");

            Property(x => x.CreatedUserId)
              .HasColumnName("us_created_user");

            Property(x => x.OrganisationId)
              .HasColumnName("us_org");

            Property(x => x.MostRecentLoginDateTime)
              .HasColumnName("us_most_recent_login_datetime");
        }
    }
}