namespace BugTracker.Web.Models.Account
{
    using System.ComponentModel.DataAnnotations;

    public sealed class LoginModel
    {
        [Display(Name = "Username")]
        public string Login { get; set; }

        [Display(Name = "Password")]
        public string Password { get; set; }

        [Display(Name = "Remember me")]
        public bool RememberMe { get; set; }

        public bool AsGuest { get; set; }
    }
}