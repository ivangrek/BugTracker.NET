/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Priorities
{
    using System.Data.Entity.ModelConfiguration;
    using BugTracker.Tracking.Changing.Priorities;

    internal sealed class PriorityConfiguration : EntityTypeConfiguration<Priority>
    {
        public PriorityConfiguration()
        {
            ToTable("priorities")
                .HasKey(x => x.Id);

            Property(x => x.Id)
                .HasColumnName("pr_id");

            Property(x => x.Name)
                .HasColumnName("pr_name")
                .HasMaxLength(60);

            Property(x => x.BackgroundColor)
                .HasColumnName("pr_background_color")
                .HasMaxLength(14);

            Property(x => x.SortSequence)
                .HasColumnName("pr_sort_seq");

            Property(x => x.Style)
                .HasColumnName("pr_style")
                .HasMaxLength(30);

            Property(x => x.Default)
                .HasColumnName("pr_default");
        }
    }
}