using ecommerce.DbContext;
using ecommerce.Models;
using ecommerce.Services.Interface;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ecommerce.Services.Repository
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly IMongoCollection<ProductCategoryModel> _categories;

        public CategoryRepository(MongoDbContext context)
        {
            _categories = context.Categories;
        }

        public async Task AddAsync(ProductCategoryModel category)
        {
            await _categories.InsertOneAsync(category);
        }

        public async Task DeleteAsync(string id)
        {
            await _categories.DeleteOneAsync(c => c.Id == id);
        }

        public async Task<ProductCategoryModel> FindByIdAsync(string id)
        {
            return await _categories.Find(c => c.Id == id).FirstOrDefaultAsync();
        }

        public async Task<List<ProductCategoryModel>> GetAllAsync()
        {
            return await _categories.Find(_ => true).ToListAsync();
        }

        public async Task UpdateAsync(string id, ProductCategoryModel updatedCategory)
        {
            await _categories.ReplaceOneAsync(c => c.Id == id, updatedCategory);
        }
    }
}
