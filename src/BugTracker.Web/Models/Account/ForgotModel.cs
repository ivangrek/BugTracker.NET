namespace BugTracker.Web.Models.Account
{
    using System.ComponentModel.DataAnnotations;

    public sealed class ForgotModel
    {
        [Display(Name = "Username")]
        public string Login { get; set; }

        [Display(Name = "Email")]
        public string Email { get; set; }
    }
}