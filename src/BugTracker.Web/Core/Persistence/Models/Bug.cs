namespace BugTracker.Web.Core.Persistence.Models
{
    using System;

    public sealed class Bug
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public int ReportedUserId { get; set; }

        public DateTime ReportedOn { get; set; }

        public int OrganizationId { get; set; }

        public int CategoryId { get; set; }

        public int ProjectId { get; set; }

        public int PriorityId { get; set; }

        public int StatusId { get; set; }

        public int? UserDefinedAttributeId { get; set; }

        public int? AssignedToUserId { get; set; }

        public int? UpdatedUserId { get; set; }

        public DateTime? UpdatedOn { get; set; }

        public string ProjectCustomDropdown1Value { get; set; }

        public string ProjectCustomDropdown2Value { get; set; }

        public string ProjectCustomDropdown3Value { get; set; }

        public string Tags { get; set; }
    }
}