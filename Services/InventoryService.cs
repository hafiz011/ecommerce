using ecommerce.Services.Interface;

namespace ecommerce.Services
{
    public class InventoryService
    {
        private readonly IProductRepository _productRepo;

        public InventoryService(IProductRepository productRepo)
        {
            _productRepo = productRepo;
        }

        // Decrease stock (checkout)
        public async Task<bool> ReserveStockAsync(string productId, string variantId, int qty)
        {
            return await _productRepo.DecreaseStockAsync(productId, variantId, qty);
        }

        // Return stock (payment fail/cancel)
        public async Task<bool> ReleaseStockAsync(string productId, string variantId, int qty)
        {
            return await _productRepo.IncreaseStockAsync(productId, variantId, qty);
        }

        // Admin restocking
        public async Task<bool> UpdateStockAsync(string productId, string variantId, int newStock)
        {
            return await _productRepo.UpdateVariantStockAsync(productId, variantId, newStock);
        }
    }

}
