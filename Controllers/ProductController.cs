using ecommerce.Models;
using ecommerce.Models.Dtos;
using ecommerce.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using System.Security.Claims;

namespace ecommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductController : ControllerBase
    {
        private readonly IProductRepository _productRepository;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICategoryRepository _category;
        private readonly IWebHostEnvironment _env;


        public ProductController(IProductRepository productRepository, 
            UserManager<ApplicationUser> userManager, 
            ICategoryRepository categoryRepository,
            IWebHostEnvironment env)
        {
            _productRepository = productRepository;
            _userManager = userManager;
            _category = categoryRepository;
            _env = env;
        }



        // view page by page
        [HttpGet("paged")]
        public async Task<IActionResult> GetPaged(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 16)
        {
            var (items, total) = await _productRepository.GetPagedAsync(page, pageSize);

            return Ok(new PagedResult<ProductDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = total
            });
        }


        [HttpGet("filterpaged")]
        public async Task<IActionResult> GetFilteredPaged(
             [FromQuery] string? category,
             [FromQuery] decimal? minPrice,
             [FromQuery] decimal? maxPrice,
             [FromQuery] string? q,
             [FromQuery] int page = 1,
             [FromQuery] int pageSize = 16)
        {
            var filter = new ProductFilter
            {
                CategoryId = category,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                //Tags = tags ?? new List<string>(),
                //IsNew = isNew,
                Search = q
            };

            var (items, total) = await _productRepository.GetFilteredPagedAsync(filter, page, pageSize);

            return Ok(new PagedResult<ProductDto>
            {
                Items = items,
                Page = page,
                PageSize = pageSize,
                TotalItems = total
            });
        }


        // GET: api/Product
        //[HttpGet("all")]
        //public async Task<IActionResult> GetAllProducts()
        //{
        //    //var products = await _productRepository.GetAllAsync();
        //    //return Ok(products);
        //}









        // seller all their products
        [HttpGet("all")]
        public async Task<IActionResult> GetMyProductsAsync()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized();
            }
            var products = await _productRepository.GetProductBySellerIdAsync(userId);
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



        // product create
        [HttpPost("create")]
        public async Task<IActionResult> CreateProduct([FromBody] SellerProductDto product)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Unauthorized access.");

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Unauthorized("Invalid seller account.");

                if (product == null)
                    return BadRequest("Invalid product data.");

                if (string.IsNullOrWhiteSpace(product.Name))
                    return BadRequest("Product name is required.");

                if (product.Price <= 0)
                    return BadRequest("Product price must be greater than zero.");

                // Handle base64 images
                var imageUrls = new List<string>();
                var uploadPath = Path.Combine(_env.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                if (product.Images != null && product.Images.Any())
                {
                    foreach (var base64Image in product.Images)
                    {
                        var base64Data = base64Image.Contains(",")
                            ? base64Image.Split(',')[1]
                            : base64Image;

                        var bytes = Convert.FromBase64String(base64Data);
                        var fileName = $"{Guid.NewGuid()}.webp";
                        var filePath = Path.Combine(uploadPath, fileName);
                        await System.IO.File.WriteAllBytesAsync(filePath, bytes);

                        var relativeUrl = $"{Request.Scheme}://{Request.Host}/images/products/{fileName}";
                        imageUrls.Add(relativeUrl);
                    }
                }

                var newProduct = new ProductModel
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    SellerId = userId,
                    Name = product.Name.Trim(),
                    Description = product.Description?.Trim(),
                    CategoryId = product.CategoryId,
                    Price = product.Price,
                    Attributes = product.Attributes ?? new(),
                    Images = imageUrls,
                    Tags = product.Tags?.Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? new(),
                    StockQuantity = product.StockQuantity,
                    Sold = 0,
                    Discounts = new(),
                    IsNew = product.IsNew,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    RestockDate = product.RestockDate
                };

                // Handle multiple discounts
                if (product.Discounts?.Any() == true)
                {
                    var duplicateCodes = product.Discounts
                        .GroupBy(d => d.Code)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();

                    if (duplicateCodes.Any())
                        return BadRequest($"Duplicate discount codes found: {string.Join(", ", duplicateCodes)}");

                    foreach (var d in product.Discounts)
                    {
                        if (d.ValidTo <= d.ValidFrom)
                            return BadRequest($"Invalid discount period for code '{d.Code}'.");

                        newProduct.Discounts.Add(new Models.Discount
                        {
                            Id = Guid.NewGuid().ToString(),
                            Code = d.Code,
                            Percentage = d.Percentage,
                            ValidFrom = d.ValidFrom,
                            ValidTo = d.ValidTo,
                            ProductId = newProduct.Id,
                            IsActive = d.IsActive &&
                                       d.ValidFrom <= DateTime.UtcNow &&
                                       d.ValidTo >= DateTime.UtcNow
                        });
                    }
                }

                await _productRepository.AddAsync(newProduct);
                return Ok();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An internal error occurred: {ex.Message}");
            }
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
