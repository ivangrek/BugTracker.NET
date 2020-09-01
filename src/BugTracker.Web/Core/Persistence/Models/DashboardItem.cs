namespace BugTracker.Web.Core.Persistence.Models
{
    public sealed class DashboardItem
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int ReportId { get; set; }

        public string ChartType { get; set; }

        public int Column { get; set; }

        public int Row { get; set; }
    }
}