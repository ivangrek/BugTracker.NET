/*
    Copyright 2017-2019 Ivan Grek

    Distributed under the terms of the GNU General Public License
*/

namespace BugTracker.Web.Models.Account
{
    using System.ComponentModel.DataAnnotations;

    public sealed class ForgotModel
    {
        [Display(Name = "User")]
        public string Login { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}