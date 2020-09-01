namespace BugTracker.Web.Core.Persistence.Configurations
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Models;

    internal sealed class ReportConfiguration : IEntityTypeConfiguration<Report>
    {
        public void Configure(EntityTypeBuilder<Report> builder)
        {
            builder.ToTable("reports")
                .HasKey(x => x.Id);

            builder.HasIndex(x => x.Name)
                .IsUnique();

            builder.Property(x => x.Id)
                .HasColumnName("rp_id");

            builder.Property(x => x.Name)
                .HasColumnName("rp_desc")
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Sql)
                .HasColumnName("rp_sql")
                .IsRequired();

            builder.Property(x => x.ChartType)
                .HasColumnName("rp_chart_type")
                .IsRequired()
                .HasMaxLength(8);
        }
    }
}