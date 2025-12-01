using ecommerce.Models;
using ecommerce.Models.Dtos;

namespace ecommerce.Services.Interface
{
    public interface IOrderRepository
    {
        Task CreateOrderAsync(OrderModel order);
        Task<List<OrderModel>> GetOrdersByUserAsync(string userId);
        Task<List<OrderModel>> GetOrdersBySellerAsync(string sellerId);
        Task<OrderModel> GetOrderByIdAsync(string orderId);
        //Task UpdateOrderStatusAsync(string orderId, string status);
        //Task UpdatePaymentStatusAsync(string id, string v);
        Task<(List<OrderModel> Orders, long TotalCount)> GetOrdersAsync(OrderFilterDto filter); // with filtering and pagination
        Task UpdateOrderStatusAsync(string orderId, string status);
        Task AddStatusTimelineAsync(string orderId, StatusTimeline timeline);
        Task UpdatePaymentStatusAsync(string orderId, string paymentStatus, string? transactionId = null);
        Task<DashboardStatsDto> GetDashboardStatsAsync(string sellerId, DateTime? from = null, DateTime? to = null);



    }
}
