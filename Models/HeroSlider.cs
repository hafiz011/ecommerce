using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ecommerce.Models
{
    public class HeroSlider
    {
        [BsonId]
        [BsonRepresentation(BsonType.ObjectId)]
        public string? Id { get; set; }
        public string? ImageUrl { get; set; }
        public string? Title { get; set; }
        public string? Subtitle { get; set; }
        public string? ButtonText { get; set; }
        public string? ButtonLink { get; set; }
        public DateTime? CountdownEnd { get; set; }
        public bool ShowArrows { get; set; } = true;
        public bool ShowDots { get; set; } = true;
        public int Order { get; set; } = 0;
        public bool IsActive { get; set; } = true;
        public string? SellerId { get; set; }
    }
}
