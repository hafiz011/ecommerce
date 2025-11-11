using ecommerce.DbContext;
using ecommerce.Models;
using ecommerce.Services.Interface;
using MongoDB.Driver;

namespace ecommerce.Repositories
{
    public class OrderRepository : IOrderRepository
    {
        private readonly IMongoCollection<OrderModel> _orders;

        public OrderRepository(MongoDbContext context)
        {
            _orders = context.Orders;
        }

        public async Task CreateOrderAsync(OrderModel order)
        {
            await _orders.InsertOneAsync(order);
        }

        public async Task<List<OrderModel>> GetOrdersByUserAsync(string userId)
        {
            return await _orders.Find(o => o.UserId == userId)
                                .SortByDescending(o => o.CreatedAt)
                                .ToListAsync();
        }

        public async Task<List<OrderModel>> GetOrdersBySellerAsync(string sellerId)
        {
            return await _orders.Find(o => o.SellerId == sellerId)
                                .SortByDescending(o => o.CreatedAt)
                                .ToListAsync();
        }

        public async Task<OrderModel> GetOrderByIdAsync(string orderId)
        {
            return await _orders.Find(o => o.Id == orderId).FirstOrDefaultAsync();
        }

        public async Task UpdateOrderStatusAsync(string orderId, string status)
        {
            var update = Builders<OrderModel>.Update
                .Set(o => o.OrderStatus, status)
                .Set(o => o.UpdatedAt, System.DateTime.UtcNow);

            await _orders.UpdateOneAsync(o => o.Id == orderId, update);
        }

        public async Task UpdatePaymentStatusAsync(string id, string paymentStatus)
        {
            var update = Builders<OrderModel>.Update
                .Set(o => o.PaymentStatus, paymentStatus)
                .Set(o => o.UpdatedAt, DateTime.UtcNow);

            await _orders.UpdateOneAsync(o => o.Id == id, update);
        }
    }
}
