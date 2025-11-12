using System.ComponentModel.DataAnnotations;

namespace ecommerce.Models.Dtos
{
    public class CreateOrderRequest
    {
        public string? ProductId { get; set; }
        [Required]
        public string PaymentMethod { get; set; }
        [Required]
        public AddShippingAddress AddShippingAddress { get; set; }
    }
    public class AddShippingAddress
    {
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }
}
