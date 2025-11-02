using ecommerce.Models;
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

            // Create indexes for better performance
            await _context.Products.Indexes.CreateManyAsync(new[]
            {
                new CreateIndexModel<ProductModel>(Builders<ProductModel>.IndexKeys.Ascending(p => p.CategoryId)),
                new CreateIndexModel<ProductModel>(Builders<ProductModel>.IndexKeys.Ascending(p => p.Price)),
                new CreateIndexModel<ProductModel>(Builders<ProductModel>.IndexKeys.Descending(p => p.CreatedAt))
            });
        }

        private async Task SeedCategoriesAsync()
        {
            var categories = _context.Categories;

            if (await categories.CountDocumentsAsync(FilterDefinition<ProductCategoryModel>.Empty) > 0)
                return; // Already seeded

            var seedCategories = new List<ProductCategoryModel>();
            for (int i = 1; i <= 16; i++)
            {
                seedCategories.Add(new ProductCategoryModel
                {
                    Name = $"Category {i}",
                    ImagePath = $"category{i}.jpg",
                    Description = $"Description for Category {i}",
                    CreatedAt = DateTime.UtcNow
                });
            }

            await categories.InsertManyAsync(seedCategories);
        }

        private async Task SeedProductsAsync()
        {
            var products = _context.Products;
            var categories = _context.Categories;

            if (await products.CountDocumentsAsync(FilterDefinition<ProductModel>.Empty) > 0)
                return; // Already seeded

            var categoryList = await categories.Find(_ => true).ToListAsync();
            var seedProducts = new List<ProductModel>();
            var now = DateTime.UtcNow;

            for (int i = 1; i <= 100; i++)
            {
                var category = categoryList[_rand.Next(categoryList.Count)];
                var price = _rand.Next(50, 2000);
                var stock = _rand.Next(1, 300);
                var hasDiscount = _rand.Next(0, 2) == 1; // 50% chance
                var discounts = new List<Discount>();

                if (hasDiscount)
                {
                    discounts.Add(new Discount
                    {
                        Code = $"SAVE{_rand.Next(5, 30)}",
                        Percentage = _rand.Next(5, 30),
                        ValidFrom = now.AddDays(-_rand.Next(0, 10)),
                        ValidTo = now.AddMonths(1),
                        IsActive = true
                    });
                }

                // Generate sample reviews
                var reviews = new List<Review>();
                int reviewCount = _rand.Next(0, 10); // Up to 10 reviews
                for (int j = 1; j <= reviewCount; j++)
                {
                    reviews.Add(new Review
                    {
                        UserId = $"user{_rand.Next(1, 20)}",
                        UserName = $"User {_rand.Next(1, 100)}",
                        Rating = _rand.Next(1, 6),
                        Comment = $"This is a sample review {j} for Product {i}.",
                        CreatedAt = now.AddDays(-_rand.Next(0, 30))
                    });
                }

                seedProducts.Add(new ProductModel
                {
                    Name = $"Product {i}",
                    Description = $"Description for Product {i}",
                    CategoryId = category.Id,
                    Price = price,
                    Images = new List<string> { $"product{i % 10 + 1}.jpg" },
                    Tags = new List<string> { "Tag1", "Tag2", "Popular" },
                    StockQuantity = stock,
                    SellerId = $"seller{_rand.Next(1, 6)}",
                    IsNew = _rand.Next(0, 2) == 1,
                    Discounts = discounts,
                    CreatedAt = now.AddDays(-_rand.Next(0, 60)),
                    UpdatedAt = now,
                    RestockDate = now.AddDays(_rand.Next(10, 40)),
                    Review = reviews,
                    Sold = _rand.Next(0, 500)
                    // Note: averageRating is not stored, it's computed dynamically
                });
            }

            await products.InsertManyAsync(seedProducts);
        }
    }
}
