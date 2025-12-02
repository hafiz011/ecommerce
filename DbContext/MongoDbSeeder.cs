using ecommerce.Models;
using MongoDB.Bson;
using MongoDB.Driver;

namespace ecommerce.DbContext
{
    public class MongoDbSeeder
    {
        private readonly MongoDbContext _context;
        private readonly Random _rand = new();

        public MongoDbSeeder(MongoDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            await SeedCategoriesAsync();
            await SeedProductsAsync();

            // Create indexes
            await _context.Products.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<ProductModel>(Builders<ProductModel>.IndexKeys.Ascending(p => p.CategoryId)),
                new CreateIndexModel<ProductModel>(Builders<ProductModel>.IndexKeys.Ascending(p => p.BasePrice)),
                new CreateIndexModel<ProductModel>(Builders<ProductModel>.IndexKeys.Descending(p => p.CreatedAt))
            });
        }

        private async Task SeedCategoriesAsync()
        {
            var categories = _context.Categories;
            if (await categories.CountDocumentsAsync(FilterDefinition<ProductCategoryModel>.Empty) > 0)
                return;

            var seedCategories = Enumerable.Range(1, 16)
                .Select(i => new ProductCategoryModel
                {
                    Name = $"Category {i}",
                    ImagePath = $"category{i}.jpg",
                    Description = $"Description for Category {i}",
                    CreatedAt = DateTime.UtcNow,
                    SellerId = "4e4701f6-dc19-4d94-81d6-0836d8e6e749"
                }).ToList();

            await categories.InsertManyAsync(seedCategories);
        }

        private async Task SeedProductsAsync()
        {
            var products = _context.Products;
            var categories = await _context.Categories.Find(_ => true).ToListAsync();

            if (await products.CountDocumentsAsync(FilterDefinition<ProductModel>.Empty) > 0)
                return;

            var seedProducts = new List<ProductModel>();
            var now = DateTime.UtcNow;

            for (int i = 1; i <= 50; i++)
            {
                var category = categories[_rand.Next(categories.Count)];
                var basePrice = _rand.Next(50, 2000);
                var isNew = _rand.Next(0, 2) == 1;

                // Generate attributes
                var attributes = new Dictionary<string, List<string>>
                {
                    { "Color", new List<string> { "Red", "Blue", "Green", "Black" }.OrderBy(_ => _rand.Next()).Take(2).ToList() },
                    { "Size", new List<string> { "S", "M", "L", "XL" }.OrderBy(_ => _rand.Next()).Take(2).ToList() }
                };

                // Generate variants
                var variants = new List<ProductVariant>();
                foreach (var color in attributes["Color"])
                {
                    foreach (var size in attributes["Size"])
                    {
                        variants.Add(new ProductVariant
                        {
                            VariantId = Guid.NewGuid().ToString(),
                            Color = color,
                            Size = size,
                            SKU = $"SKU-{i}-{color[0]}{size}",
                            Price = basePrice + _rand.Next(0, 50),
                            Stock = _rand.Next(1, 100),
                            Images = new List<string> { $"product{i % 10 + 1}.jpg" },
                        });
                    }
                }

                // Generate discounts
                var discounts = new List<Discount>();
                if (_rand.Next(0, 2) == 1) // 50% chance
                {
                    discounts.Add(new Discount
                    {
                        Id = Guid.NewGuid().ToString(),
                        Code = $"SAVE{_rand.Next(5, 30)}",
                        Percentage = _rand.Next(5, 30),
                        ValidFrom = now.AddDays(-_rand.Next(0, 10)),
                        ValidTo = now.AddDays(_rand.Next(15, 60)),
                        IsActive = true
                    });
                }

                // Generate reviews
                var reviews = new List<Review>();
                for (int j = 0; j < _rand.Next(0, 10); j++)
                {
                    reviews.Add(new Review
                    {
                        UserId = $"user{_rand.Next(1, 50)}",
                        UserName = $"User {_rand.Next(1, 100)}",
                        Rating = _rand.Next(1, 6),
                        Comment = $"Sample review {j + 1} for Product {i}.",
                        CreatedAt = now.AddDays(-_rand.Next(0, 30))
                    });
                }

                seedProducts.Add(new ProductModel
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    Name = $"Product {i}",
                    Description = $"Description for Product {i}",
                    CategoryId = category.Id,
                    CategoryName = category.Name,
                    BasePrice = basePrice,
                    Attributes = attributes,
                    Variants = variants,
                    Tags = new List<string> { "Popular", "Sale", "New" },
                    SellerId = "4e4701f6-dc19-4d94-81d6-0836d8e6e749",
                    IsNew = isNew,
                    Discounts = discounts,
                    CreatedAt = now.AddDays(-_rand.Next(0, 60)),
                    UpdatedAt = now,
                    RestockDate = now.AddDays(_rand.Next(10, 40)),
                    Review = reviews,
                    Sold = _rand.Next(0, 500)
                });
            }

            await products.InsertManyAsync(seedProducts);
        }
    }
}
