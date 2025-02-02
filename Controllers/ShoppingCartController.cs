using ecommerce.Models;
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
    public class ShoppingCartController : ControllerBase
    {
        private readonly IShoppingCartRepository _shoppingCartRepository;
        private readonly IProductRepository _productRepository;
        private readonly UserManager<ApplicationUser> _userManager;

        public ShoppingCartController(
            IShoppingCartRepository shoppingCartRepository,
            IProductRepository productRepository,
            UserManager<ApplicationUser> userManager)
        {
            _shoppingCartRepository = shoppingCartRepository;
            _productRepository = productRepository;
            _userManager = userManager;
        }

        public class AddToCartRequest
        {
            public string ProductId { get; set; }
            public int Quantity { get; set; }
        }


        [HttpPost("AddToCart")]
        public async Task<IActionResult> AddToCart([FromBody] AddToCartRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.ProductId) || request.Quantity <= 0)
                return BadRequest("Invalid product or quantity.");

            // Retrieve the authenticated user's ID from claims
            //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value; 
            var userId = "01fa0bba-f32b-4327-adc9-893a8ef38d00";
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not authenticated.");

            var product = await _productRepository.GetByIdAsync(request.ProductId);
            if (product == null)
                return NotFound("Product not found.");

            // Get or create the user's shopping cart
            var cart = await _shoppingCartRepository.GetCartByUserIdAsync(userId)
                       ?? new ShoppingCartModel
                       {
                           Id = Guid.NewGuid().ToString(),
                           UserId = userId,
                           CreatedAt = DateTime.UtcNow,
                           UpdatedAt = DateTime.UtcNow
                       };

            // Check if the product is already in the cart
            var existingItem = cart.Items.FirstOrDefault(i => i.ProductId == request.ProductId);
            if (existingItem != null)
            {
                // Update quantity and price
                existingItem.Quantity += request.Quantity;
                existingItem.Price = product.Price * existingItem.Quantity;
            }
            else
            {
                // Add new product to the cart
                cart.Items.Add(new CartItem
                {
                    ProductId = request.ProductId,
                    Quantity = request.Quantity,
                    Price = product.Price * request.Quantity
                });
            }

            // Update cart's total amount and save
            cart.TotalAmount = cart.Items.Sum(i => i.Price);
            cart.UpdatedAt = DateTime.UtcNow;
            await _shoppingCartRepository.UpsertCartAsync(cart);

            return Ok(cart);
        }




        [HttpGet("GetCart")]
        public async Task<IActionResult> GetCart()
        {
            //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = "01fa0bba-f32b-4327-adc9-893a8ef38d00";
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not authenticated.");

            var cart = await _shoppingCartRepository.GetCartByUserIdAsync(userId);
            if (cart == null)
                return NotFound("Cart not found.");

            return Ok(cart);
        }


        /// Removes an item from the shopping cart.
        [HttpDelete("RemoveFromCart/{productId}")]
        public async Task<IActionResult> RemoveFromCart(string productId)
        {
            //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = "01fa0bba-f32b-4327-adc9-893a8ef38d00";

            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not authenticated.");

            var cart = await _shoppingCartRepository.GetCartByUserIdAsync(userId);
            if (cart == null)
                return NotFound("Cart not found.");

            var itemToRemove = cart.Items.FirstOrDefault(i => i.ProductId == productId);
            if (itemToRemove == null)
                return NotFound("Product not found in cart.");

            cart.Items.Remove(itemToRemove);
            cart.TotalAmount = cart.Items.Sum(i => i.Price);
            cart.UpdatedAt = DateTime.UtcNow;

            await _shoppingCartRepository.UpsertCartAsync(cart);
            return Ok(cart);
        }

        /// Clears the shopping cart.
        [HttpDelete("ClearCart")]
        public async Task<IActionResult> ClearCart()
        {
            //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var userId = "01fa0bba-f32b-4327-adc9-893a8ef38d00";
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not authenticated.");

            var cart = await _shoppingCartRepository.GetCartByUserIdAsync(userId);
            if (cart == null)
                return NotFound("Cart not found.");

            cart.Items.Clear();
            cart.TotalAmount = 0;
            cart.UpdatedAt = DateTime.UtcNow;

            await _shoppingCartRepository.UpsertCartAsync(cart);
            return Ok("Cart cleared successfully.");
        }



    }
}
