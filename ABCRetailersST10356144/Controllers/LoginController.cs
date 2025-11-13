using System.Security.Claims;
using ABCRetailersST10356144.Data;
using ABCRetailersST10356144.Models;
using ABCRetailersST10356144.Models.ViewModels;
using ABCRetailersST10356144.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailersST10356144.Controllers
{
    public class LoginController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly IFunctionsApi _functionsApi;
        private readonly ILogger<LoginController> _logger;

        public LoginController(AuthDbContext db, IFunctionsApi functionsApi, ILogger<LoginController> logger)
        {
            _db = db;
            _functionsApi = functionsApi;
            _logger = logger;
        }

        // GET: /Login
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Index(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model, string? returnUrl)
        {
            if (!ModelState.IsValid)
                return View(model);

            try
            {
                var user = await _db.Users.FirstOrDefaultAsync(u => u.Username == model.Username);
                if (user == null || user.PasswordHash != model.Password)
                {
                    ViewBag.Error = "Invalid username or password";
                    return View(model);
                }

                var customer = (await _functionsApi.GetCustomersAsync())
                    .FirstOrDefault(c => c.Username == model.Username);

                if (customer == null)
                {
                    _logger.LogWarning("Failed to load customer data from Functions API.");
                    ViewBag.Error = "Invalid username or password";
                    return View(model);
                }

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.Username),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("CustomerID", customer.CustomerID)
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal,
                    new AuthenticationProperties { IsPersistent = true, ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(60) });

                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);
                HttpContext.Session.SetString("CustomerID", customer.CustomerID);

                if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);

                return user.Role switch
                {
                    "Admin" => RedirectToAction("AdminDashboard", "Home"),
                    "Customer" => RedirectToAction("CustomerDashboard", "Home"),
                    _ => RedirectToAction("Index", "Home")
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected login error for user");
                ViewBag.Error = "Unexpected error occurred during login";
                return View(model);
            }
        }

        // GET: /Login/Register
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        // POST: /Login/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            if (await _db.Users.AnyAsync(u => u.Username == model.Username))
            {
                ViewBag.Error = "Username already exists";
                return View(model);
            }

            try
            {
                // Save plain-text password
                var user = new User
                {
                    Username = model.Username,
                    PasswordHash = model.Password,
                    Role = model.Role
                };

                _db.Users.Add(user);
                await _db.SaveChangesAsync();

                var customer = new Customer
                {
                    Username = model.Username,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Email = model.Email,
                    ShipAddress = model.ShipAddress
                };

                await _functionsApi.CreateCustomerAsync(customer);

                TempData["Success"] = "Registration successful! You can now login.";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for user");
                ViewBag.Error = "Could not complete registration";
                return View(model);
            }
        }

        // Logout
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            HttpContext.Session.Clear();
            return RedirectToAction("Index", "Home");
        }

        // Access denied
        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied() => View();
    }
}
