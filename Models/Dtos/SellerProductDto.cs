using MongoDB.Bson.Serialization.Attributes;

namespace ecommerce.Models.Dtos
{
    public class SellerProductDto
    {
        public string? Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }

        // e.g., { "Color": ["Red", "Blue"], "Size": ["S", "M", "L"] }
        public Dictionary<string, List<string>> Attributes { get; set; }
            = new Dictionary<string, List<string>>();

        public List<ProductVariantCreateDto> Variants { get; set; } = new List<ProductVariantCreateDto>();
        public List<string> Tags { get; set; } = new List<string>();
        public bool IsNew { get; set; } = true;
        public List<DiscountCreateDto> Discounts { get; set; } = new List<DiscountCreateDto>();
        public DateTime? RestockDate { get; set; }
        public string CategoryId { get; set; } // Reference to Category
        public string? CategoryName { get; set; }
        public int? Sold { get; set; } // Number of units sold

    }

    public class ProductVariantCreateDto
    {
        public string Color { get; set; }
        public string Size { get; set; }
        public string SKU { get; set; }

        [BsonRepresentation(MongoDB.Bson.BsonType.Decimal128)]
        public decimal Price { get; set; }
        public int Stock { get; set; }
        public List<string> Images { get; set; } = new List<string>();
    }

    public class DiscountCreateDto
    {
        public string Code { get; set; }
        public decimal Percentage { get; set; }
        public DateTime ValidFrom { get; set; }
        public DateTime ValidTo { get; set; }
        public bool IsActive { get; set; } = true;
    }
}
