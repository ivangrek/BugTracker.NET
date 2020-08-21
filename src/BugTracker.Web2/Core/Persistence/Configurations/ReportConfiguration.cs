/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Persistence.Configurations
{
    using System.Data.Entity.ModelConfiguration;
    using Models;

    internal sealed class ReportConfiguration : EntityTypeConfiguration<Report>
    {
        public ReportConfiguration()
        {
            ToTable("reports")
                .HasKey(x => x.Id);

            Property(x => x.Id)
                .HasColumnName("rp_id");

            Property(x => x.Name)
                .HasColumnName("rp_desc")
                .HasMaxLength(200);

            Property(x => x.Sql)
                .HasColumnName("rp_sql");

            Property(x => x.ChartType)
                .HasColumnName("rp_chart_type")
                .HasMaxLength(8);
        }
    }
}