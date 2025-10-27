namespace ecommerce.Models.Dtos
{
    public class ProductDto
    {
        public string Id { get; set; }

        public string Name { get; set; }
        public string? Description { get; set; }

        public string CategoryId { get; set; }

        public decimal Price { get; set; }
        public decimal FinalPrice { get; set; } // Price after active discount

        public string? ImageUrl { get; set; } // preview image

        public int StockQuantity { get; set; }
        public bool IsNew { get; set; }

        public double Rating { get; set; } // (future review rating calculation)

        public List<string> Tags { get; set; } = new();
        public List<string> Images { get; set; } = new();

        public bool HasActiveDiscount { get; set; }
        public decimal DiscountPercent { get; set; }

        public string SellerId { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
