/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Bugs
{
    using System.Data.Entity.ModelConfiguration;
    using BugTracker.Tracking.Changing.Bugs;

    internal sealed class BugConfiguration : EntityTypeConfiguration<Bug>
    {
        public BugConfiguration()
        {
            ToTable("bugs")
                .HasKey(x => x.Id);

            Property(x => x.Id)
                .HasColumnName("bg_id");

            Property(x => x.OrganizationId)
                .HasColumnName("bg_org");

            Property(x => x.CategoryId)
                .HasColumnName("bg_category");

            Property(x => x.ProjectId)
                .HasColumnName("bg_project");

            Property(x => x.PriorityId)
                .HasColumnName("bg_priority");

            Property(x => x.StatusId)
                .HasColumnName("bg_status");

            Property(x => x.UserDefinedAttributeId)
                .HasColumnName("bg_user_defined_attribute");
        }
    }
}