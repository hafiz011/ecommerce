using AspNetCore.Identity.MongoDbCore.Models;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using System.ComponentModel.DataAnnotations;

namespace ecommerce.Models
{
    public class ApplicationRole : MongoIdentityRole<Guid>
    {
        [Required]
        public string Description { get; set; } // Add custom properties like description if needed
    }
}
