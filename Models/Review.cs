namespace ecommerce.Models
{
    public class Review
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string UserId { get; set; }  // Reference to User
        public string UserName { get; set; } // User's display name
        public int Rating { get; set; }  // Rating out of 5
        public List<string> Images { get; set; } = new List<string>(); // Review images
        public string Comment { get; set; } // Review comment
        public DateTime CreatedAt { get; set; }
    }
}
