/*
   Copyright 2017-2019 Ivan Grek

   Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Core.Persistence.Models
{
    using System;

    public class User
    {
        public int Id { get; set; }

        public string Name { get; set; }

        public string Password { get; set; }

        public int? Salt { get; set; }

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

        public int? ForcedProject { get; set; }

        public int ReportedNotifications { get; set; }

        public int AssignedNotifications { get; set; }

        public int SubscribedNotifications { get; set; }

        public string Signature { get; set; }

        public int UseFckeditor { get; set; }

        public int EnableBugListPopups { get; set; }

        public int CreatedUserId { get; set; }

        public int OrganisationId { get; set; }

        public DateTime? MostRecentLoginDateTime { get; set; }
    }
}