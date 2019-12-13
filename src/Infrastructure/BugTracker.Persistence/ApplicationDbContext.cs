/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Persistence
{
    using System.Data.Entity;
    using Identification;
    using Tracking.Bugs;
    using Tracking.Categories;
    using Tracking.Organizations;
    using Tracking.Priorities;
    using Tracking.Projects;
    using Tracking.Statuses;
    using Tracking.UserDefinedAttributes;

    internal sealed class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext() : base("DefaultConnection")
        {
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new UserConfiguration());

            modelBuilder.Configurations.Add(new BugConfiguration());
            modelBuilder.Configurations.Add(new OrganizationConfiguration());
            modelBuilder.Configurations.Add(new CategoryConfiguration());

            modelBuilder.Configurations.Add(new ProjectConfiguration());
            modelBuilder.Configurations.Add(new PriorityConfiguration());
            modelBuilder.Configurations.Add(new StatusConfiguration());
            modelBuilder.Configurations.Add(new UserDefinedAttributeConfiguration());

            //modelBuilder.Configurations.Add(new QueryConfiguration());
            //modelBuilder.Configurations.Add(new ReportConfiguration());
            //modelBuilder.Configurations.Add(new DashboardItemConfiguration());
        }
    }
}