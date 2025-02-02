using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ecommerce.Models
{
    public class InventoryModel
    {
        public string Id { get; set; }
        public string ProductId { get; set; }
        public int StockQuantity { get; set; }

        public DateTime RestockDate { get; set; }  // Optional field

        public DateTime LastUpdated { get; set; }
    }
}
