using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System;
using System.Collections.Generic;

namespace ecommerce.Models
{
    public class OrderModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string UserId { get; set; } // ID of the user who placed the order
        public string SellerId { get; set; }   // for seller-specific orders

        public List<OrderItem> Items { get; set; } = new List<OrderItem>(); // List of products in the order

        public decimal SubTotal { get; set; } // Sum of item prices
        public decimal ShippingCost { get; set; } // Shipping fee
        public decimal TotalAmount { get; set; } // Final amount (SubTotal + ShippingCost - Discounts)

        public string PaymentMethod { get; set; } // e.g. COD, SSLCommerz, Stripe etc.
        public string PaymentStatus { get; set; } = "Pending";
        public string OrderStatus { get; set; } = "Processing"; // Processing, Shipped, Delivered, Cancelled

        public ShippingAddress ShippingAddress { get; set; } // Shipping details

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow; // Order creation timestamp

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow; // Last update timestamp
    }

    public class OrderItem
    {
        public string ProductId { get; set; }
        public string VariantId { get; set; }
        public string ProductName { get; set; }
        public Dictionary<string, string> SelectedAttributes { get; set; }
            = new Dictionary<string, string>();
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Image { get; set; }
        public string SellerId { get; set; }

    }

    public class ShippingAddress
    {
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }
}
