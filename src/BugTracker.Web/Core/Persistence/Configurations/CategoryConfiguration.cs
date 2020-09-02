namespace BugTracker.Web.Core.Persistence.Configurations
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Models;

    internal sealed class CategoryConfiguration : IEntityTypeConfiguration<Category>
    {
        public void Configure(EntityTypeBuilder<Category> builder)
        {
            builder.ToTable("categories")
                .HasKey(x => x.Id);

            builder.HasIndex(x => x.Name)
                .IsUnique();

            builder.Property(x => x.Id)
                .HasColumnName("ct_id");

            builder.Property(x => x.Name)
                .HasColumnName("ct_name")
                .IsRequired()
                .HasMaxLength(80);

            builder.Property(x => x.SortSequence)
                .HasColumnName("ct_sort_seq")
                .IsRequired()
                .HasDefaultValue(0); ;

            builder.Property(x => x.Default)
                .HasColumnName("ct_default")
                .IsRequired()
                .HasDefaultValue(0);
        }
    }
}