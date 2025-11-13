using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ABCRetailersST10356144.Data;
using ABCRetailersST10356144.Models;
using ABCRetailersST10356144.Models.ViewModels;
using ABCRetailersST10356144.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.CodeAnalysis;
using Microsoft.EntityFrameworkCore;

namespace ABCRetailersST10356144.Controllers
{
    [Authorize(Roles = "Customer")]
    public class CartController : Controller
    {
        private readonly AuthDbContext _db;
        private readonly IFunctionsApi _api;

        public CartController(AuthDbContext db, IFunctionsApi api)
        {
            _db = db;
            _api = api;
        }

        public async Task<IActionResult> Index()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            var cartItems = await _db.Cart
                .Where(c => c.CustomerUsername == username)
                .ToListAsync();

            var viewModelList = new List<CartItemViewModel>();

            foreach (var item in cartItems)
            {
                var product = await _api.GetProductAsync(item.ProductID);
                if (product == null)
                    continue;

                viewModelList.Add(new CartItemViewModel
                {
                    ProductID = product.ProductID,
                    ProductName = product.ProductName,
                    Quantity = item.Quantity,
                    UnitPrice = product.Price
                });
            }

            return View(new CartPageViewModel { Items = viewModelList });
        }

        public async Task<IActionResult> Add(string productID)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(productID))
                return RedirectToAction("Index", "Product");

            var product = await _api.GetProductAsync(productID);
            if (product == null)
                return NotFound();

            var existing = await _db.Cart.FirstOrDefaultAsync(c =>
            c.ProductID == productID && c.CustomerUsername == username);

            if (existing != null)
            {
                existing.Quantity += 1;
            }
            else
            {
                _db.Cart.Add(new Cart
                {
                    CustomerUsername = username,
                    ProductID = productID,
                    Quantity = 1
                });
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = $"{product.ProductName} added to cart";
            return RedirectToAction("Index", "Product");
        }

        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index", "Login");

            var customer = (await _api.GetCustomersAsync())
               .FirstOrDefault(c => c.Username == username);
            if (customer == null)
            {
                TempData["Error"] = "Customer not found";
                return RedirectToAction("Index");
            }

            var cartItems = await _db.Cart
                .Where(c => c.CustomerUsername == username)
                .ToListAsync();

            if (!cartItems.Any())
            {
                TempData["Error"] = "Your cart is empty";
                return RedirectToAction("Index");
            }

            foreach (var item in cartItems)
            {
                await _api.CreateOrderAsync(customer.CustomerID, item.ProductID, item.Quantity);
            }

            _db.Cart.RemoveRange(cartItems);
            await _db.SaveChangesAsync();

            TempData["SuccessMessage"] = "Order placed successfully!";
            return RedirectToAction("Confirmation");
        }

        public IActionResult Confirmation()
        {
            ViewBag.Message = TempData["SuccessMessage"] ?? "Thank you for your purchase";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Remove (string productID)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index");

            var item = await _db.Cart.FirstOrDefaultAsync(c =>
            c.CustomerUsername == username && c.ProductID == productID);

            if (item != null)
            {
                _db.Cart.Remove(item);
                await _db.SaveChangesAsync();
                TempData["Success"] = "Item removed from cart";
            }


            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> UpdateQuantities(List<CartItemViewModel> items)
        {
            var username = User.Identity?.Name;
            if (string.IsNullOrEmpty(username))
                return RedirectToAction("Index");

            foreach (var item in items)
            {
                var cartItem = await _db.Cart.FirstOrDefaultAsync(c =>
            c.CustomerUsername == username && c.ProductID == item.ProductID);

                if (cartItem != null)
                {
                    cartItem.Quantity = item.Quantity;
                }
            }

            await _db.SaveChangesAsync();
            TempData["Success"] = "Cart updated successfully";
            return RedirectToAction("Index");
        }
    }
}
