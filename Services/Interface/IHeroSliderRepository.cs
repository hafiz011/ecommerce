using ecommerce.Models;

namespace ecommerce.Services.Interface
{
    public interface IHeroSliderRepository
    {
        Task<List<HeroSlider>> GetAllSlidersAsync();
        Task<HeroSlider> GetSliderByIdAsync(string id);
        Task<HeroSlider> CreateSliderAsync(HeroSlider slider);
        Task<HeroSlider> UpdateSliderAsync(string id, HeroSlider slider);
        Task<bool> DeleteSliderAsync(string id);
    }
}
