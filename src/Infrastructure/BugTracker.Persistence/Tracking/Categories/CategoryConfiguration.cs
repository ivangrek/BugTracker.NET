/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence.Tracking.Categories
{
    using System.Data.Entity.ModelConfiguration;
    using BugTracker.Tracking.Changing.Categories;

    internal sealed class CategoryConfiguration : EntityTypeConfiguration<Category>
    {
        public CategoryConfiguration()
        {
            ToTable("categories")
                .HasKey(x => x.Id);

            Property(x => x.Id)
                .HasColumnName("ct_id");

            Property(x => x.Name)
                .HasColumnName("ct_name")
                .HasMaxLength(60);

            Property(x => x.SortSequence)
                .HasColumnName("ct_sort_seq");

            Property(x => x.Default)
                .HasColumnName("ct_default");
        }
    }
}