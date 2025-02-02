using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson;

namespace ecommerce.Models
{
    public class ReviewModel
    {
        public string Id { get; set; }
        public string ProductId { get; set; }
        public string UserId { get; set; }
        public int Rating { get; set; }  // Rating out of 5
        public List<string> Images { get; set; } = new List<string>();
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
