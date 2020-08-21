/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Account
{
    using System.ComponentModel.DataAnnotations;

    public sealed class ChangePasswordModel
    {
        public string Id { get; set; }

        [Display(Name = "New Password")]
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }

        [Display(Name = "Reenter Password")]
        [Required(ErrorMessage = "Confirm password is required.")]
        [Compare(nameof(Password), ErrorMessage = "Confirm doesn't match password.")]
        public string ConfirmedPassword { get; set; }
    }
}