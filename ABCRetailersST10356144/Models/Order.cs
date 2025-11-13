using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Azure;
using Azure.Data.Tables;

namespace ABCRetailersST10356144.Models
{
    public enum OderStatus
    {
        Submitted,      //First created
        Processing,     //Company opens the order
        Completed,      //Order is delivered
        Cancelled       //Order is cancelled
    }

    public class Order : ITableEntity
    {

        public string PartitionKey { get; set; } = "Order";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();

        [NotMapped]
        public DateTimeOffset? Timestamp { get; set; }

        [NotMapped]
        public ETag ETag { get; set; }

        [Display(Name = "Order ID")]
        public string OrderID => RowKey;

        [Display(Name = "Order ID")]
        public string Id { get; set; } = string.Empty; // set from Function response

        [Required]
        [Display(Name = "Customer")]
        public string CustomerID { get; set; } = string.Empty;

        [Display(Name = "Username")]
        public string Username { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Product")]
        public string ProductID { get; set; } = string.Empty;

        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Order Date")]
        public DateTimeOffset OrderDate { get; set; } = DateTimeOffset.UtcNow;

        [Required]
        [Display(Name = "Quantity")]
        [Range(1, int.MaxValue, ErrorMessage = "Quantity cannot be less than 1")]
        public int Quantity { get; set; }

        [Display(Name = "Unit Price"), DataType(DataType.Currency)]
        public decimal UnitPrice { get; set; }

        [Display(Name = "Total price")]
        [DataType(DataType.Currency)]
        public decimal TotalPrice { get; set; }

        [Required]
        [Display(Name = "Status")]
        public OderStatus Status { get; set; } = OderStatus.Submitted;
    }
}
