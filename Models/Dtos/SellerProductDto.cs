namespace ecommerce.Models.Dtos
{
    public class SellerProductDto
    {
        public string Id { get; set; }
        public string Name { get; set; } // Product name
        public Dictionary<string, string> Attributes { get; set; } = new Dictionary<string, string>(); // Additional attributes (e.g., color, size)
        public string Description { get; set; } // Detailed description
        public string CategoryId { get; set; } // Reference to Category

        public decimal Price { get; set; } // Base price
        public List<string> Images { get; set; } = new List<string>(); // Multiple images
        public List<string> Tags { get; set; } = new List<string>(); // Tags for search optimization

        public int StockQuantity { get; set; } // Available stock
        public int Sold { get; set; } // Number of units sold
        public List<DiscountDto> Discounts { get; set; } = new List<DiscountDto>(); // List of discounts
        public bool IsNew { get; set; } // true for new, false for used
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime RestockDate { get; set; } // Expected restock date if out of stock
    }

    // Discount Model
    public class DiscountDto
    {
        public string Id { get; set; }
        public string Code { get; set; } // Discount code (e.g., "SAVE20")
        public decimal Percentage { get; set; } // Discount percentage
        public DateTime ValidFrom { get; set; } // Start date of the discount
        public DateTime ValidTo { get; set; } // End date of the discount
        public bool IsActive { get; set; } = true; // Is the discount currently active
        public string ProductId { get; set; } // Applies to a specific product, or null for all products
    }
}
