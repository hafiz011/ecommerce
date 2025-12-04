using ecommerce.Models;
using ecommerce.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ecommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SliderController : ControllerBase
    {
        private readonly IHeroSliderRepository _sliderService;
        private readonly IWebHostEnvironment _env;

        public SliderController(IHeroSliderRepository sliderService, IWebHostEnvironment env)
        {
            _sliderService = sliderService;
            _env = env;
        }

        // GET: api/slider/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var item = await _sliderService.GetSliderByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        // GET: api/slider/sliders
        [HttpGet("sliders")]
        public async Task<IActionResult> GetAllSliders()
        {
            var sliders = await _sliderService.GetAllSlidersAsync();
            return Ok(sliders.OrderBy(x => x.Order));
        }

        // POST: api/slider/create
        [HttpPost("create")]
        public async Task<IActionResult> CreateSlider([FromBody] HeroSlider slider)
        {
            if (slider == null)
                return BadRequest("Slider data missing.");

            var SellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(SellerId)) return Unauthorized();

            // Save image if it's Base64
            if (!string.IsNullOrEmpty(slider.ImageUrl) && slider.ImageUrl.StartsWith("data:"))
            {
                slider.ImageUrl = await SaveBase64Image(slider.ImageUrl);
            }

            slider.SellerId = SellerId;
            var created = await _sliderService.CreateSliderAsync(slider);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // PUT: api/slider/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateSlider(string id, [FromBody] HeroSlider slider)
        {
            if (slider == null)
                return BadRequest("Invalid slider data.");

            var SellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(SellerId)) return Unauthorized();

            var existing = await _sliderService.GetSliderByIdAsync(id);
            if (existing == null) return NotFound();

            // Save image if Base64
            if (!string.IsNullOrEmpty(slider.ImageUrl) && slider.ImageUrl.StartsWith("data:"))
            {
                slider.ImageUrl = await SaveBase64Image(slider.ImageUrl);

                // Optionally delete old image
                if (!string.IsNullOrEmpty(existing.ImageUrl))
                    DeleteImage(existing.ImageUrl);
            }
            else
            {
                slider.ImageUrl = existing.ImageUrl; // keep old image if not changed
            }

            var updated = await _sliderService.UpdateSliderAsync(id, slider);
            if (updated == null)
                return StatusCode(500, "Failed to update slider.");

            return Ok(updated);
        }

        // DELETE: api/slider/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteSlider(string id)
        {

            var SellerId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(SellerId)) return Unauthorized();

            var slider = await _sliderService.GetSliderByIdAsync(id);
            if (slider == null) return NotFound();

            var success = await _sliderService.DeleteSliderAsync(id);
            if (!success) return StatusCode(500, "Failed to delete slider.");

            // Delete image from server
            if (!string.IsNullOrEmpty(slider.ImageUrl))
                DeleteImage(slider.ImageUrl);

            return NoContent();
        }

        // Helper: Save Base64 image to wwwroot/images/slider
        private async Task<string> SaveBase64Image(string base64Image)
        {
            if (string.IsNullOrWhiteSpace(base64Image))
                throw new ArgumentException("Image data is null or empty");

            var uploadPath = Path.Combine(_env.WebRootPath ?? "", "images", "slider");
            Directory.CreateDirectory(uploadPath);

            var base64Data = base64Image.Contains(",") ? base64Image.Split(',')[1] : base64Image;

            byte[] bytes;
            try
            {
                bytes = Convert.FromBase64String(base64Data);
            }
            catch
            {
                throw new InvalidOperationException("Invalid Base64 string.");
            }

            var fileName = $"{Guid.NewGuid()}.webp";
            var filePath = Path.Combine(uploadPath, fileName);

            await System.IO.File.WriteAllBytesAsync(filePath, bytes);

            return $"{Request.Scheme}://{Request.Host}/images/slider/{fileName}";
        }

        // Helper: Delete image file from server
        private void DeleteImage(string imageUrl)
        {
            try
            {
                var uri = new Uri(imageUrl);
                var relativePath = uri.LocalPath.TrimStart('/');
                var fullPath = Path.Combine(_env.WebRootPath ?? "", relativePath.Replace("/", Path.DirectorySeparatorChar.ToString()));
                if (System.IO.File.Exists(fullPath))
                    System.IO.File.Delete(fullPath);
            }
            catch
            {
                // ignore errors
            }
        }
    }
}
