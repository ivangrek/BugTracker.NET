namespace BugTracker.Web.Core.Persistence
{
    using System.Data.Entity;
    using Configurations;
    using Models;

    internal sealed class ApplicationContext : DbContext
    {
        public ApplicationContext() : base("DefaultConnection")
        { }

        public DbSet<Status> Statuses { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new StatusConfiguration());
        }
    }
}