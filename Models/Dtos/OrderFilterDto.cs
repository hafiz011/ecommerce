namespace ecommerce.Models.Dtos
{
    public class OrderFilterDto
    {
        public string? UserId { get; set; }
        public string? SellerId { get; set; }
        public string? OrderStatus { get; set; }
        public string? PaymentStatus { get; set; }
        public DateTime? From { get; set; }
        public DateTime? To { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }
}
