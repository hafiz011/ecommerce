using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ecommerce.Models
{
    public class ShoppingCartModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string UserId { get; set; }

        public List<CartItem> Items { get; set; } = new List<CartItem>();

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
    public class CartItem
    {
        public string ProductId { get; set; }
        public string VariantId { get; set; }
        public string ProductName { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
        public string SKU { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public string Image { get; set; }
        public string SellerId { get; set; }
        public Dictionary<string, string> SelectedAttributes { get; set; }
        = new Dictionary<string, string>();
        public string Remark { get; set; } // used for comment
    }
}
