/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Persistence.Configurations
{
    using System.Data.Entity.ModelConfiguration;
    using Models;

    internal sealed class QueryConfiguration : EntityTypeConfiguration<Query>
    {
        public QueryConfiguration()
        {
            ToTable("queries")
                .HasKey(x => x.Id);

            Property(x => x.Id)
                .HasColumnName("qu_id");

            Property(x => x.Name)
                .HasColumnName("qu_desc")
                .HasMaxLength(200);

            Property(x => x.Sql)
                .HasColumnName("qu_sql");

            Property(x => x.Default)
                .HasColumnName("qu_default");

            Property(x => x.UserId)
                .HasColumnName("qu_user");

            Property(x => x.OrganizationId)
                .HasColumnName("qu_org");
        }
    }
}