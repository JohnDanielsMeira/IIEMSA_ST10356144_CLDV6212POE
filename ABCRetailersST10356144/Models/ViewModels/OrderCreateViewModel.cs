using System.ComponentModel.DataAnnotations;

namespace ABCRetailersST10356144.Models.ViewModels
{
    public class OrderCreateViewModel
    {
        [Required]
        [Display(Name = "Customer")]
        public string CustomerID { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product")]
        public string ProductID { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity cannot be less than 1")]
        public int Quantity { get; set; }

        [Required]
        [Display(Name = "Order Date")]
        [DataType(DataType.Date)]
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        [Required]
        [Display(Name = "Status")]
        public string Status { get; set; } = "Submitted";

        public List<Customer> Customers { get; set; } = new();
        public List<Product> Products { get; set; } = new();
    }
}
