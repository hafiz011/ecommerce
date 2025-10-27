using ecommerce.DbContext;
using ecommerce.Models;
using ecommerce.Models.Dtos;
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



        // Page by page (no filters)
        public async Task<(List<ProductDto> items, int total)> GetPagedAsync(int page, int pageSize)
        {
            var total = (int)await _products.CountDocumentsAsync(_ => true);
            var now = DateTime.UtcNow;

            var items = await _products.Find(_ => true)
                .SortByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            var productDtos = items.Select(p =>
            {
                var activeDiscount = p.Discounts?.FirstOrDefault(d => d.IsActive && d.ValidFrom <= now && d.ValidTo >= now);

                return new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    ImageUrl = p.Images?.FirstOrDefault() ?? "default.jpg",
                    CategoryId = p.CategoryId,
                    IsNew = p.IsNew,
                    Rating = p.Rating,
                    StockQuantity = p.StockQuantity,
                    CreatedAt = p.CreatedAt,
                    SellerId = p.SellerId,
                    Tags = p.Tags ?? new List<string>(),
                    Images = p.Images ?? new List<string>(),
                    HasActiveDiscount = activeDiscount != null,
                    DiscountPercent = activeDiscount?.Percentage ?? 0,
                    FinalPrice = p.Price - ((activeDiscount?.Percentage ?? 0) * p.Price / 100)
                };
            }).ToList();
            return (productDtos, total);
        }



        // Page by page including filters
        public async Task<(List<ProductDto> items, int total)> GetFilteredPagedAsync(ProductFilter filter, int page, int pageSize)
        {
            var builder = Builders<ProductModel>.Filter;
            var conditions = new List<FilterDefinition<ProductModel>>();

            if (!string.IsNullOrWhiteSpace(filter.Search))
                conditions.Add(builder.Regex(p => p.Name,
                    new MongoDB.Bson.BsonRegularExpression(filter.Search, "i")));

            if (!string.IsNullOrWhiteSpace(filter.CategoryId))
                conditions.Add(builder.Eq(p => p.CategoryId, filter.CategoryId));

            if (filter.MinPrice.HasValue)
                conditions.Add(builder.Gte(p => p.Price, filter.MinPrice.Value));

            if (filter.MaxPrice.HasValue)
                conditions.Add(builder.Lte(p => p.Price, filter.MaxPrice.Value));

            var finalFilter = conditions.Any()
                ? builder.And(conditions)
                : builder.Empty;

            var total = (int)await _products.CountDocumentsAsync(finalFilter);

            var now = DateTime.UtcNow;

            var items = await _products.Find(finalFilter)
                .SortByDescending(p => p.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            var productDtos = items.Select(p => {
                var activeDiscount = p.Discounts?.FirstOrDefault(d => d.IsActive && d.ValidFrom <= now && d.ValidTo >= now);
                var discountPercent = activeDiscount?.Percentage ?? 0;

                return new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    ImageUrl = p.Images?.FirstOrDefault() ?? "default.jpg",
                    CategoryId = p.CategoryId,
                    IsNew = p.IsNew,
                    Rating = p.Rating,
                    StockQuantity = p.StockQuantity,
                    CreatedAt = p.CreatedAt,
                    SellerId = p.SellerId,
                    Tags = p.Tags ?? new List<string>(),
                    Images = p.Images ?? new List<string>(),
                    HasActiveDiscount = activeDiscount != null,
                    DiscountPercent = discountPercent,
                    FinalPrice = p.Price - (discountPercent * p.Price / 100)
                };
            }).ToList();
            return (productDtos, total);
        }


    }
}
