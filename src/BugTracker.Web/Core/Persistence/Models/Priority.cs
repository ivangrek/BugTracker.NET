﻿namespace BugTracker.Web.Core.Persistence.Models
{
    public class Priority
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string BackgroundColor { get; set; }

        public int SortSequence { get; set; }

        public string Style { get; set; }

        public int Default { get; set; }
    }
}