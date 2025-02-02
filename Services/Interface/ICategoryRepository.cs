using ecommerce.Models;
using MongoDB.Bson;

namespace ecommerce.Services.Interface
{
    public interface ICategoryRepository
    {
        Task AddAsync(ProductCategoryModel category);

        Task<List<ProductCategoryModel>> GetAllAsync();

        Task<ProductCategoryModel> FindByIdAsync(string id);

        Task UpdateAsync(string id, ProductCategoryModel updatedCategory);

        Task DeleteAsync(string id);
    }
}
