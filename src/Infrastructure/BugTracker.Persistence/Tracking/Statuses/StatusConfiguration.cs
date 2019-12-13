/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Statuses
{
    using System.Data.Entity.ModelConfiguration;
    using BugTracker.Tracking.Changing.Statuses;

    internal sealed class StatusConfiguration : EntityTypeConfiguration<Status>
    {
        public StatusConfiguration()
        {
            ToTable("statuses")
                .HasKey(x => x.Id);

            Property(x => x.Id)
                .HasColumnName("st_id");

            Property(x => x.Name)
                .HasColumnName("st_name")
                .HasMaxLength(60);

            Property(x => x.SortSequence)
                .HasColumnName("st_sort_seq");

            Property(x => x.Style)
                .HasColumnName("st_style")
                .HasMaxLength(30);

            Property(x => x.Default)
                .HasColumnName("st_default");
        }
    }
}