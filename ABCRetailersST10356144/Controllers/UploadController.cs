using ABCRetailersST10356144.Models;
using ABCRetailersST10356144.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailersST10356144.Controllers
{
    public class UploadController : Controller
    {
        private readonly IFunctionsApi _api;
        public UploadController(IFunctionsApi api) => _api = api;

        public IActionResult Index()
        {
            return View(new FileUploadModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                if (model.ProofOfPayment is null || model.ProofOfPayment.Length == 0)
                {
                    ModelState.AddModelError("ProofOfPayment", "Please select a file to upload.");
                    return View(model);
                }

                var fileName = await _api.UploadProofOfPaymentAsync(
                    model.ProofOfPayment,
                    model.OrderID,
                    model.CustomerName
                );

                TempData["Success"] = $"File uploaded successfully! File name: {fileName}";
                return View(new FileUploadModel());
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error uploading file: {ex.Message}");
                return View(model);
            }
        }
    }
}
