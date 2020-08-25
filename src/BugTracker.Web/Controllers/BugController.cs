namespace BugTracker.Web.Controllers
{
    using Core;
    using Microsoft.AspNetCore.Authorization;
    using Microsoft.AspNetCore.Mvc;

    [Authorize]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public class BugController : Controller
    {
        public IActionResult Index()
        {
            ViewBag.SelectedItem = MainMenuSection.Bugs;

            return View();
        }
    }
}
