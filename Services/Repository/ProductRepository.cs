using ecommerce.DbContext;
using ecommerce.Models;
using ecommerce.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ecommerce.Services.Repository
{
    public class ProductRepository : IProductRepository
    {
        private readonly IMongoCollection<ProductModel> _products;

        public ProductRepository(MongoDbContext context)
        {
            _products = context.Products;
        }

        public async Task<List<ProductModel>> GetAllAsync()
        {
            return await _products.Find(_ => true).ToListAsync();
        }

        public async Task<ProductModel> GetByIdAsync(string id)
        {
            return await _products.Find(p => p.Id == id).FirstOrDefaultAsync();
        }

        public async Task AddAsync(ProductModel product)
        {
            await _products.InsertOneAsync(product);
        }

        public async Task UpdateAsync(string id, ProductModel product)
        {
            await _products.ReplaceOneAsync(p => p.Id == id, product);
        }

        public async Task DeleteAsync(string id)
        {
            await _products.DeleteOneAsync(p => p.Id == id);
        }
    }
}
