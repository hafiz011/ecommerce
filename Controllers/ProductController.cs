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

            var result = new ProductDetailsDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Attributes = product.Attributes,
                Variants = product.Variants.Select(v => new ProductVariantDto
                {
                    VariantId = v.VariantId,
                    Color = v.Color,
                    Size = v.Size,
                    SKU = v.SKU,
                    Price = v.Price,
                    Stock = v.Stock,
                    Images = v.Images
                }).ToList(),
                Tags = product.Tags,
                IsNew = product.IsNew,
                Discounts = product.Discounts.Select(d => new DiscountDto
                {
                    Id = d.Id,
                    Code = d.Code,
                    Percentage = d.Percentage,
                    ValidFrom = d.ValidFrom,
                    ValidTo = d.ValidTo,
                    IsActive = d.IsActive
                }).ToList(),
                RestockDate = product.RestockDate,
                CategoryId = product.CategoryId,
                CategoryName = product.CategoryName,
                Sold = product.Sold,
                Reviews = product.Review.Select(r => new ReviewDto
                {
                    Id = r.Id,
                    UserName = r.UserName,
                    Rating = r.Rating,
                    Comment = r.Comment,
                    Images = r.Images,
                    CreatedAt = r.CreatedAt
                }).ToList()
            };

            return Ok(result);
        }

        [Authorize(Roles = "Seller")]
        [HttpGet("{Id}/seller")]
        public async Task<IActionResult> GetSellerProductById(string Id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId)) return Unauthorized();
            var product = await _productRepository.GetByIdAsync(Id);
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
        public async Task<IActionResult> Create([FromBody] SellerProductDto dto)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrEmpty(userId)) return Unauthorized("Unauthorized access.");
                if (dto == null) return BadRequest("Invalid product data.");
                if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Product name is required.");

                // save variants images and get urls
                var variants = new List<ProductVariant>();

                foreach (var v in dto.Variants)
                {
                    var savedImages = await SaveBase64Images(v.Images);

                    variants.Add(new ProductVariant
                    {
                        VariantId = Guid.NewGuid().ToString(),
                        Color = v.Color,
                        Size = v.Size,
                        SKU = v.SKU,
                        Price = v.Price,
                        Stock = v.Stock,
                        Images = savedImages
                    });
                }

                // Product-level images = merge variant images
                var basePrice = variants.Any() ? variants.Min(x => x.Price) : 0;
               
                // Map DTO → ProductModel
                var product = new ProductModel
                {
                    Id = ObjectId.GenerateNewId().ToString(),
                    SellerId = userId,
                    Name = dto.Name.Trim(),
                    Description = dto.Description?.Trim(),
                    BasePrice = basePrice,
                    Attributes = dto.Attributes ?? new Dictionary<string, List<string>>(),
                    Variants = variants,
                    Tags = dto.Tags?.Where(t => !string.IsNullOrWhiteSpace(t)).Select(t => t.Trim()).ToList() ?? new List<string>(),
                    Discounts = dto.Discounts?.Select(d => new Discount
                    {
                        Id = Guid.NewGuid().ToString(),
                        Code = d.Code,
                        Percentage = d.Percentage,
                        ValidFrom = d.ValidFrom,
                        ValidTo = d.ValidTo,
                        IsActive = d.IsActive && d.ValidTo >= DateTime.UtcNow.Date
                    }).ToList() ?? new List<Discount>(),
                    IsNew = dto.IsNew,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    RestockDate = dto.RestockDate ?? DateTime.UtcNow,
                    CategoryId = dto.CategoryId,
                    CategoryName = dto.CategoryName,
                    Sold = 0
                };

                await _productRepository.AddAsync(product);
                return Ok(new { message = "Product created successfully", productId = product.Id });
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
                if (string.IsNullOrEmpty(userId)) return Unauthorized();
                if (dto == null) return BadRequest("Invalid product data.");
                if (string.IsNullOrWhiteSpace(dto.Name)) return BadRequest("Product name required.");

                var existingProduct = await _productRepository.GetByIdAsync(id);
                if (existingProduct == null) return NotFound("Product not found.");

                string uploadPath = Path.Combine(_env.WebRootPath, "images", "products");
                Directory.CreateDirectory(uploadPath);

                var updatedVariants = new List<ProductVariant>();

                foreach (var dtoVariant in dto.Variants)
                {
                    ProductVariant oldVariant = existingProduct.Variants
                        .FirstOrDefault(x =>
                            x.Color == dtoVariant.Color &&
                            x.Size == dtoVariant.Size &&
                            x.SKU == dtoVariant.SKU
                        );

                    List<string> oldImages = oldVariant?.Images ?? new List<string>();
                    List<string> updatedImages = new();

                    foreach (var img in dtoVariant.Images)
                    {
                        if (img.StartsWith("http"))
                        {
                            updatedImages.Add(img);
                        }
                        else
                        {
                            var base64Data = img.Contains(",") ? img.Split(',')[1] : img;
                            var bytes = Convert.FromBase64String(base64Data);

                            var fileName = $"{Guid.NewGuid()}.webp";
                            var filePath = Path.Combine(uploadPath, fileName);

                            await System.IO.File.WriteAllBytesAsync(filePath, bytes);

                            updatedImages.Add($"{Request.Scheme}://{Request.Host}/images/products/{fileName}");
                        }
                    }

                    foreach (var oldUrl in oldImages)
                    {
                        if (!updatedImages.Contains(oldUrl))
                        {
                            var oldFile = Path.Combine(uploadPath, Path.GetFileName(oldUrl));
                            if (System.IO.File.Exists(oldFile))
                                System.IO.File.Delete(oldFile);
                        }
                    }

                    updatedVariants.Add(new ProductVariant
                    {
                        VariantId = oldVariant?.VariantId ?? Guid.NewGuid().ToString(),
                        Color = dtoVariant.Color,
                        Size = dtoVariant.Size,
                        SKU = dtoVariant.SKU,
                        Price = dtoVariant.Price,
                        Stock = dtoVariant.Stock,
                        Images = updatedImages
                    });
                }

                // removed variant delete (delete their images)
                foreach (var oldVariant in existingProduct.Variants)
                {
                    if (!updatedVariants.Any(x => x.VariantId == oldVariant.VariantId))
                    {
                        foreach (var img in oldVariant.Images)
                        {
                            var oldFile = Path.Combine(uploadPath, Path.GetFileName(img));
                            if (System.IO.File.Exists(oldFile))
                                System.IO.File.Delete(oldFile);
                        }
                    }
                }

                // assign updated variants
                existingProduct.Variants = updatedVariants;
                existingProduct.BasePrice = updatedVariants.Min(v => v.Price);

                // other fields
                existingProduct.Name = dto.Name.Trim();
                existingProduct.Description = dto.Description?.Trim();
                existingProduct.CategoryId = dto.CategoryId;
                existingProduct.CategoryName = dto.CategoryName;
                existingProduct.Attributes = dto.Attributes;
                existingProduct.Tags = dto.Tags;
                existingProduct.IsNew = dto.IsNew;
                existingProduct.RestockDate = dto.RestockDate ?? existingProduct.RestockDate;
                existingProduct.UpdatedAt = DateTime.UtcNow;

                existingProduct.Discounts = dto.Discounts.Select(d => new Discount
                {
                    Id = Guid.NewGuid().ToString(),
                    Code = d.Code,
                    Percentage = d.Percentage,
                    ValidFrom = d.ValidFrom,
                    ValidTo = d.ValidTo,
                    IsActive = d.IsActive
                }).ToList();

                var success = await _productRepository.UpdateProductAsync(existingProduct);
                if (!success) return StatusCode(500, "Failed to update product.");

                return NoContent();
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.Message);
            }
        }

        // helper method to save base64 images
        private async Task<List<string>> SaveBase64Images(List<string> images)
        {
            var result = new List<string>();
            if (images == null || !images.Any()) return result;

            var uploadPath = Path.Combine(_env.WebRootPath, "images", "products");
            Directory.CreateDirectory(uploadPath);

            foreach (var base64Image in images)
            {
                var base64Data = base64Image.Contains(",") ? base64Image.Split(',')[1] : base64Image;

                var bytes = Convert.FromBase64String(base64Data);

                var fileName = $"{Guid.NewGuid()}.webp";
                var filePath = Path.Combine(uploadPath, fileName);

                await System.IO.File.WriteAllBytesAsync(filePath, bytes);

                result.Add($"{Request.Scheme}://{Request.Host}/images/products/{fileName}");
            }

            return result;
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
