namespace BugTracker.Web.Core.Persistence.Models
{
    public sealed class Query
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Sql { get; set; }

        public int Default { get; set; }

        public int? UserId { get; set; }

        public int? OrganizationId { get; set; }
    }
}