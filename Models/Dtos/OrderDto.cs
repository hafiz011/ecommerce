namespace ecommerce.Models.Dtos
{
    public class OrderDto
    {
        public string Id { get; set; }
        public List<OrderItem> Items { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal TotalAmount { get; set; }

        public string PaymentMethod { get; set; }
        public string PaymentStatus { get; set; }
        public string OrderStatus { get; set; }

        public ShippingAddress ShippingAddress { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
    public class OrderItem
    {
        public string ProductId { get; set; }
        public string ProductName { get; set; }
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public string Image { get; set; }
    }

    public class ShippingAddress
    {
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
    }
}
