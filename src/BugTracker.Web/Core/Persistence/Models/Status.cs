namespace BugTracker.Web.Core.Persistence.Models
{
    public class Status
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int SortSequence { get; set; }

        public string Style { get; set; }

        public int Default { get; set; }
    }
}