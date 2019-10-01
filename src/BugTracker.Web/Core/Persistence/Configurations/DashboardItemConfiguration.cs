/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Persistence.Configurations
{
    using System.Data.Entity.ModelConfiguration;
    using Models;

    internal sealed class DashboardItemConfiguration : EntityTypeConfiguration<DashboardItem>
    {
        public DashboardItemConfiguration()
        {
            ToTable("dashboard_items")
                .HasKey(x => x.Id);

            Property(x => x.Id)
                .HasColumnName("ds_id");

            Property(x => x.UserId)
                .HasColumnName("ds_user");

            Property(x => x.ReportId)
                .HasColumnName("ds_report");

            Property(x => x.ChartType)
                .HasColumnName("ds_chart_type")
                .HasMaxLength(8);

            Property(x => x.Column)
                .HasColumnName("ds_col");

            Property(x => x.Row)
                .HasColumnName("ds_row");
        }
    }
}