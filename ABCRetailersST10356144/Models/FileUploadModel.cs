using System.ComponentModel.DataAnnotations;

namespace ABCRetailersST10356144.Models
{
    public class FileUploadModel
    {
        [Required]
        [Display(Name = "Proof of Payment")]
        public IFormFile ProofOfPayment { get; set; }

        [Display(Name = "Order ID")]
        public string? OrderID { get; set; }

        [Display(Name = "Customer Name")]
        public string? CustomerName { get; set; }
    }
}
