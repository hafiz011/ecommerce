using ecommerce.Models;
using ecommerce.Models.Dtos;
using ecommerce.Services.Interface;
using Microsoft.AspNetCore.Authorization;
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


        // need configerd to get all products using seller id
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

        // need configerd to get product by filter and paged using seller id
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


        // need configerd to get product by id and seller id
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







        // seller all their products
        [Authorize(Roles = "Seller")]
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

        // product create
        [Authorize(Roles = "Seller")]
        [HttpPost("create")]
        public async Task<IActionResult> Create([FromBody] SellerProductDto product)
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
                            IsActive = d.IsActive  && d.ValidTo >= DateTime.UtcNow.Date
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



        // PUT: api/Product/update/{id}
        [Authorize(Roles = "Seller")]
        [HttpPut("update/{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] SellerProductDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized("Unauthorized access.");
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return Unauthorized("Invalid seller account.");
                if (string.IsNullOrEmpty(id))
                    return BadRequest("Invalid product ID.");
                if (dto == null)
                    return BadRequest("Invalid product data.");
                if (string.IsNullOrWhiteSpace(dto.Name))
                    return BadRequest("Product name is required.");
                if (dto.Price <= 0)
                    return BadRequest("Product price must be greater than zero.");

                var existingProduct = await _productRepository.GetByIdAsync(id);
                if (existingProduct == null)
                    return NotFound("Product not found.");

                // Update basic fields
                existingProduct.Name = dto.Name.Trim();
                existingProduct.Description = dto.Description?.Trim();
                existingProduct.CategoryId = dto.CategoryId;
                existingProduct.Price = dto.Price;
                existingProduct.StockQuantity = dto.StockQuantity;
                existingProduct.IsNew = dto.IsNew;
                existingProduct.Attributes = dto.Attributes ?? new();
                existingProduct.Tags = dto.Tags?.Select(t => t.Trim()).Where(t => !string.IsNullOrWhiteSpace(t)).ToList() ?? new();
                existingProduct.RestockDate = dto.RestockDate;
                existingProduct.UpdatedAt = DateTime.UtcNow;

                // Handle images: Process new base64, preserve URLs, delete removed files
                var uploadPath = Path.Combine(_env.WebRootPath, "images", "products");
                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var oldImageUrls = existingProduct.Images ?? new List<string>();
                var newImageUrls = new List<string>();

                if (dto.Images != null && dto.Images.Any())
                {
                    foreach (var img in dto.Images)
                    {
                        if (string.IsNullOrWhiteSpace(img))
                            continue;

                        if (img.StartsWith("http")) // Existing URL, preserve
                        {
                            newImageUrls.Add(img);
                        }
                        else // Base64, process and save
                        {
                            var base64Data = img.Contains(",")
                                ? img.Split(',')[1]
                                : img;
                            var bytes = Convert.FromBase64String(base64Data);
                            var fileName = $"{Guid.NewGuid()}.webp";
                            var filePath = Path.Combine(uploadPath, fileName);
                            await System.IO.File.WriteAllBytesAsync(filePath, bytes);
                            var relativeUrl = $"{Request.Scheme}://{Request.Host}/images/products/{fileName}";
                            newImageUrls.Add(relativeUrl);
                        }
                    }
                }

                // Delete files for removed images
                foreach (var oldUrl in oldImageUrls)
                {
                    if (!newImageUrls.Contains(oldUrl))
                    {
                        var fileName = Path.GetFileName(oldUrl);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            var oldFilePath = Path.Combine(uploadPath, fileName);
                            if (System.IO.File.Exists(oldFilePath))
                                System.IO.File.Delete(oldFilePath);
                        }
                    }
                }

                existingProduct.Images = newImageUrls;

                // Handle discounts: Replace list, validate duplicates and dates
                existingProduct.Discounts.Clear();
                if (dto.Discounts?.Any(d => d != null) == true)
                {
                    var duplicateCodes = dto.Discounts
                        .Where(d => d != null)
                        .GroupBy(d => d.Code)
                        .Where(g => g.Count() > 1)
                        .Select(g => g.Key)
                        .ToList();
                    if (duplicateCodes.Any())
                        return BadRequest($"Duplicate discount codes found: {string.Join(", ", duplicateCodes)}");

                    foreach (var d in dto.Discounts.Where(d => d != null))
                    {
                        if (d.ValidTo <= d.ValidFrom)
                            return BadRequest($"Invalid discount period for code '{d.Code}'.");
                        existingProduct.Discounts.Add(new Models.Discount
                        {
                            Id = Guid.NewGuid().ToString(),
                            Code = d.Code,
                            Percentage = d.Percentage,
                            ValidFrom = d.ValidFrom,
                            ValidTo = d.ValidTo,
                            ProductId = id,
                            IsActive = d.IsActive && d.ValidTo >= DateTime.UtcNow.Date
                        });
                    }
                }

                // Keep existing values
                existingProduct.SellerId = existingProduct.SellerId; // Unchanged
                existingProduct.CreatedAt = existingProduct.CreatedAt; // Unchanged
                existingProduct.Sold = existingProduct.Sold; // Unchanged

                await _productRepository.UpdateAsync(id, existingProduct);
                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An internal error occurred: {ex.Message}");
            }
        }



        // DELETE: api/Product/{id}
        [Authorize(Roles = "Seller")]
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
