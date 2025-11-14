using System.Diagnostics;
using ABCRetailersST10356144.Models;
using ABCRetailersST10356144.Models.ViewModels;
using ABCRetailersST10356144.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ABCRetailersST10356144.Controllers
{
    public class HomeController : Controller
    {
        private readonly IFunctionsApi _api;
        private readonly ILogger<HomeController> _logger;

        public HomeController(IFunctionsApi api, ILogger<HomeController> logger)
        {
            _api = api;
            _logger = logger;
        }

        // Public Home Page / Redirect for authenticated users
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                if (User.IsInRole("Admin"))
                    return RedirectToAction("AdminDashboard");
                else if (User.IsInRole("Customer"))
                    return RedirectToAction("CustomerDashboard");
            }

            // Public home page for unauthenticated users
            try
            {
                var products = _api.GetProductsAsync().Result ?? new List<Product>();

                var vm = new HomeViewModel
                {
                    FeaturedProducts = products.Take(8).ToList(),
                    ProductCount = products.Count,
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load home page products");
                TempData["Error"] = "Could not load products. Please try again.";
                return View(new HomeViewModel());
            }
        }

        // Admin Dashboard
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            try
            {
                var customers = await _api.GetCustomersAsync() ?? new List<Customer>();
                var orders = await _api.GetOrdersAsync() ?? new List<Order>();

                ViewBag.TotalCustomers = customers.Count;
                ViewBag.TotalOrders = orders.Count;

                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load admin dashboard");
                TempData["Error"] = "Could not load dashboard data. Please try again.";
                return View();
            }
        }

        // Customer Dashboard
        [Authorize(Roles = "Customer")]
        public IActionResult CustomerDashboard()
        {
            try
            {
                ViewBag.Username = User.Identity?.Name;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load customer dashboard");
                TempData["Error"] = "Could not load dashboard data. Please try again.";
                return View();
            }
        }

        [AllowAnonymous]
        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() =>
            View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
