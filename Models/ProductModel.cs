using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ecommerce.Models
{
    public class ProductModel
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string Id { get; set; }

        public string Name { get; set; } // Product name
        public string Description { get; set; } // Detailed description
        public string CategoryId { get; set; } // Reference to Category
        public string CategoryName { get; set; } // Category name for easier access

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal BasePrice { get; set; } // Base price                                                                                                    // Multiple selectable attributes (color list, size list, etc.)
        public Dictionary<string, List<string>> Attributes { get; set; } 
            = new Dictionary<string, List<string>>(); // e.g., {"Color": ["Red", "Blue"], "Size": ["S", "M", "L"]}

        public List<ProductVariant> Variants { get; set; }
            = new List<ProductVariant>(); // Variants based on attributes
        public List<string> Tags { get; set; } = new List<string>(); // Tags for search optimization
        public int Sold { get; set; } // Number of units sold
        public List<Discount> Discounts { get; set; } = new List<Discount>(); // List of discounts
        public string SellerId { get; set; } // Reference to Seller
        public bool IsNew { get; set; } // true for new, false for used
        public List<Review> Review { get; set; } = new List<Review>(); // Customer reviews
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime RestockDate { get; set; } // Expected restock date if out of stock
    }
    
}
