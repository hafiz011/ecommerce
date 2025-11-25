using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ecommerce.Models
{
    public class ProductVariant
    {
        public string VariantId { get; set; } = Guid.NewGuid().ToString();

        public string Color { get; set; }
        public string Size { get; set; }

        public string SKU { get; set; }

        [BsonRepresentation(BsonType.Decimal128)]
        public decimal Price { get; set; }

        public int Stock { get; set; }

        public List<string> Images { get; set; } = new List<string>();
    }
}
