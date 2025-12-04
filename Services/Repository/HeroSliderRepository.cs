using ecommerce.DbContext;
using ecommerce.Models;
using ecommerce.Services.Interface;
using MongoDB.Driver;

namespace ecommerce.Services.Repository
{
    public class HeroSliderRepository : IHeroSliderRepository
    {
        public IMongoCollection<HeroSlider> _sliderCollection;

        public HeroSliderRepository(MongoDbContext context)
        {
            _sliderCollection = context.HeroSliders;
        }


        public async Task<HeroSlider> CreateSliderAsync(HeroSlider slider)
        {
            await _sliderCollection.InsertOneAsync(slider);
            return slider;
        }

        public async Task<bool> DeleteSliderAsync(string id)
        {
            var result = await _sliderCollection.DeleteOneAsync(s => s.Id == id);
            return result.DeletedCount > 0;
        }

        public async Task<List<HeroSlider>> GetAllSlidersAsync()
        {
            return await _sliderCollection.Find(x => x.IsActive).SortBy(x => x.Order).ToListAsync();
        }

        public async Task<HeroSlider?> GetSliderByIdAsync(string id)
        {
            return await _sliderCollection.Find(s => s.Id == id).FirstOrDefaultAsync();
        }

        public async Task<HeroSlider> UpdateSliderAsync(string id, HeroSlider slider)
        {
            var result = await _sliderCollection.ReplaceOneAsync(s => s.Id == id, slider);
            return result.ModifiedCount > 0 ? slider : null;
        }

        public async Task<List<HeroSlider>> GetSlidersByUserIdAsync(string userId)
        {
            return await _sliderCollection.Find(s => s.SellerId == userId).ToListAsync();
        }



    }
}
