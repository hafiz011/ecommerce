namespace ecommerce.Models.Dtos
{
    public class DashboardStatsDto
    {
        public long TotalOrders { get; set; }
        public decimal TotalRevenue { get; set; }
        public Dictionary<string, long> OrdersByStatus { get; set; } = new();
        public List<KeyValuePair<string, long>> OrdersPerDay { get; set; } = new(); // date string, count
        public List<KeyValuePair<string, decimal>> RevenuePerDay { get; set; } = new();
    }
}
