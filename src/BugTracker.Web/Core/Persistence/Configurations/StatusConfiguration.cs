namespace BugTracker.Web.Core.Persistence.Configurations
{
    using System.Data.Entity.ModelConfiguration;
    using Models;

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