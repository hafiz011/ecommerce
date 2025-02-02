using ecommerce.Models;
using MongoDB.Bson;

namespace ecommerce.Services.Interface
{
    public interface IShoppingCartRepository
    {
        Task<ShoppingCartModel> GetCartByUserIdAsync(string userId);
        Task UpsertCartAsync(ShoppingCartModel cart);
    }
}
