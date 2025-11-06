using System.ComponentModel.DataAnnotations;

namespace ecommerce.Models.Dtos
{
    public class CategoryDto
    {
        public string Name { get; set; }
        public byte[]? ImageData { get; set; }
        public string? Description { get; set; }
    }
}
