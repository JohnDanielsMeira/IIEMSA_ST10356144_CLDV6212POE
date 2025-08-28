using System.ComponentModel.DataAnnotations;
using Azure;
using Azure.Data.Tables;

namespace ABCRetailersST10356144.Models
{
    public class Product : ITableEntity
    {
        public string PartitionKey { get; set; } = "Product";
        public string RowKey { get; set; } = Guid.NewGuid().ToString();
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        [Display(Name = "Product ID")]
        public string ProductID => RowKey;

        [Required]
        [Display(Name = "Product Name")]
        public string ProductName { get; set; } = string.Empty;

        [Required]
        [Display(Name = "Description")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Price is required")]
        [Display(Name = "Price")]
        public string Pricing { get; set; } = string.Empty;

        [Display(Name = "Price")]
        public double Price
        {
            get
            {
                return double.TryParse(Pricing, out var result) ? result : 0;
            }

            set
            {
                Pricing = value.ToString("F2");
            }
        }

        [Required]
        [Display(Name = "Stock Available")]
        public int AvailableStock { get; set; }

        [Display(Name = "Image")]
        public string ImageURL { get; set; } = string.Empty;
    }
}
