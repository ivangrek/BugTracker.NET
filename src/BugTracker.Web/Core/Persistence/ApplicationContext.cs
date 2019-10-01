/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Persistence
{
    using System.Data.Entity;
    using Configurations;
    using Models;

    internal sealed class ApplicationContext : DbContext
    {
        public ApplicationContext() : base("DefaultConnection")
        { }

        public DbSet<Category> Categories { get; set; }

        public DbSet<Priority> Priorities { get; set; }

        public DbSet<Status> Statuses { get; set; }

        public DbSet<UserDefinedAttribute> UserDefinedAttributes { get; set; }

        public DbSet<Report> Reports { get; set; }

        public DbSet<DashboardItem> DashboardItems { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new CategoryConfiguration());
            modelBuilder.Configurations.Add(new PriorityConfiguration());
            modelBuilder.Configurations.Add(new StatusConfiguration());
            modelBuilder.Configurations.Add(new UserDefinedAttributeConfiguration());

            modelBuilder.Configurations.Add(new ReportConfiguration());
            modelBuilder.Configurations.Add(new DashboardItemConfiguration());
        }
    }
}