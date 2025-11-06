using ecommerce.Services.Interface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ecommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SellerController : ControllerBase
    {
        private readonly IWebHostEnvironment _env;
        private readonly ICategoryRepository _categoryRepository;
        private readonly IProductRepository _productRepository;
       

        public SellerController(IWebHostEnvironment env, 
            ICategoryRepository categoryRepository,
            IProductRepository productRepository)
        {
            _env = env;
            _categoryRepository = categoryRepository;
            _productRepository = productRepository;
        }

        //[HttpGet("products")]
        //public async Task<IActionResult> GetMyProductsAsync()
        //{
        //    var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (string.IsNullOrEmpty(userId))
        //    {
        //        return Unauthorized();
        //    }
        //    var products = await _productRepository.GetProductBySellerIdAsync(userId);
        //    return Ok(products);
        //}


    }
}
