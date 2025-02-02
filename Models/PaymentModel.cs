using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ecommerce.Models
{
    public class PaymentModel
    {
        public string Id { get; set; }
        public string UserId { get; set; }

        public string OrderId { get; set; }

        public string PaymentMethod { get; set; }  // e.g., Credit Card, PayPal

        public string PaymentStatus { get; set; }  // "Paid", "Pending"

        public string TransactionId { get; set; }

        public DateTime PaymentDate { get; set; }
    }
}
