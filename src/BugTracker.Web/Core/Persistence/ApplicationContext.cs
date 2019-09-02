namespace BugTracker.Web.Core.Persistence
{
    using System.Data.Entity;
    using Configurations;
    using Models;

    internal sealed class ApplicationContext : DbContext
    {
        public ApplicationContext() : base("DefaultConnection")
        { }

        public DbSet<Priority> Priorities { get; set; }
        public DbSet<Status> Statuses { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new PriorityConfiguration());
            modelBuilder.Configurations.Add(new StatusConfiguration());
        }
    }
}