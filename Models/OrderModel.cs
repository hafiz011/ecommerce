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

        public string UserId { get; set; }
        public string SellerId { get; set; }

        public List<OrderItem> Items { get; set; } = new();

        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TotalAmount { get; set; }

        public PaymentModel Payment { get; set; } = new PaymentModel();

        // Multiple Status Changes Required, So make it a List
        public List<StatusTimeline> StatusTimeline { get; set; } = new();

        // Additional Order Status for Quick Query (Processing, Shipped, Delivered)
        public string OrderStatus { get; set; } = "Processing";

        public DeliveryInfo DeliveryInfo { get; set; } = new DeliveryInfo();

        public InvoiceModel Invoice { get; set; } = new InvoiceModel();

        public ShippingAddress ShippingAddress { get; set; }

        public string AdminNote { get; set; }
        public string SellerNote { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpdatedAt { get; set; }
    }

    public class OrderItem
    {
        public string ProductId { get; set; }
        public string VariantId { get; set; }
        public string ProductName { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
        public string SKU { get; set; }

        public Dictionary<string, string> SelectedAttributes { get; set; }
            = new Dictionary<string, string>();

        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Image { get; set; }
        public string SellerId { get; set; }

        public string Remark { get; set; }
    }

    public class PaymentModel
    {
        public string Method { get; set; }            // COD, Stripe, SSLCommerz, Bkash
        public string Status { get; set; }  // Paid, Pending, Failed

        public string TransactionId { get; set; }     // Gateway Transaction ID
        public string Gateway { get; set; }           // Stripe, Paypal
        public string GatewayResponse { get; set; }   // Raw gateway JSON

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? PaidAt { get; set; }         // Null until paid
    }

    public class StatusTimeline
    {
        public string Status { get; set; }            // Processing, Shipped, Delivered
        public string Message { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime UpadateAt { get; set; } = DateTime.UtcNow;
    }

    public class DeliveryInfo
    {
        public string CourierName { get; set; }
        public string TrackingNumber { get; set; }
        public string TrackingUrl { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ShippedAt { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? EstimatedDelivery { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? DeliveredAt { get; set; }
    }

    public class InvoiceModel
    {
        public string InvoiceNumber { get; set; }

        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime IssuedAt { get; set; }
        public string BillingAddress { get; set; }
        public string TaxId { get; set; }
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
