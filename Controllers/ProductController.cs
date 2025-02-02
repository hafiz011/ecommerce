using ecommerce.Models;
using ecommerce.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;

namespace ecommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICategoryRepository _category;

        public ProductController(IProductRepository productRepository, UserManager<ApplicationUser> userManager, ICategoryRepository categoryRepository)
        {
            _productRepository = productRepository;
            _userManager = userManager;
            _category = categoryRepository;
        }

        // GET: api/Product
        [HttpGet]
        public async Task<IActionResult> GetAllProducts()
        {
            var products = await _productRepository.GetAllAsync();
            return Ok(products);
        }

        // GET: api/Product/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetProductById(string id)
        {
            if (id == null)
                return BadRequest("Invalid product ID.");

            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                return NotFound("Product not found.");

            return Ok(product);
        }

        // POST: api/Product
        [HttpPost]
        public async Task<IActionResult> CreateProduct(ProductModel product)
        {
            product.Id = Guid.NewGuid().ToString();
            if (product == null)
                return BadRequest("Invalid product data.");
            var user = await _userManager.FindByIdAsync(product.SellerId);
            if (user == null)
                return BadRequest("Invalid user");
            var catagory = await _category.FindByIdAsync(product.CategoryId);
            if (catagory == null)
                return BadRequest("Invalid Category");

            if(product.Discounts != null)
            {
                foreach (var discount in product.Discounts)
                {
                    if (string.IsNullOrEmpty(discount.Id) || discount.Id == Guid.Empty.ToString())
                    {
                        discount.Id = Guid.NewGuid().ToString();
                        discount.ProductId = product.Id;
                    }
                }
            }

            product.CreatedAt = DateTime.UtcNow;
            product.UpdatedAt = DateTime.UtcNow;
            product.SellerId = user.Id.ToString();
            product.CategoryId = catagory.Id;

            await _productRepository.AddAsync(product);
            return CreatedAtAction(nameof(GetProductById), new { id = product.Id.ToString() }, product);
        }

        // PUT: api/Product/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateProduct(string id, ProductModel product)
        {
            if (id == null)
                return BadRequest("Invalid product ID.");

            var existingProduct = await _productRepository.GetByIdAsync(id);
            if (existingProduct == null)
                return NotFound("Product not found.");

            existingProduct.Name = product.Name;
            existingProduct.Description = product.Description;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.Price = product.Price;
            existingProduct.Images = product.Images;  //test
            existingProduct.Tags = product.Tags;
            existingProduct.StockQuantity = product.StockQuantity;
            existingProduct.Discounts = product.Discounts;
            existingProduct.SellerId = product.SellerId; //test
            existingProduct.IsNew = product.IsNew;
            existingProduct.Attributes = product.Attributes;
            existingProduct.UpdatedAt = DateTime.UtcNow;

            await _productRepository.UpdateAsync(id, existingProduct);
            return NoContent();
        }

        // DELETE: api/Product/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(string id)
        {
            if (id == null)
                return BadRequest("Invalid product ID.");

            var product = await _productRepository.GetByIdAsync(id);
            if (product == null)
                return NotFound("Product not found.");

            await _productRepository.DeleteAsync(id);
            return NoContent();
        }
    }
}
