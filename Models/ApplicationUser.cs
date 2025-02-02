using AspNetCore.Identity.MongoDbCore.Models;
using Microsoft.AspNetCore.Identity;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;
using System.Collections.Generic;

namespace ecommerce.Models
{
    public class ApplicationUser : MongoIdentityUser<Guid>
    {
        [BsonElement("FirstName")]
        public string FirstName { get; set; }

        [BsonElement("LastName")]
        public string LastName { get; set; }

        [BsonElement("Phone")]
        public string Phone { get; set; }

        [BsonElement("Address")]
        public Address Address { get; set; }

        [BsonElement("Wishlist")]
        public List<WishlistItem> Wishlist { get; set; } = new List<WishlistItem>();

        [BsonElement("Cart")]
        public List<CartItem> Cart { get; set; } = new List<CartItem>();

        [BsonElement("OrderHistory")]
        public List<OrderHistory> OrderHistory { get; set; } = new List<OrderHistory>();

        [BsonElement("RefreshToken")]
        public string RefreshToken { get; set; }

        [BsonElement("RefreshTokenExpiryTime")]
        public DateTime RefreshTokenExpiryTime { get; set; }
    }

    public class Address
    {
        public string Street { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public string Zip { get; set; }
    }

    public class WishlistItem
    {
        public string ProductId { get; set; }
        public DateTime AddedAt { get; set; }
    }

   

    public class OrderHistory
    {
        public string OrderId { get; set; }
        public string Status { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
