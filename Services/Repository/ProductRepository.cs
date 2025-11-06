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
                // Calculate average rating safely
                double averageRating = 0;
                if (p.Review != null && p.Review.Count > 0)
                {
                    averageRating = p.Review.Average(r => r.Rating);
                }
                return new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    ImageUrl = p.Images?.FirstOrDefault() ?? "default.jpg",
                    CategoryId = p.CategoryId,
                    IsNew = p.IsNew,
                    Rating = averageRating,
                    StockQuantity = p.StockQuantity,
                    Sold = p.Sold,
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

            var productDtos = items.Select(p =>
            {
                var activeDiscount = p.Discounts?.FirstOrDefault(d => d.IsActive && d.ValidFrom <= now && d.ValidTo >= now);
                // Calculate average rating safely
                double averageRating = 0;
                if (p.Review != null && p.Review.Count > 0)
                {
                    averageRating = p.Review.Average(r => r.Rating);
                }
                return new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    ImageUrl = p.Images?.FirstOrDefault() ?? "default.jpg",
                    CategoryId = p.CategoryId,
                    IsNew = p.IsNew,
                    Rating = averageRating,
                    StockQuantity = p.StockQuantity,
                    Sold = p.Sold,
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






        //public async Task<(List<ProductDto> items, int total)> GetFilteredPagedAsync(ProductFilter filter, int page, int pageSize)
        //{
        //    var now = DateTime.UtcNow;
        //    var nowBson = new BsonDateTime(now);

        //    var filterBuilder = Builders<ProductModel>.Filter;
        //    var conditions = new List<FilterDefinition<ProductModel>>();

        //    // Text search
        //    if (!string.IsNullOrWhiteSpace(filter.Search))
        //        conditions.Add(filterBuilder.Regex(p => p.Name, new BsonRegularExpression(filter.Search, "i")));

        //    // Category filter
        //    if (!string.IsNullOrWhiteSpace(filter.CategoryId))
        //        conditions.Add(filterBuilder.Eq(p => p.CategoryId, filter.CategoryId));

        //    var baseFilter = conditions.Any()
        //        ? filterBuilder.And(conditions)
        //        : FilterDefinition<ProductModel>.Empty;

        //    // Render filter for pipeline
        //    var renderedFilter = baseFilter.Render(
        //        BsonSerializer.LookupSerializer<ProductModel>(),
        //        BsonSerializer.SerializerRegistry
        //    );

        //    // Core aggregation pipeline
        //    var pipeline = new List<BsonDocument>
        //    {
        //        new("$match", renderedFilter),

        //        // Compute ActiveDiscount and FinalPrice efficiently
        //        new("$set", new BsonDocument
        //        {
        //            {
        //                "ActiveDiscount",
        //                new BsonDocument("$first", new BsonDocument("$filter", new BsonDocument
        //                {
        //                    { "input", "$Discounts" },
        //                    { "as", "d" },
        //                    { "cond", new BsonDocument("$and", new BsonArray
        //                        {
        //                            new BsonDocument("$eq", new BsonArray { "$$d.IsActive", true }),
        //                            new BsonDocument("$lte", new BsonArray { "$$d.ValidFrom", nowBson }),
        //                            new BsonDocument("$gte", new BsonArray { "$$d.ValidTo", nowBson })
        //                        })
        //                    }
        //                }))
        //            },
        //            {
        //                "FinalPrice",
        //                new BsonDocument("$cond", new BsonArray
        //                {
        //                    new BsonDocument("$gt", new BsonArray { "$ActiveDiscount", BsonNull.Value }),
        //                    new BsonDocument("$subtract", new BsonArray
        //                    {
        //                        "$Price",
        //                        new BsonDocument("$multiply", new BsonArray
        //                        {
        //                            "$Price",
        //                            new BsonDocument("$divide", new BsonArray { "$ActiveDiscount.Percentage", 100 })
        //                        })
        //                    }),
        //                    "$Price"
        //                })
        //            }
        //        })
        //    };

        //    // Apply price range filter on server
        //    if (filter.MinPrice.HasValue || filter.MaxPrice.HasValue)
        //    {
        //        var priceRange = new BsonDocument();
        //        if (filter.MinPrice.HasValue) priceRange.Add("$gte", filter.MinPrice.Value);
        //        if (filter.MaxPrice.HasValue) priceRange.Add("$lte", filter.MaxPrice.Value);
        //        pipeline.Add(new("$match", new BsonDocument("FinalPrice", priceRange)));
        //    }

        //    // Count before paging
        //    var countPipeline = new List<BsonDocument>(pipeline)
        //    {
        //        new("$count", "total")
        //    };

        //            var countResult = await _products.Aggregate<BsonDocument>(countPipeline).FirstOrDefaultAsync();
        //            var total = countResult?["total"].AsInt32 ?? 0;

        //            // Pagination and sorting
        //            pipeline.AddRange(new[]
        //            {
        //        new BsonDocument("$sort", new BsonDocument("CreatedAt", -1)),
        //        new BsonDocument("$skip", (page - 1) * pageSize),
        //        new BsonDocument("$limit", pageSize)
        //    });

        //    // Execute aggregation
        //    var results = await _products.Aggregate<BsonDocument>(pipeline).ToListAsync();

        //    // Map to DTO using your old mapping logic
        //    var productDtos = results.Select(r =>
        //    {
        //        var price = r["Price"].ToDecimal();
        //        var finalPrice = r.Contains("FinalPrice") ? r["FinalPrice"].ToDecimal() : price;

        //        Discount activeDiscount = null;
        //        if (r.Contains("ActiveDiscount") && !r["ActiveDiscount"].IsBsonNull)
        //        {
        //            var ad = r["ActiveDiscount"].AsBsonDocument;
        //            activeDiscount = new Discount
        //            {
        //                Id = ad["_id"].AsString,
        //                Code = ad["Code"].AsString,
        //                Percentage = ad["Percentage"].ToDecimal(),
        //                ValidFrom = ad["ValidFrom"].ToUniversalTime(),
        //                ValidTo = ad["ValidTo"].ToUniversalTime(),
        //                IsActive = ad["IsActive"].ToBoolean(),
        //                ProductId = ad.Contains("ProductId") ? ad["ProductId"].AsString : null
        //            };
        //        }

        //        return new ProductDto
        //        {
        //            Id = r["_id"].AsObjectId.ToString(),
        //            Name = r["Name"].AsString,
        //            Description = r.Contains("Description") ? r["Description"].AsString : null,
        //            CategoryId = r["CategoryId"].AsString,
        //            Price = price,
        //            FinalPrice = finalPrice,
        //            HasActiveDiscount = activeDiscount != null,
        //            DiscountPercent = activeDiscount?.Percentage ?? 0,
        //            ImageUrl = r.Contains("Images") && r["Images"].AsBsonArray.Count > 0
        //                ? r["Images"].AsBsonArray[0].AsString
        //                : "default.jpg",
        //            Images = r.Contains("Images") ? r["Images"].AsBsonArray.Select(i => i.AsString).ToList() : new List<string>(),
        //            Tags = r.Contains("Tags") ? r["Tags"].AsBsonArray.Select(i => i.AsString).ToList() : new List<string>(),
        //            StockQuantity = r["StockQuantity"].ToInt32(),
        //            IsNew = r["IsNew"].ToBoolean(),
        //            Rating = r["Rating"].ToDouble(),
        //            SellerId = r["SellerId"].AsString,
        //            CreatedAt = r["CreatedAt"].ToUniversalTime()
        //        };
        //    }).ToList();


        //    return (productDtos, total);
        //}

        
        public async Task<List<ProductDto>> GetProductBySellerIdAsync(string userId)
        {
            var products = await _products.Find(p => p.SellerId == userId).ToListAsync();
            return products.Select(p =>
            {
                var now = DateTime.UtcNow;
                var activeDiscount = p.Discounts?
                    .FirstOrDefault(d => d.IsActive && d.ValidFrom <= now && d.ValidTo >= now);

                // Calculate average rating safely
                double averageRating = 0;
                if (p.Review != null && p.Review.Count > 0)
                {
                    averageRating = p.Review.Average(r => r.Rating);
                }
                return new ProductDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price,
                    ImageUrl = p.Images?.FirstOrDefault() ?? "default.jpg",
                    CategoryId = p.CategoryId,
                    IsNew = p.IsNew,
                    Rating = averageRating,
                    StockQuantity = p.StockQuantity,
                    Sold = p.Sold,
                    CreatedAt = p.CreatedAt,
                    SellerId = p.SellerId,
                    Tags = p.Tags ?? new List<string>(),
                    Images = p.Images ?? new List<string>(),
                    HasActiveDiscount = activeDiscount != null,
                    DiscountPercent = activeDiscount?.Percentage ?? 0,
                    FinalPrice = p.Price - ((activeDiscount?.Percentage ?? 0) * p.Price / 100)
                };
            }).ToList();
        }

         
    }
}
