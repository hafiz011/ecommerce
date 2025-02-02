using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ecommerce.Models
{
    public class Order
    {
        public string Id { get; set; }

        public string UserId { get; set; }

        public string OrderStatus { get; set; }  // "Pending", "Shipped", "Delivered"

        public string PaymentStatus { get; set; }  // "Paid" or "Unpaid"

        public decimal TotalAmount { get; set; }

        public ShippingAddress ShippingAddress { get; set; }

        public List<OrderItem> Items { get; set; } = new List<OrderItem>();

        public DateTime OrderDate { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; }
    }

    public class ShippingAddress
    {
        public string House { get; set; }
        public string City { get; set; }
        public string Zip { get; set; }
        public string Country { get; set; }
        
    }

    public class OrderItem
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }
}
