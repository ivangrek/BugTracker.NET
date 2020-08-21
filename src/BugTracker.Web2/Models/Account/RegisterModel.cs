/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Account
{
    using System.ComponentModel.DataAnnotations;

    public sealed class RegisterModel
    {
        [Display(Name = "User")]
        [Required(ErrorMessage = "Username is required.")]
        public string Login { get; set; }

        [Display(Name = "Email")]
        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "Format of email address is invalid.")]
        public string Email { get; set; }

        [Display(Name = "Password")]
        [Required(ErrorMessage = "Password is required.")]
        public string Password { get; set; }

        [Display(Name = "Confirm Password")]
        [Required(ErrorMessage = "Confirm password is required.")]
        [Compare(nameof(Password), ErrorMessage = "Confirm doesn't match password.")]
        public string ConfirmedPassword { get; set; }

        [Display(Name = "First Name")]
        [Required(ErrorMessage = "Firstname is required.")]
        public string FirstName { get; set; }

        [Display(Name = "Last Name")]
        [Required(ErrorMessage = "Lastname is required.")]
        public string LastName { get; set; }
    }
}