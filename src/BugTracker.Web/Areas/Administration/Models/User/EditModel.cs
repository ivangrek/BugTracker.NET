/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Areas.Administration.Models.User
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;

    public sealed class EditModel
    {
        public EditModel()
        {
            AutoSubscribePerProjectIds = new List<int>();
            AdminProjectIds = new List<int>();
        }

        public int Id { get; set; }

        [Display(Name = "Username")]
        [Required(ErrorMessage = "Username is required.")]
        public string Login { get; set; }

        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Confirm Password")]
        public string ConfirmedPassword { get; set; }

        [Display(Name = "First Name")]
        public string FirstName { get; set; }

        [Display(Name = "Organization")]
        [Required(ErrorMessage = "You must select a Organization.")]
        public int? OrganizationId { get; set; }

        [Display(Name = "Last Name")]
        public string LastName { get; set; }

        [Display(Name = "Active")]
        public bool Active { get; set; }

        [Display(Name = "Admin")]
        public bool Admin { get; set; }

        [Display(Name = "Bugs Per Page")]
        [Required(ErrorMessage = "Per Page is required.")]
        [RegularExpression("([0-9]+)", ErrorMessage = "Per Page must be a number.")]
        public int BugsPerPage { get; set; }

        [Display(Name = "Enable buglist popups")]
        public bool EnableBugListPopups { get; set; }

        [Display(Name = "Edit text using colors and fonts")]
        public bool EditText { get; set; }

        [Display(Name = "Default bug Query")]
        public int DefaultQueryId { get; set; }

        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Format of email address is invalid.")]
        public string Email { get; set; }

        [Display(Name = "Outgoing Email Signature")]
        public string EmailSignature { get; set; }

        [Display(Name = "Enable notifications")]
        public bool EnableNotifications { get; set; }

        [Display(Name = "Auto-subscribe to all items")]
        public bool AutoSubscribeToAllItems { get; set; }

        [Display(Name = "Auto-subscribe per project")]
        public List<int> AutoSubscribePerProjectIds { get; set; }

        [Display(Name = "Auto-subscribe to all items ASSIGNED TO you")]
        public bool AutoSubscribeToAllItemsAssignedToYou { get; set; }

        [Display(Name = "Auto-subscribe to all items REPORTED BY you")]
        public bool AutoSubscribeToAllItemsReportedByYou { get; set; }

        [Display(Name = "Apply subscription changes retroactively")]
        public bool ApplySubscriptionChangesRetroactively { get; set; }

        [Display(Name = "Notifications for subscribed bugs reported by me")]
        public int NotificationsSubscribedBugsReportedByMe { get; set; }

        [Display(Name = "Notifications for subscribed bugs assigned to me")]
        public int NotificationsSubscribedBugsAssignedToMe { get; set; }

        [Display(Name = "Notifications for all other subscribed bugs")]
        public int NotificationsForAllOtherSubscribedBugs { get; set; }

        [Display(Name = "Send notifications even for items you add or change")]
        public bool SendNotificationsEvenForItemsAddOrChange { get; set; }

        [Display(Name = "Force user to add new bugs to this project")]
        public int ForcedProjectId { get; set; }

        [Display(Name = "Allowed to add/delete other users for the following projects")]
        public List<int> AdminProjectIds { get; set; }

        public Dictionary<string, string[]> Permissions { get; set; }
    }
}