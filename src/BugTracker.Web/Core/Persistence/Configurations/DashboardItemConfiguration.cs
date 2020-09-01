namespace BugTracker.Web.Core.Persistence.Configurations
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Models;

    internal sealed class DashboardItemConfiguration : IEntityTypeConfiguration<DashboardItem>
    {
        public void Configure(EntityTypeBuilder<DashboardItem> builder)
        {
            builder.ToTable("dashboard_items")
                .HasKey(x => x.Id);

            builder.HasIndex(x => x.UserId);

            builder.Property(x => x.Id)
                .HasColumnName("ds_id");

            builder.Property(x => x.UserId)
                .HasColumnName("ds_user")
                .IsRequired();

            builder.Property(x => x.ReportId)
                .HasColumnName("ds_report")
                .IsRequired();

            builder.Property(x => x.ChartType)
                .HasColumnName("ds_chart_type")
                .IsRequired()
                .HasMaxLength(8);

            builder.Property(x => x.Column)
                .HasColumnName("ds_col")
                .IsRequired();

            builder.Property(x => x.Row)
                .HasColumnName("ds_row")
                .IsRequired();
        }
    }
}