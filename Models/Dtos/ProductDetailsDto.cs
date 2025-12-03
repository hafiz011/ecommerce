using MongoDB.Bson.Serialization.Attributes;

namespace ecommerce.Models.Dtos
{
    public class ProductDetailsDto
    {
        public string? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        // e.g., { "Color": ["Red", "Blue"], "Size": ["S", "M", "L"] }
        public Dictionary<string, List<string>> Attributes { get; set; }
            = new Dictionary<string, List<string>>();

        public List<ProductVariantDto> Variants { get; set; } = new List<ProductVariantDto>();
        public List<string> Tags { get; set; } = new List<string>();
        public bool IsNew { get; set; } = true;
        public List<DiscountDto> Discounts { get; set; } = new List<DiscountDto>();
        public DateTime? RestockDate { get; set; }
        public string CategoryId { get; set; } // Reference to Category
        public string? CategoryName { get; set; }
        public int? Sold { get; set; } // Number of units sold
        public List<ReviewDto> Reviews { get; set; } = new List<ReviewDto>(); // Customer reviews

    }

    public class ProductVariantDto
    {
        public string VariantId { get; set; }
        public string Color { get; set; }
        public string Size { get; set; }
        public string SKU { get; set; }

        [BsonRepresentation(MongoDB.Bson.BsonType.Decimal128)]
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public List<string> Images { get; set; } = new List<string>();
    }

    public class DiscountDto
    {
        public string Id { get; set; }
        public string Code { get; set; }
        public decimal Percentage { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class ReviewDto
    {
        public string Id { get; set; }
        public string UserName { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public List<string> Images { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
