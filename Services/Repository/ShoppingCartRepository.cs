using ecommerce.DbContext;
using ecommerce.Models;
using ecommerce.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ecommerce.Services.Repository
{
    public class ShoppingCartRepository : IShoppingCartRepository
    {
        private readonly IMongoCollection<ShoppingCartModel> _shopping;
        public ShoppingCartRepository(MongoDbContext context)
        {
            _shopping = context.ShoppingCart;
        }
        public async Task<ShoppingCartModel> GetCartByUserIdAsync(string userId)
        {
            return await _shopping.Find(c => c.UserId == userId).FirstOrDefaultAsync();
        }

        public async Task UpsertCartAsync(ShoppingCartModel cart)
        {
            var filter = Builders<ShoppingCartModel>.Filter.Eq(c => c.Id, cart.Id);
            await _shopping.ReplaceOneAsync(filter, cart, new ReplaceOptions { IsUpsert = true });
        }


    }
}
