using ABCRetailersST10356144.Models;
using ABCRetailersST10356144.Services;
using Microsoft.AspNetCore.Mvc;

namespace ABCRetailersST10356144.Controllers
{
    public class UploadController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public UploadController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }
        public IActionResult Index()
        {
            return View(new FileUploadModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(FileUploadModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (model.ProofOfPayment != null && model.ProofOfPayment.Length > 0)
                    {
                        //Upload to blob storage
                        var fileName = await _storageService.UploadFileAsync(model.ProofOfPayment, "payment-proofs");

                        //Upload to file share for contracts
                        await _storageService.UploadToFileShareAsync(model.ProofOfPayment, "contracts", "payments");

                        TempData["Success"] = $"File uploaded successfully! File name: {fileName}";

                        //Clear model for fresh form
                        return View(new FileUploadModel());
                    }
                    else
                    {
                        ModelState.AddModelError("ProofOfPayment", "Please select a file to upload");
                    }
                }
                catch (Exception e)
                {
                    ModelState.AddModelError("", $"Error uploading file: {e.Message}");
                }
            }

            return View(model);
        }
    }
}
