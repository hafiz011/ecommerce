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
        [HttpGet("all")]
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
        public async Task<IActionResult> AddCategory(ProductCategoryModel category)
        {
            category.Id = Guid.NewGuid().ToString();
            if (category == null)
            {
                return BadRequest("Category data is required.");
            }
            
            category.CreatedAt = DateTime.UtcNow;
            await _categoryRepository.AddAsync(category);
            return CreatedAtAction(nameof(GetById), new { id = category.Id }, category);
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
        public async Task<IActionResult> Update(string id, ProductCategoryModel updatedCategory)
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
            await _categoryRepository.UpdateAsync(id, updatedCategory);
            return Ok(updatedCategory);
        }
    }
}
