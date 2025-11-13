using System.ComponentModel.DataAnnotations;
namespace ABCRetailersST10356144.Models.ViewModels
{
    public class RegisterViewModel
    {
        [Required]
        public string Username { get; set; }

        [Required, EmailAddress]
        public string Email {  get; set; }

        [Required, DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        public string FirstName { get; set; }

        [Required]
        public string LastName { get; set; }

        [Required]
        public string ShipAddress { get; set; }

        [Required]
        public string Role {  get; set; }
    }
}
