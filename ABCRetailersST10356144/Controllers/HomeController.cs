using System.Diagnostics;
using System.Threading.Tasks;
using ABCRetailersST10356144.Models;
using ABCRetailersST10356144.Models.ViewModels;
using ABCRetailersST10356144.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;

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
        //Main home page (Public)
        [AllowAnonymous]
        public async Task<IActionResult> Index()
        {
            try
            {
                var products = await _api.GetProductsAsync() ?? new List<Product>();

                var vm = new HomeViewModel
                {
                    FeaturedProducts = products.Take(8).ToList(),
                    ProductCount = products.Count,
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load dashboard data from Functions API.");
                TempData["Error"] = "Could not load dashboard data. Please try again.";
                // Show an empty but valid model so the view renders
                return View(new HomeViewModel());
            }
        }

        //Admin Dashboard
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AdminDashboard()
        {
            try
            {
                var customers = await _api.GetCustomersAsync() ?? new List<Customer>();
                var orders = await _api.GetOrdersAsync() ?? new List<Order>();

                var model = new
                {
                    TotalCustomers = customers.Count,
                    TotalPrders = orders.Count
                };

                ViewBag.AdminSummary = model;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load dashboard data from Functions API.");
                TempData["Error"] = "Could not load dashboard data. Please try again.";
                // Show an empty but valid model so the view renders
                return View();
            }
        }

        //Customer Dashboard
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> CustomerDashboard()
        {
            try
            {
                var userEmail = User.Identity?.Name;
                ViewBag.UserEmail = userEmail;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load dashboard data from Functions API.");
                TempData["Error"] = "Could not load dashboard data. Please try again.";
                // Show an empty but valid model so the view renders
                return View();
            }
        }

        [AllowAnonymous]
        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
            => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
