using ecommerce.Models;
using ecommerce.Models.Dtos;
using ecommerce.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ecommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoryController : ControllerBase
    {
        private readonly ICategoryRepository _categoryRepository;
        private readonly IWebHostEnvironment _env;


        public CategoryController(ICategoryRepository categoryRepository, IWebHostEnvironment env)
        {
            _categoryRepository = categoryRepository;
            _env = env;
        }

        // Get all categories
        [HttpGet("allcategories")]
        public async Task<IActionResult> AllCategory()
        {
            var all = await _categoryRepository.GetAllAsync();
            if (all == null || all.Count == 0)
            {
                return NotFound("No categories found.");
            }
            return Ok(all);
        }

        // Get category by ID
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var data = await _categoryRepository.FindByIdAsync(id);
            if (data == null)
            {
                return NotFound($"Category with ID {id} not found.");
            }
            return Ok(data);
        }



        // Add a new category
        [HttpPost]
        public async Task<IActionResult> AddCategory([FromBody] CategoryDto category)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (category.ImageData == null || category.ImageData.Length == 0)
                return BadRequest("Image is required.");

            // Validate file size
            if (category.ImageData.Length > 524_288)
                return BadRequest("File size must be less than 512 KB.");

            // Validate image type via simple signature or extension (optional)
            var fileName = $"{Guid.NewGuid()}.webp"; // Force a valid extension
            var imagesPath = Path.Combine(_env.WebRootPath, "images", "category");

            try
            {
                if (!Directory.Exists(imagesPath))
                    Directory.CreateDirectory(imagesPath);

                var filePath = Path.Combine(imagesPath, fileName);
                await System.IO.File.WriteAllBytesAsync(filePath, category.ImageData);

                var imageUrl = $"{Request.Scheme}://{Request.Host}/images/category/{fileName}";

                var modelCategory = new ProductCategoryModel
                {
                    Name = category.Name,
                    Description = category.Description,
                    ImagePath = imageUrl,
                    CreatedAt = DateTime.UtcNow
                };

                await _categoryRepository.AddAsync(modelCategory);

                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "Image upload failed", Error = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] CategoryDto updatedCategory)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingCategory = await _categoryRepository.FindByIdAsync(id);
            if (existingCategory == null)
                return NotFound($"Category with ID {id} not found.");

            existingCategory.Name = updatedCategory.Name;
            existingCategory.Description = updatedCategory.Description;

            if (updatedCategory.ImageData != null && updatedCategory.ImageData.Length > 0)
            {
                if (updatedCategory.ImageData.Length > 524_288)
                    return BadRequest("File size must be less than 512 KB.");

                var imagesPath = Path.Combine(_env.WebRootPath, "images", "category");
                if (!Directory.Exists(imagesPath))
                    Directory.CreateDirectory(imagesPath);

                var newFileName = $"{Guid.NewGuid()}.webp";
                var newFilePath = Path.Combine(imagesPath, newFileName);

                try
                {
                    await System.IO.File.WriteAllBytesAsync(newFilePath, updatedCategory.ImageData);

                    // Remove the old image if exists
                    if (!string.IsNullOrWhiteSpace(existingCategory.ImagePath))
                    {
                        var oldFileName = Path.GetFileName(existingCategory.ImagePath);
                        var oldFilePath = Path.Combine(imagesPath, oldFileName);

                        if (System.IO.File.Exists(oldFilePath))
                            System.IO.File.Delete(oldFilePath);
                    }

                    existingCategory.ImagePath = $"{Request.Scheme}://{Request.Host}/images/category/{newFileName}";
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new { Message = "Image upload failed", Error = ex.Message });
                }
            }

            await _categoryRepository.UpdateAsync(id, existingCategory);
            return Ok();
        }




        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existingCategory = await _categoryRepository.FindByIdAsync(id);
            if (existingCategory == null)
            {
                return NotFound($"Category with ID {id} not found.");
            }

            // Delete image from server
            if (!string.IsNullOrEmpty(existingCategory.ImagePath))
            {
                var imagesPath = Path.Combine(_env.WebRootPath, "images", "category");
                var oldFileName = Path.GetFileName(existingCategory.ImagePath);
                var oldFilePath = Path.Combine(imagesPath, oldFileName);

                if (System.IO.File.Exists(oldFilePath))
                    System.IO.File.Delete(oldFilePath);
            }

            await _categoryRepository.DeleteAsync(id);

            return NoContent();
        }



    }
}