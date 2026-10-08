using System.Diagnostics;
using CLDV6212_POE.Models;
using Microsoft.AspNetCore.Mvc;

namespace CLDV6212_POE.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Check if user is logged in using session
            if (HttpContext.Session.GetString("Email") == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get user info from session
            var userName = HttpContext.Session.GetString("UserName");
            var role = HttpContext.Session.GetString("Role");

            ViewBag.UserName = userName;
            ViewBag.Role = role;

            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
