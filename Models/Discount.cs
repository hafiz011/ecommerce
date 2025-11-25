namespace ecommerce.Models
{
    public class Discount
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Code { get; set; } // Discount code (e.g., "SAVE20")
        public decimal Percentage { get; set; } // Discount percentage
        public DateTime ValidFrom { get; set; } // Start date of the discount
        public DateTime ValidTo { get; set; } // End date of the discount
        public bool IsActive { get; set; } = true; // Is the discount currently active
    }
}
