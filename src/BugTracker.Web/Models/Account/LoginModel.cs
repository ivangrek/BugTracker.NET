/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Account
{
    using System.ComponentModel.DataAnnotations;

    public sealed class LoginModel
    {
        [Display(Name = "User")]
        public string Login { get; set; }

        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        public bool AsGuest { get; set; }
    }
}