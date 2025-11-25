using ecommerce.DbContext;
using ecommerce.Models;
using ecommerce.Models.Dtos;
using ecommerce.Services.Interface;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
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

        public async Task<bool> UpdateProductAsync(ProductModel updatedProduct)
        {
            updatedProduct.UpdatedAt = DateTime.UtcNow;
            var result = await _products.ReplaceOneAsync(x => x.Id == updatedProduct.Id, updatedProduct);
            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteAsync(string id)
        {
            var result = await _products.DeleteOneAsync(x => x.Id == id);
            return result.DeletedCount > 0;
        }

        // variants
        public async Task<bool> AddVariantAsync(string productId, ProductVariant variant)
        {
            variant.VariantId = Guid.NewGuid().ToString();

            var update = Builders<ProductModel>.Update
                .AddToSet(x => x.Variants, variant)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _products.UpdateOneAsync(
                x => x.Id == productId,
                update
            );

            return result.ModifiedCount > 0;
        }

        public async Task<bool> UpdateVariantAsync(string productId, ProductVariant variant)
        {
            var update = Builders<ProductModel>.Update
                .Set(x => x.Variants[-1], variant)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _products.UpdateOneAsync(
                x => x.Id == productId && x.Variants.Any(v => v.VariantId == variant.VariantId),
                update
            );

            return result.ModifiedCount > 0;
        }

        public async Task<bool> DeleteVariantAsync(string productId, string variantId)
        {
            var update = Builders<ProductModel>.Update
                .PullFilter(x => x.Variants, v => v.VariantId == variantId)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _products.UpdateOneAsync(
                x => x.Id == productId,
                update
            );

            return result.ModifiedCount > 0;
        }

        // inventory / stock
        public async Task<bool> UpdateVariantStockAsync(string productId, string variantId, int newStock)
        {
            var update = Builders<ProductModel>.Update
                .Set(x => x.Variants[-1].Stock, newStock)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _products.UpdateOneAsync(
                x => x.Id == productId && x.Variants.Any(v => v.VariantId == variantId),
                update
            );

            return result.ModifiedCount > 0;
        }

        public async Task<bool> IncreaseStockAsync(string productId, string variantId, int amount)
        {
            var update = Builders<ProductModel>.Update
                .Inc(x => x.Variants[-1].Stock, amount)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _products.UpdateOneAsync(
                x => x.Id == productId && x.Variants.Any(v => v.VariantId == variantId),
                update
            );

            return result.ModifiedCount > 0;
        }

        public async Task<bool> DecreaseStockAsync(string productId, string variantId, int amount)
        {
            var update = Builders<ProductModel>.Update
                .Inc(x => x.Variants[-1].Stock, -amount)
                .Set(x => x.UpdatedAt, DateTime.UtcNow);

            var result = await _products.UpdateOneAsync(
                x => x.Id == productId && x.Variants.Any(v => v.VariantId == variantId),
                update
            );

            return result.ModifiedCount > 0;
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
                var DiscountPrice = p.BasePrice - ((activeDiscount?.Percentage ?? 0) * p.BasePrice / 100);
                // Calculate average rating safely
                double averageRating = 0;
                if (p.Review != null && p.Review.Count > 0)
                {
                    averageRating = p.Review.Average(r => r.Rating);
                }
                var images = new List<string>();
                if (p.Variants != null && p.Variants.Count > 0)
                {
                    // Variant 1 → first image
                    var img1 = p.Variants[0].Images?.FirstOrDefault();
                    if (!string.IsNullOrEmpty(img1))
                        images.Add(img1);

                    // Variant 2 → first image
                    if (p.Variants.Count > 1)
                    {
                        var img2 = p.Variants[1].Images?.FirstOrDefault();
                        if (!string.IsNullOrEmpty(img2))
                            images.Add(img2);
                    }
                }
                return new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    CategoryId = p.CategoryId,
                    CategoryName = p.CategoryName,
                    Images = images,
                    Price = p.BasePrice,
                    FinalPrice = Math.Floor(DiscountPrice) + ((DiscountPrice % 1) >= 0.5m ? 1 : 0),
                    StockQuantity = p.Variants?.Sum(v => v.Stock) ?? 0,
                    Sold = p.Sold,
                    IsNew = p.IsNew,
                    Rating = averageRating,
                    HasActiveDiscount = activeDiscount != null,
                    DiscountPercent = activeDiscount?.Percentage ?? 0,
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
                conditions.Add(builder.Gte(p => p.BasePrice, filter.MinPrice.Value));

            if (filter.MaxPrice.HasValue)
                conditions.Add(builder.Lte(p => p.BasePrice, filter.MaxPrice.Value));

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

            var productDtos = items.Select(p =>
            {
                var activeDiscount = p.Discounts?.FirstOrDefault(d => d.IsActive && d.ValidFrom <= now && d.ValidTo >= now);
                var DiscountPrice = p.BasePrice - ((activeDiscount?.Percentage ?? 0) * p.BasePrice / 100);
                // Calculate average rating safely
                double averageRating = 0;
                if (p.Review != null && p.Review.Count > 0)
                {
                    averageRating = p.Review.Average(r => r.Rating);
                }
                var images = new List<string>();
                if (p.Variants != null && p.Variants.Count > 0)
                {
                    // Variant 1 → first image
                    var img1 = p.Variants[0].Images?.FirstOrDefault();
                    if (!string.IsNullOrEmpty(img1))
                        images.Add(img1);

                    // Variant 2 → first image
                    if (p.Variants.Count > 1)
                    {
                        var img2 = p.Variants[1].Images?.FirstOrDefault();
                        if (!string.IsNullOrEmpty(img2))
                            images.Add(img2);
                    }
                }
                return new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    CategoryId = p.CategoryId,
                    CategoryName = p.CategoryName,
                    Images = images,
                    Price = p.BasePrice,
                    FinalPrice = Math.Floor(DiscountPrice) + ((DiscountPrice % 1) >= 0.5m ? 1 : 0),
                    StockQuantity = p.Variants?.Sum(v => v.Stock) ?? 0,
                    Sold = p.Sold,
                    IsNew = p.IsNew,
                    Rating = averageRating,
                    HasActiveDiscount = activeDiscount != null,
                    DiscountPercent = activeDiscount?.Percentage ?? 0,
                };
            }).ToList();
            return (productDtos, total);
        }

        
        public async Task<List<ProductDto>> GetProductBySellerIdAsync(string userId)
        {
            var products = await _products.Find(p => p.SellerId == userId).ToListAsync();
            return products.Select(p =>
            {
                var now = DateTime.UtcNow;
                var activeDiscount = p.Discounts?
                    .FirstOrDefault(d => d.IsActive && d.ValidFrom <= now && d.ValidTo >= now);

                var DiscountPrice = p.BasePrice - ((activeDiscount?.Percentage ?? 0) * p.BasePrice / 100);

                // Calculate average rating safely
                double averageRating = 0;
                if (p.Review != null && p.Review.Count > 0)
                {
                    averageRating = p.Review.Average(r => r.Rating);
                }

                var images = new List<string>();

                if (p.Variants != null && p.Variants.Count > 0)
                {
                    // Variant 1 → first image
                    var img1 = p.Variants[0].Images?.FirstOrDefault();
                    if (!string.IsNullOrEmpty(img1))
                        images.Add(img1);

                    // Variant 2 → first image
                    if (p.Variants.Count > 1)
                    {
                        var img2 = p.Variants[1].Images?.FirstOrDefault();
                        if (!string.IsNullOrEmpty(img2))
                            images.Add(img2);
                    }
                }

                return new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    CategoryId = p.CategoryId,
                    CategoryName = p.CategoryName,
                    Images = images,
                    Price = p.BasePrice,
                    FinalPrice = Math.Floor(DiscountPrice) + ((DiscountPrice % 1) >= 0.5m ? 1 : 0),
                    StockQuantity = p.Variants?.Sum(v => v.Stock) ?? 0,
                    Sold = p.Sold,
                    IsNew = p.IsNew,
                    Rating = averageRating,
                    HasActiveDiscount = activeDiscount != null,
                    DiscountPercent = activeDiscount?.Percentage ?? 0,
                };
            }).ToList();
        }

         
    }
}
