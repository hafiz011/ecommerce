using ecommerce.Models;
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

        public CategoryController(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
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
        public async Task<IActionResult> AddCategory([FromForm] ProductCategoryModel category)
        {
            if (category == null)
                return BadRequest("Category data is required.");

            category.Id = Guid.NewGuid().ToString();

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            if (category.ImgUrl != null)
            {
                var extension = Path.GetExtension(category.ImgUrl.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest("Only JPG, JPEG, PNG, and WEBP formats are allowed.");

                if (category.ImgUrl.Length > 524288) // 512 KB limit
                    return BadRequest("File size must be less than 512 KB.");

                // Ensure folder exists
                var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "category");
                if (!Directory.Exists(imagesPath))
                    Directory.CreateDirectory(imagesPath);

                // Generate unique file name
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(imagesPath, fileName);

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await category.ImgUrl.CopyToAsync(stream);
                    }

                    // Store URL in category
                    category.ImgUrlPath = $"{Request.Scheme}://{Request.Host}/images/category/{fileName}";
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new
                    {
                        Message = "An error occurred while uploading the image.",
                        Error = ex.Message
                    });
                }
            }
            else
            {
                return BadRequest("Image is required.");
            }

            category.CreatedAt = DateTime.UtcNow;
            await _categoryRepository.AddAsync(category);
            category.ImgUrl = null; // Prevent serialization issues

            var responseCategory = new
            {
                category.Id,
                category.Name,
                category.ImgUrlPath,
                category.Description,
                category.CreatedAt
            };

            return CreatedAtAction(nameof(GetById), new { id = category.Id }, responseCategory);
        }

        // Delete category by ID
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var existingCategory = await _categoryRepository.FindByIdAsync(id);
            if (existingCategory == null)
            {
                return NotFound($"Category with ID {id} not found.");
            }

            await _categoryRepository.DeleteAsync(id);
            return NoContent(); // 204 No Content response
        }


        // Update category by ID
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromForm] ProductCategoryModel updatedCategory)
        {
            if (updatedCategory == null)
            {
                return BadRequest("Updated category data is required.");
            }

            var existingCategory = await _categoryRepository.FindByIdAsync(id);
            if (existingCategory == null)
            {
                return NotFound($"Category with ID {id} not found.");
            }

            updatedCategory.Id = id;
            updatedCategory.CreatedAt = existingCategory.CreatedAt; // Preserve original creation date

            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };

            bool hasNewImage = updatedCategory.ImgUrl != null;
            if (hasNewImage)
            {
                var extension = Path.GetExtension(updatedCategory.ImgUrl.FileName).ToLowerInvariant();
                if (!allowedExtensions.Contains(extension))
                    return BadRequest("Only JPG, JPEG, PNG, and WEBP formats are allowed.");

                if (updatedCategory.ImgUrl.Length > 524288) // 512 KB limit
                    return BadRequest("File size must be less than 512 KB.");

                // Ensure folder exists
                var imagesPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "category");
                if (!Directory.Exists(imagesPath))
                    Directory.CreateDirectory(imagesPath);

                // Generate unique file name
                var fileName = $"{Guid.NewGuid()}{extension}";
                var filePath = Path.Combine(imagesPath, fileName);

                try
                {
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await updatedCategory.ImgUrl.CopyToAsync(stream);
                    }

                    // Store URL in category
                    updatedCategory.ImgUrlPath = $"{Request.Scheme}://{Request.Host}/images/category/{fileName}";
                }
                catch (Exception ex)
                {
                    return StatusCode(500, new
                    {
                        Message = "An error occurred while uploading the image.",
                        Error = ex.Message
                    });
                }
            }
            else
            {
                // Preserve existing image path if no new image
                updatedCategory.ImgUrlPath = existingCategory.ImgUrlPath;
            }

            await _categoryRepository.UpdateAsync(id, updatedCategory);
            updatedCategory.ImgUrl = null; // Prevent serialization issues

            var responseCategory = new
            {
                updatedCategory.Id,
                updatedCategory.Name,
                updatedCategory.ImgUrlPath,
                updatedCategory.Description,
                updatedCategory.CreatedAt
            };

            return Ok(responseCategory);
        }


        //// Update category by ID
        //[HttpPut("{id}")]
        //public async Task<IActionResult> Update(string id, ProductCategoryModel updatedCategory)
        //{
        //    if (updatedCategory == null)
        //    {
        //        return BadRequest("Updated category data is required.");
        //    }

        //    var existingCategory = await _categoryRepository.FindByIdAsync(id);
        //    if (existingCategory == null)
        //    {
        //        return NotFound($"Category with ID {id} not found.");
        //    }

        //    updatedCategory.Id = id;
        //    await _categoryRepository.UpdateAsync(id, updatedCategory);
        //    return Ok(updatedCategory);
        //}
    }
}