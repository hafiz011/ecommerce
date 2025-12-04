using ecommerce.Models;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace ecommerce.DbContext
{
    public class MongoDbContext
    {
        private readonly IMongoDatabase _database;

        public MongoDbContext(IOptions<MongoDbSettings> settings)
        {
            var client = new MongoClient(settings.Value.ConnectionString);
            _database = client.GetDatabase(settings.Value.DatabaseName);
        }
        public IMongoCollection<ApplicationUser> Users => _database.GetCollection<ApplicationUser>("Users");
        public IMongoCollection<ApplicationRole> Roles => _database.GetCollection<ApplicationRole>("Roles");
        public IMongoCollection<ProductCategoryModel> Categories => _database.GetCollection<ProductCategoryModel>("Categories");
        public IMongoCollection<ProductModel> Products => _database.GetCollection<ProductModel>("Products");
        public IMongoCollection<ShoppingCartModel> ShoppingCart => _database.GetCollection<ShoppingCartModel>("Shopping_Cart");
        public IMongoCollection<OrderModel> Orders => _database.GetCollection<OrderModel>("Orders");
        public IMongoCollection<HeroSlider> HeroSliders => _database.GetCollection<HeroSlider>("HeroSliders");
        //public IMongoCollection<InventoryModel> Inventories => _database.GetCollection<InventoryModel>("Inventories");
        public IMongoCollection<GeolocationModel> UserGeolocation => _database.GetCollection<GeolocationModel>("User_Geolocation");
        public IMongoCollection<ActivityLogsModel> UserLogs => _database.GetCollection<ActivityLogsModel>("User_Logs");

    }

}
