using ABCRetailersST10356144.Models;
using ABCRetailersST10356144.Models.ViewModels;
using ABCRetailersST10356144.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.BlazorIdentity.Shared;
using System.Text.Json;

namespace ABCRetailersST10356144.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public OrderController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        public async Task<IActionResult> Index()
        {
            var orders = await _storageService.GetAllEntitiesAsync<Order>();
            return View(orders);
        }

        public async Task<IActionResult> Create()
        {
            var customers = await _storageService.GetAllEntitiesAsync<Customer>();
            var products = await _storageService.GetAllEntitiesAsync<Product>();

            var viewModel = new OrderCreateViewModel
            {
                Customers = customers,
                Products = products
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    //Get customer and product details
                    var customer = await _storageService.GetEntityAsync<Customer>("Customer", model.CustomerID);
                    var product = await _storageService.GetEntityAsync<Product>("Product", model.ProductID);

                    if (customer == null || product == null)
                    {
                        ModelState.AddModelError("", "Invalid customer or product selected.");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    //Check stock availability
                    if (product.AvailableStock < model.Quantity)
                    {
                        ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.AvailableStock}");
                        await PopulateDropdowns(model);
                        return View(model);
                    }

                    //Create order
                    var order = new Order
                    {
                        CustomerID = model.CustomerID,
                        Username = customer.Username,
                        ProductID = model.ProductID,
                        ProductName = product.ProductName,
                        OrderDate = DateTime.SpecifyKind(model.OrderDate, DateTimeKind.Utc),
                        Quantity = model.Quantity,
                        UnitPrice = product.Price,
                        TotalPrice = product.Price * model.Quantity,
                        Status = "Submitted" //Always start as submitted??
                    };

                    await _storageService.AddEntityAsync(order);

                    //Update product stock
                    product.AvailableStock -= model.Quantity;
                    await _storageService.UpdateEntityAsync(product);

                    //Send queue message for new order
                    var orderMessage = new
                    {
                        OrderID = order.OrderID,
                        CustomerID = order.CustomerID,
                        CustomerName = customer.FirstName + " " + customer.LastName,
                        ProductName = product.ProductName,
                        Quantity = order.Quantity,
                        TotalPrice = order.TotalPrice,
                        OrderDate = order.OrderDate,
                        Status = order.Status
                    };

                    await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(orderMessage));

                    //Send stock update message
                    var stockMessage = new
                    {
                        ProductID = product.ProductID,
                        ProductName = product.ProductName,
                        OldStock = product.AvailableStock + model.Quantity,
                        NewStock = product.AvailableStock,
                        UpdatedBy = "Order System",
                        UpdateDate = DateTime.UtcNow
                    };

                    await _storageService.SendMessageAsync("stock-notifications", JsonSerializer.Serialize(stockMessage));

                    TempData["Success"] = "Order created successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error creating order: {ex.Message}");
                }
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _storageService.UpdateEntityAsync(order);
                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                }
            }

            return View(order);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Order>("Order", id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> GetProductPrice(string productID)
        {
            try
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", productID);

                if (product != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.AvailableStock,
                        productName = product.ProductName
                    });
                }
                return Json(new { success = false });
            }
            catch
            {
                return Json(new { success = false });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(string id, string newStatus)
        {
            try
            {
                var order = await _storageService.GetEntityAsync<Order>("Order", id);

                if (order != null)
                {
                    return Json(new
                    {
                        success = false,
                        message = "Order not found"
                    });
                }

                var previousStatus = order.Status;
                order.Status = newStatus;
                await _storageService.UpdateEntityAsync(order);

                var statusMessage = new
                {
                    OrderID = order.OrderID,
                    CustomerID = order.CustomerID,
                    CustomerName = order.Username,
                    ProductName = order.ProductName,
                    PreviousStatus = previousStatus,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "System"
                };

                await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(statusMessage));

                return Json(new
                {
                    success = true,
                    message = $"Order status updated to {newStatus}"
                });
            }
            catch (Exception e)
            {
                return Json(new
                {
                    success = false,
                    message = e.Message

                });
            }
        }

        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
            model.Products = await _storageService.GetAllEntitiesAsync<Product>();
        }
    }
}
