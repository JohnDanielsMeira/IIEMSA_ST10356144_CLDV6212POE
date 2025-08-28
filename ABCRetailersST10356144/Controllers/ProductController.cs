using ABCRetailersST10356144.Models;
using ABCRetailersST10356144.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailersST10356144.Controllers
{
    public class ProductController : Controller
    {
        private readonly IAzureStorageService _storageService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IAzureStorageService storageService, ILogger<ProductController> logger)
        {
            _storageService = storageService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            var products = await _storageService.GetAllEntitiesAsync<Product>();
            return View(products);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Product product, IFormFile? imageFile)
        {
            //Manual price parsing to fix binding
            if (Request.Form.TryGetValue("Price", out var priceForValue))
            {
                _logger.LogInformation("Raw price from form: '{PriceForValue}'", priceForValue.ToString());

                if (decimal.TryParse(priceForValue, out var parsedPrice))
                {
                    product.Price = (double)parsedPrice;
                    _logger.LogInformation("Successfully parsed price: {Price}", parsedPrice);
                }
                else
                {
                    _logger.LogWarning("Failed to parse price: {PriceForValue}", priceForValue.ToString());
                }
            }

            _logger.LogInformation("Final product price: {Price}", product.Price);

            if (ModelState.IsValid)
            {
                try
                {
                    if (product.Price <= 0)
                    {
                        ModelState.AddModelError("Price", "Price must be greater than $0.00");
                        return View(product);
                    }

                    //Upload image
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageURL = await _storageService.UploadImageAsync(imageFile, "product-images");
                        product.ImageURL = imageURL;
                    }

                    await _storageService.AddEntityAsync(product);
                    TempData["Success"] = $"Product '{product.ProductName}' created successfully with price {product.Price:C}!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error creating product");
                    ModelState.AddModelError("", $"Error creating product: {ex.Message}");
                }
            }

            return View(product);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Product product, IFormFile? imageFile)
        {
            //Manual price parsing to fix binding
            if (Request.Form.TryGetValue("Price", out var priceForValue))
            {
                if (decimal.TryParse(priceForValue, out var parsedPrice))
                {
                    product.Price = (double)parsedPrice;
                    _logger.LogInformation("Edit: Successfully parsed price: {Price}", parsedPrice);
                }
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var originalProduct = await _storageService.GetEntityAsync<Product>("Product", product.RowKey);
                    if (originalProduct == null)
                    {
                        return NotFound();
                    }

                    //Update properties but keep original Etag
                    originalProduct.ProductName = product.ProductName;
                    originalProduct.Description = product.Description;
                    originalProduct.Price = product.Price;
                    originalProduct.AvailableStock = product.AvailableStock;

                    //Upload image
                    if (imageFile != null && imageFile.Length > 0)
                    {
                        var imageURL = await _storageService.UploadImageAsync(imageFile, "product-images");
                        originalProduct.ImageURL = imageURL;
                    }

                    await _storageService.AddEntityAsync(originalProduct);
                    TempData["Success"] = "Product updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error updating product: {Message}", ex.Message);
                    ModelState.AddModelError("", $"Error updating product: {ex.Message}");
                }
            }

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Customer>("Product", id);
                TempData["Success"] = "Product deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting product: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
