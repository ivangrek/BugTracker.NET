/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Persistence.Configurations
{
    using System.Data.Entity.ModelConfiguration;
    using Models;

    internal sealed class UserDefinedAttributeConfiguration : EntityTypeConfiguration<UserDefinedAttribute>
    {
        public UserDefinedAttributeConfiguration()
        {
            ToTable("user_defined_attribute")
                .HasKey(x => x.Id);

            Property(x => x.Id)
                .HasColumnName("udf_id");

            Property(x => x.Name)
                .HasColumnName("udf_name")
                .HasMaxLength(60);

            Property(x => x.SortSequence)
                .HasColumnName("udf_sort_seq");

            Property(x => x.Default)
                .HasColumnName("udf_default");
        }
    }
}