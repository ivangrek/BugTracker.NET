/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Identification.Querying
{
    using System;
    using BugTracker.Querying;

    public interface IUserSource : ISource
    {
        int Id { get; }

        string Name { get; }

        string Password { get; }

        int? Salt { get; }

        string FirstName { get; }

        string LastName { get; }

        string Email { get; }

        int Admin { get; }

        int DefaultQueryId { get; }

        int EnableNotifications { get; }

        int AutoSubscribe { get; }

        int? AutoSubscribeOwnBugs { get; }

        int? AutoSubscribeReportedBugs { get; }

        int? SendNotificationsToSelf { get; }

        int Active { get; }

        int? BugsPerPage { get; }

        int? ForcedProject { get; }

        int ReportedNotifications { get; }

        int AssignedNotifications { get; }

        int SubscribedNotifications { get; }

        string Signature { get; }

        int UseFckeditor { get; }

        int EnableBugListPopups { get; }

        int CreatedUserId { get; }

        int OrganizationId { get; }

        DateTime? MostRecentLoginDateTime { get; }
    }
}