using ecommerce.Models;
using ecommerce.Models.Dtos;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace ecommerce.Services.Interface
{
    public interface IProductRepository
    {
        Task<List<ProductModel>> GetAllAsync();
        Task<ProductModel> GetByIdAsync(string id);
        Task AddAsync(ProductModel product);

        //Task<bool> UpdateAsync(string id, ProductModel product);
        Task<bool> UpdateProductAsync(ProductModel updatedProduct);
        Task<bool> DeleteAsync(string id);

        Task<(List<ProductDto> items, int total)> GetPagedAsync(int page, int pageSize);
        Task<(List<ProductDto> items, int total)> GetFilteredPagedAsync(ProductFilter filter, int page, int pageSize);
        Task<List<ProductDto>> GetProductBySellerIdAsync(string userId);

        // Variants
        Task<bool> AddVariantAsync(string productId, ProductVariant variant);
        Task<bool> UpdateVariantAsync(string productId, ProductVariant variant);
        Task<bool> DeleteVariantAsync(string productId, string variantId);

        // Inventory / Stock
        Task<bool> UpdateVariantStockAsync(string productId, string variantId, int newStock);
        Task<bool> IncreaseStockAsync(string productId, string variantId, int amount);
        Task<bool> DecreaseStockAsync(string productId, string variantId, int amount);
    }
}
