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
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>(); // Additional attributes (e.g., color, size)
        public string Description { get; set; } // Detailed description
        public string CategoryId { get; set; } // Reference to Category

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Price { get; set; } // Base price
        public List<string> Images { get; set; } = new List<string>(); // Multiple images
        public List<string> Tags { get; set; } = new List<string>(); // Tags for search optimization
        public int StockQuantity { get; set; } // Available stock
        public int Sold { get; set; } // Number of units sold
        public List<Discount> Discounts { get; set; } = new List<Discount>(); // List of discounts
        public string SellerId { get; set; } // Reference to Seller
        public bool IsNew { get; set; } // true for new, false for used
        public List<Review> Review { get; set; } = new List<Review>(); // Customer reviews
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime RestockDate { get; set; } // Expected restock date if out of stock
    }

    // Discount Model
    public class Discount
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Code { get; set; } // Discount code (e.g., "SAVE20")
        public decimal Percentage { get; set; } // Discount percentage
        public DateTime ValidFrom { get; set; } // Start date of the discount
        public DateTime ValidTo { get; set; } // End date of the discount
        public bool IsActive { get; set; } = true; // Is the discount currently active
        public string ProductId { get; set; } // Applies to a specific product, or null for all products
    }

    public class Review
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }  // Reference to User
        public string UserName { get; set; } // User's display name
        public int Rating { get; set; }  // Rating out of 5
        public List<string> Images { get; set; } = new List<string>(); // Review images
        public string Comment { get; set; } // Review comment
        public DateTime CreatedAt { get; set; }
    }
}
