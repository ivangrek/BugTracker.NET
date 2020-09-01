namespace BugTracker.Web.Core.Persistence.Configurations
{
    using Microsoft.EntityFrameworkCore;
    using Microsoft.EntityFrameworkCore.Metadata.Builders;
    using Models;

    internal sealed class QueryConfiguration : IEntityTypeConfiguration<Query>
    {
        public void Configure(EntityTypeBuilder<Query> builder)
        {
            builder.ToTable("queries")
                .HasKey(x => x.Id);

            builder.HasIndex(x => new { x.Name, x.UserId, x.OrganizationId })
                .IsUnique();

            builder.Property(x => x.Id)
                .HasColumnName("qu_id");

            builder.Property(x => x.Name)
                .HasColumnName("qu_desc")
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(x => x.Sql)
                .IsRequired()
                .HasColumnName("qu_sql");

            builder.Property(x => x.Default)
                .HasColumnName("qu_default");

            builder.Property(x => x.UserId)
                .HasColumnName("qu_user");

            builder.Property(x => x.OrganizationId)
                .HasColumnName("qu_org");
        }
    }
}