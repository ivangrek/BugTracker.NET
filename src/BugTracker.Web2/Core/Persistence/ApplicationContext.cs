/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Persistence
{
    using System.Data.Entity;
    using Configurations;
    using Models;

    public sealed class ApplicationContext : DbContext
    {
        public ApplicationContext() : base("DefaultConnection")
        { }

        public DbSet<User> Users { get; set; }

        public DbSet<Organization> Organizations { get; set; }

        public DbSet<Query> Queries { get; set; }

        public DbSet<Report> Reports { get; set; }

        public DbSet<DashboardItem> DashboardItems { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new UserConfiguration());
            modelBuilder.Configurations.Add(new OrganizationConfiguration());

            modelBuilder.Configurations.Add(new QueryConfiguration());
            modelBuilder.Configurations.Add(new ReportConfiguration());
            modelBuilder.Configurations.Add(new DashboardItemConfiguration());
        }
    }
}