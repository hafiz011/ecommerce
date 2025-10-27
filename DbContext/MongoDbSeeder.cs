using ecommerce.Models;
using MongoDB.Driver;

namespace ecommerce.DbContext
{
    public class MongoDbSeeder
    {
        private readonly MongoDbContext _context;

        public MongoDbSeeder(MongoDbContext context)
        {
            _context = context;
        }

        public async Task SeedAsync()
        {
            await SeedCategoriesAsync();
            await SeedProductsAsync();
        }

        private async Task SeedCategoriesAsync()
        {
            var categories = _context.Categories;

            if (await categories.CountDocumentsAsync(FilterDefinition<ProductCategoryModel>.Empty) > 0)
                return; // Already seeded

            var seedCategories = new List<ProductCategoryModel>
            {
                new() { Name = "Electronics", ImagePath = "electronics.jpg", Description = "All electronic items", CreatedAt = DateTime.UtcNow },
                new() { Name = "Clothing", ImagePath = "clothing.jpg", Description = "Men & Women Clothing", CreatedAt = DateTime.UtcNow },
                new() { Name = "Books", ImagePath = "books.jpg", Description = "Books & Stationery", CreatedAt = DateTime.UtcNow },
                new() { Name = "Home & Kitchen", ImagePath = "home_kitchen.jpg", Description = "Home & Kitchen Essentials", CreatedAt = DateTime.UtcNow }
            };

            await categories.InsertManyAsync(seedCategories);
        }

        private async Task SeedProductsAsync()
        {
            var products = _context.Products;
            var categories = _context.Categories;

            if (await products.CountDocumentsAsync(FilterDefinition<ProductModel>.Empty) > 0)
                return; // Already seeded

            var categoryList = await categories.Find(_ => true).ToListAsync();

            var seedProducts = new List<ProductModel>
            {
                new()
                {
                    Name = "iPhone 15",
                    Description = "Latest Apple iPhone 15",
                    CategoryId = categoryList.First(c => c.Name == "Electronics").Id,
                    Price = 1200,
                    Images = new List<string> { "iphone15-1.jpg", "iphone15-2.jpg" },
                    Tags = new List<string> { "Smartphone", "Apple" },
                    StockQuantity = 50,
                    SellerId = "seller1",
                    IsNew = true,
                    Rating = 4.8,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Discounts = new List<Discount>
                    {
                        new Discount
                        {
                            Code = "SAVE10",
                            Percentage = 10,
                            ValidFrom = DateTime.UtcNow,
                            ValidTo = DateTime.UtcNow.AddMonths(1),
                            IsActive = true
                        }
                    }
                },
                new()
                {
                    Name = "Men's T-Shirt",
                    Description = "Comfortable cotton t-shirt",
                    CategoryId = categoryList.First(c => c.Name == "Clothing").Id,
                    Price = 20,
                    Images = new List<string> { "tshirt1.jpg" },
                    Tags = new List<string> { "Cotton", "Men" },
                    StockQuantity = 100,
                    SellerId = "seller2",
                    IsNew = true,
                    Rating = 4.2,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                }
            };

            await products.InsertManyAsync(seedProducts);
        }
    }
}
