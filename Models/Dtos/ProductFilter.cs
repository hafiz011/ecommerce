namespace ecommerce.Models.Dtos
{
    public class ProductFilter
    {
        public string? Search { get; set; }
        public string? CategoryId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
    }
}
