using ecommerce.DbContext;
using ecommerce.Models;
using ecommerce.Models.Dtos;
using ecommerce.Services.Interface;
using Microsoft.AspNetCore.SignalR;
using MongoDB.Bson;
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


        public async Task<(List<OrderModel> Orders, long TotalCount)> GetOrdersAsync(OrderFilterDto filter)
        {
            var builder = Builders<OrderModel>.Filter;
            var f = builder.Empty;

            if (!string.IsNullOrEmpty(filter.UserId))
                f &= builder.Eq(o => o.UserId, filter.UserId);

            if (!string.IsNullOrEmpty(filter.SellerId))
                f &= builder.Eq(o => o.SellerId, filter.SellerId);

            if (!string.IsNullOrEmpty(filter.OrderStatus))
                f &= builder.Eq(o => o.OrderStatus, filter.OrderStatus);

            if (!string.IsNullOrEmpty(filter.PaymentStatus))
                f &= builder.Eq("Payment.Status", filter.PaymentStatus);

            if (filter.From.HasValue)
                f &= builder.Gte(o => o.CreatedAt, filter.From.Value);

            if (filter.To.HasValue)
                f &= builder.Lte(o => o.CreatedAt, filter.To.Value);

            var total = await _orders.CountDocumentsAsync(f);

            var skip = (Math.Max(filter.Page, 1) - 1) * filter.PageSize;
            var orders = await _orders.Find(f)
                .SortByDescending(o => o.CreatedAt)
                .Skip(skip)
                .Limit(filter.PageSize)
                .ToListAsync();

            return (orders, total);
        }



        public async Task UpdateOrderStatusAsync(string orderId, string status)
        {
            var filter = Builders<OrderModel>.Filter.Eq(o => o.Id, orderId);
            var update = Builders<OrderModel>.Update
                .Set(o => o.OrderStatus, status)
                .Set(o => o.UpdatedAt, DateTime.UtcNow);

            await _orders.UpdateOneAsync(filter, update);

            // push timeline entry
            await AddStatusTimelineAsync(orderId, new StatusTimeline
            {
                Status = status,
                Message = $"Order status changed to {status}",
                UpadateAt = DateTime.UtcNow
            });
        }
        public async Task AddStatusTimelineAsync(string orderId, StatusTimeline timeline)
        {
            var filter = Builders<OrderModel>.Filter.Eq(o => o.Id, orderId);
            var update = Builders<OrderModel>.Update
                .Push(o => o.StatusTimeline, timeline)
                .Set(o => o.UpdatedAt, DateTime.UtcNow);

            await _orders.UpdateOneAsync(filter, update);
        }

        public async Task UpdatePaymentStatusAsync(string orderId, string paymentStatus, string? transactionId = null)
        {
            var filter = Builders<OrderModel>.Filter.Eq(o => o.Id, orderId);

            var updates = new List<UpdateDefinition<OrderModel>>();
            updates.Add(Builders<OrderModel>.Update.Set("Payment.Status", paymentStatus));
            updates.Add(Builders<OrderModel>.Update.Set(o => o.UpdatedAt, DateTime.UtcNow));

            if (!string.IsNullOrEmpty(transactionId))
            {
                updates.Add(Builders<OrderModel>.Update.Set("Payment.TransactionId", transactionId));
                updates.Add(Builders<OrderModel>.Update.Set("Payment.PaidAt", DateTime.UtcNow));
            }

            var update = Builders<OrderModel>.Update.Combine(updates);

            await _orders.UpdateOneAsync(filter, update);

            // timeline and notify
            var timeline = new StatusTimeline
            {
                Status = paymentStatus == "Paid" ? "Payment Received" : $"Payment {paymentStatus}",
                Message = paymentStatus == "Paid" ? "Payment confirmed." : $"Payment status updated to {paymentStatus}",
                UpadateAt = DateTime.UtcNow
            };

            await AddStatusTimelineAsync(orderId, timeline);
        }

        public async Task<DashboardStatsDto> GetDashboardStatsAsync(string sellerId, DateTime? from = null, DateTime? to = null)
        {
            // default last 7 days
            var end = to ?? DateTime.UtcNow;
            var start = from ?? end.AddDays(-6).Date;

            var builder = Builders<OrderModel>.Filter;
            var f = builder.Gte(o => o.CreatedAt, start) & builder.Lte(o => o.CreatedAt, end) & builder.Eq(o => o.SellerId, sellerId);

            var totalOrders = await _orders.CountDocumentsAsync(f);
            var totalRevenueAgg = await _orders.Aggregate()
                .Match(f)
                .Group(new BsonDocument
                {
                { "_id", BsonNull.Value },
                { "totalRevenue", new BsonDocument("$sum", "$TotalAmount") }
                }).FirstOrDefaultAsync();

            decimal totalRevenue = 0;
            if (totalRevenueAgg != null && totalRevenueAgg.Contains("totalRevenue"))
                totalRevenue = totalRevenueAgg["totalRevenue"].AsDecimal;

            // orders by status
            var statusAgg = await _orders.Aggregate()
                .Match(f)
                .Group(new BsonDocument
                {
                { "_id", "$OrderStatus" },
                { "count", new BsonDocument("$sum", 1) }
                }).ToListAsync();

            var ordersByStatus = statusAgg.ToDictionary(
                d => d["_id"].AsString,
                d => d["count"].ToInt64());

            // orders per day (last N days)
            var perDayAgg = await _orders.Aggregate()
                .Match(f)
                .Project(new BsonDocument
                {
                { "date", new BsonDocument("$dateToString", new BsonDocument { { "format", "%Y-%m-%d" }, { "date", "$CreatedAt" } }) },
                { "TotalAmount", "$TotalAmount" }
                })
                .Group(new BsonDocument
                {
                { "_id", "$date" },
                { "count", new BsonDocument("$sum", 1) },
                { "revenue", new BsonDocument("$sum", "$TotalAmount") }
                })
                .Sort(new BsonDocument("_id", 1))
                .ToListAsync();

            var ordersPerDay = perDayAgg.Select(d => new KeyValuePair<string, long>(d["_id"].AsString, d["count"].ToInt64())).ToList();
            var revenuePerDay = perDayAgg.Select(d => new KeyValuePair<string, decimal>(d["_id"].AsString, d["revenue"].ToDecimal())).ToList();

            return new DashboardStatsDto
            {
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                OrdersByStatus = ordersByStatus,
                OrdersPerDay = ordersPerDay,
                RevenuePerDay = revenuePerDay
            };
        }

    }
}
