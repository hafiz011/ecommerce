using ecommerce.Models;
using MongoDB.Bson;

namespace ecommerce.Services.Interface
{
    public interface IProductRepository
    {
        Task<List<ProductModel>> GetAllAsync();
        Task<ProductModel> GetByIdAsync(string id);
        Task AddAsync(ProductModel product);
        Task UpdateAsync(string id, ProductModel product);
        Task DeleteAsync(string id);
       
    }
}
