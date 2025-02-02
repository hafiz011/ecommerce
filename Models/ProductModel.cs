using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ecommerce.Models
{
    public class ProductModel
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string Description { get; set; }

        public string CategoryId { get; set; }

        public decimal Price { get; set; }

        public List<string> Images { get; set; } = new List<string>();

        public List<string> Tags { get; set; } = new List<string>();

        public int StockQuantity { get; set; }
        public List<Discount> Discounts { get; set; } = new List<Discount>();
        public string SellerId { get; set; } // Reference to User
        public bool IsNew { get; set; } // true for new, false for used

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>();
    }

    // Discount Model
    public class Discount
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Code { get; set; } // Discount code (e.g., "SAVE20")
        public decimal Percentage { get; set; } // Discount percentage
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsActive { get; set; } = true;
        public string ProductId { get; set; } // Applies to a specific product, or null for all products
    }
}
