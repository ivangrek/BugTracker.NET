namespace BugTracker.Web.Core.Persistence.Models
{
    public sealed class Category 
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int SortSequence { get; set; }

        public int Default { get; set; }
    }
}