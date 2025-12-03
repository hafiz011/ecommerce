namespace ecommerce.Models.Dtos
{
    public class ProductDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string CategoryId { get; set; }
        public string CategoryName { get; set; }
        public List<string> Images { get; set; } = new();
        public decimal Price { get; set; }
        public decimal FinalPrice { get; set; } // Price after active discount
        public int StockQuantity { get; set; }
        public int Sold { get; set; }
        public bool IsNew { get; set; }
        public double Rating { get; set; } // (future review rating calculation)
        public bool HasActiveDiscount { get; set; }
        public decimal DiscountPercent { get; set; }
    }
}
