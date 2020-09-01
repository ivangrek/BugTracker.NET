namespace BugTracker.Web.Core.Persistence.Models
{
    public sealed class Report
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Sql { get; set; }

        public string ChartType { get; set; }
    }
}