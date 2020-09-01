namespace BugTracker.Web.Core.Persistence.Models
{
    using System;

    public sealed class User
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public string Salt { get; set; }

        public string Password { get; set; }

        public string PasswordResetKey { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Email { get; set; }

        public int Admin { get; set; }

        public int DefaultQueryId { get; set; }

        public int EnableNotifications { get; set; }

        public int AutoSubscribe { get; set; }

        public int? AutoSubscribeOwnBugs { get; set; }

        public int? AutoSubscribeReportedBugs { get; set; }

        public int? SendNotificationsToSelf { get; set; }

        public int Active { get; set; }

        public int? BugsPerPage { get; set; }

        public int? ForcedProjectId { get; set; }

        public int ReportedNotifications { get; set; }

        public int AssignedNotifications { get; set; }

        public int SubscribedNotifications { get; set; }

        public string Signature { get; set; }

        public int UseFckeditor { get; set; }

        public int EnableBugListPopups { get; set; }

        public int CreatedUserId { get; set; }

        public int OrganizationId { get; set; }

        public DateTime? MostRecentLoginDateTime { get; set; }
    }
}