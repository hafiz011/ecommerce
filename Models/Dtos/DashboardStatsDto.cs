namespace ecommerce.Models.Dtos
{
    public class DashboardStatsDto
    {
        public int TotalOrders { get; set; }
        public int PendingOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int ShippedOrders { get; set; }
        public int DeliveredOrders { get; set; }

        public decimal TotalSales { get; set; }
        public decimal TodaySales { get; set; }

        public List<DailySalesDto> SalesChart { get; set; } = new();
    }
    public class DailySalesDto
    {
        public string Date { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
