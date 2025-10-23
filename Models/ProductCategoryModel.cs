using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.ComponentModel.DataAnnotations.Schema;

namespace ecommerce.Models
{
    public class ProductCategoryModel
    {
        public string Id { get; set; }

        public string Name { get; set; }
        
        // Upload file from client
        [NotMapped] // Ignore in database
        public IFormFile ImgUrl { get; set; }
        public string ImgUrlPath { get; set; }
        public string Description { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
