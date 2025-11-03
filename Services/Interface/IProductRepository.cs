using ecommerce.Models;
using ecommerce.Models.Dtos;
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
        Task<(List<ProductDto> items, int total)> GetPagedAsync(int page, int pageSize);
        Task<(List<ProductDto> items, int total)> GetFilteredPagedAsync(ProductFilter filter, int page, int pageSize);
        Task<List<ProductDto>> GetProductBySellerIdAsync(string userId);
    }
}
