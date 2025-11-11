using ecommerce.Models;

namespace ecommerce.Services.Interface
{
    public interface IOrderRepository
    {
        Task CreateOrderAsync(OrderModel order);
        Task<List<OrderModel>> GetOrdersByUserAsync(string userId);
        Task<List<OrderModel>> GetOrdersBySellerAsync(string sellerId);
        Task<OrderModel> GetOrderByIdAsync(string orderId);
        Task UpdateOrderStatusAsync(string orderId, string status);
        Task UpdatePaymentStatusAsync(string id, string v);
    }
}
