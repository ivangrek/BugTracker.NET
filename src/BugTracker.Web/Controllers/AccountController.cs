namespace BugTracker.Web.Controllers
{
    using System.Collections.Generic;
    using System.Security.Claims;
    using System.Threading.Tasks;
    using Core;
    using Microsoft.AspNetCore.Authentication;
    using Microsoft.AspNetCore.Authentication.Cookies;
    using Microsoft.AspNetCore.Mvc;
    using Models.Account;

    public class AccountController : Controller
    {
        private readonly IApplicationSettings applicationSettings;

        public AccountController(
            IApplicationSettings applicationSettings)
        {
            this.applicationSettings = applicationSettings;
        }

        [HttpGet]
        public IActionResult Login()
        {
            ViewBag.ApplicationSettings = this.applicationSettings;
            ViewBag.Title = $"{this.applicationSettings.ApplicationTitle} - Sign in";

            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (string.IsNullOrEmpty(model.Login))
            {
                ModelState.AddModelError(nameof(LoginModel.Login), "Required username.");
            }

            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.AddModelError(nameof(LoginModel.Password), "Required password.");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, model.Login)
            };

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = model.RememberMe
            };

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LoginModel model)
        {
            await HttpContext
                .SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction(nameof(Login));
        }
    }
}
