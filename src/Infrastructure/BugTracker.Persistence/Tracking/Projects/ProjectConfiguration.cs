/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Projects
{
    using System.Data.Entity.ModelConfiguration;
    using BugTracker.Tracking.Changing.Projects;

    internal sealed class ProjectConfiguration : EntityTypeConfiguration<Project>
    {
        public ProjectConfiguration()
        {
            ToTable("projects")
                .HasKey(x => x.Id);

            Property(x => x.Id)
                .HasColumnName("pj_id");

            Property(x => x.Name)
                .HasColumnName("pj_name");

            Property(x => x.Description)
                .HasColumnName("pj_description");

            Property(x => x.DefaultUserId)
                .HasColumnName("pj_default_user");

            Property(x => x.AutoAssignDefaultUser)
                .HasColumnName("pj_auto_assign_default_user");

            Property(x => x.AutoSubscribeDefaultUser)
                .HasColumnName("pj_auto_subscribe_default_user");

            Property(x => x.EnablePop3)
                .HasColumnName("pj_enable_pop3");

            Property(x => x.Pop3Username)
                .HasColumnName("pj_pop3_username");

            Property(x => x.Pop3Password)
                .HasColumnName("pj_pop3_password");

            Property(x => x.Pop3EmailFrom)
                .HasColumnName("pj_pop3_email_from");

            Property(x => x.Active)
                .HasColumnName("pj_active");

            Property(x => x.Default)
                .HasColumnName("pj_default");

            Property(x => x.EnableCustomDropdown1)
                .HasColumnName("pj_enable_custom_dropdown1");

            Property(x => x.CustomDropdown1Label)
                .HasColumnName("pj_custom_dropdown_label1");

            Property(x => x.CustomDropdown1Values)
                .HasColumnName("pj_custom_dropdown_values1");

            Property(x => x.EnableCustomDropdown2)
                .HasColumnName("pj_enable_custom_dropdown2");

            Property(x => x.CustomDropdown2Label)
                .HasColumnName("pj_custom_dropdown_label2");

            Property(x => x.CustomDropdown2Values)
                .HasColumnName("pj_custom_dropdown_values2");

            Property(x => x.EnableCustomDropdown3)
                .HasColumnName("pj_enable_custom_dropdown3");

            Property(x => x.CustomDropdown3Label)
                .HasColumnName("pj_custom_dropdown_label3");

            Property(x => x.CustomDropdown3Values)
                .HasColumnName("pj_custom_dropdown_values3");
        }
    }
}