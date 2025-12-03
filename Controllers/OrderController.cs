using ecommerce.Models;
using ecommerce.Models.Dtos;
using ecommerce.Services.Interface;
using ecommerce.Services.Repository;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
using MongoDB.Driver.Core.Servers;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace ecommerce.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrderController : ControllerBase
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IShoppingCartRepository _shoppingCartRepository;
        private readonly IProductRepository _productRepository;

        public OrderController(IOrderRepository orderRepository, 
            IShoppingCartRepository shoppingCartRepository,
            IProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _shoppingCartRepository = shoppingCartRepository;
            _productRepository = productRepository;
        }

        public class CreateOrderRequest
        {
            public string? ProductId { get; set; }
            public string? VariantId { get; set; }
            [Required]
            public string PaymentMethod { get; set; }
            [Required]
            public AddShippingAddress AddShippingAddress { get; set; }
        }
        public class AddShippingAddress
        {
            public string FullName { get; set; }
            public string Phone { get; set; }
            public string Email { get; set; }
            public string Address { get; set; }
            public string City { get; set; }
            public string? Country { get; set; } 
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            if (request == null)
                return BadRequest("Invalid order request.");

            // Get current user ID (from claims)
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized("User not authenticated.");

            List<CartItem> cartItems = new();

            // CASE 1: buy now — single product checkout
            if (!string.IsNullOrEmpty(request.ProductId))
            {
                // verify from database if product exists and price matches
                var product = await _productRepository.GetByIdAsync(request.ProductId);
                if (product == null)
                    return NotFound("Product not found.");

                var  selectedVariant = product.Variants.FirstOrDefault(v => v.VariantId == request.VariantId);
                if (selectedVariant == null)
                    return BadRequest("Invalid variant selected.");

                var now = DateTime.UtcNow;
                var activeDiscount = product.Discounts?
                    .FirstOrDefault(d => d.IsActive && d.ValidFrom <= now && d.ValidTo >= now);

                var FinalPrice = selectedVariant.Price - ((activeDiscount?.Percentage ?? 0) * selectedVariant.Price / 100);
                FinalPrice = Math.Floor(FinalPrice) + ((FinalPrice % 1) >= 0.5m ? 1 : 0);

                cartItems.Add(new CartItem
                {
                    ProductId = product.Id,
                    VariantId = request.VariantId,
                    ProductName = product.Name,
                    Color = selectedVariant.Color,
                    Size = selectedVariant.Size,
                    SKU = selectedVariant.SKU,
                    Quantity = 1,
                    Price = FinalPrice,
                    Image = selectedVariant.Images.FirstOrDefault() ?? string.Empty,
                    SellerId = product.SellerId,
                    //Remark = request.Remark
                    //SelectedAttributes = new Dictionary<string, string>()
                });
            }
            else
            {
                // CASE 2: full cart checkout
                var cart = await _shoppingCartRepository.GetCartByUserIdAsync(userId);
                if (cart == null || cart.Items.Count == 0)
                    return BadRequest("Shopping cart is empty.");

                cartItems = cart.Items;
            }

            // Group by Seller (Multi-Seller Split)
            var groupedBySeller = cartItems.GroupBy(i => i.SellerId);
            var createdOrders = new List<OrderModel>();

            foreach (var group in groupedBySeller)
            {
                decimal subTotal = group.Sum(i => i.Price * i.Quantity);
                decimal shippingCost = request.AddShippingAddress?.City?.Trim().ToLower() == "dhaka" ? 70 : 130;
                decimal total = subTotal + shippingCost;

                var order = new OrderModel
                {
                    UserId = userId,
                    SellerId = group.Key,
                    Items = group.Select(i => new Models.OrderItem
                    {
                        ProductId = i.ProductId,
                        VariantId = i.VariantId,
                        ProductName = i.ProductName,
                        Color = i.Color,
                        Size = i.Size,
                        SKU = i.SKU,
                        Price = i.Price,
                        Quantity = i.Quantity,
                        Image = i.Image,
                        SellerId = i.SellerId,
                        SelectedAttributes = i.SelectedAttributes,
                        Remark = i.Remark,
                    }).ToList(),
                    SubTotal = subTotal,
                    ShippingCost = shippingCost,
                    TotalAmount = total,
                    Payment = new PaymentModel
                    {
                        Method = request.PaymentMethod,
                        Status = "Pending",
                        PaidAt = null
                    },
                    OrderStatus = "Processing",
                    StatusTimeline = new List<StatusTimeline>
                    {
                        new StatusTimeline
                        {
                            Status = "Processing",
                            Message = "Order has been placed.",
                            UpadateAt = DateTime.UtcNow
                        }
                    },
                    ShippingAddress = new Models.ShippingAddress
                    {
                        FullName = request.AddShippingAddress?.FullName ?? "",
                        Phone = request.AddShippingAddress?.Phone ?? "",
                        Email = request.AddShippingAddress?.Email ?? "",
                        Address = request.AddShippingAddress?.Address ?? "",
                        City = request.AddShippingAddress?.City ?? "",
                        Country = request.AddShippingAddress?.Country ?? "Bangladesh",
                    },
                    CreatedAt = DateTime.UtcNow,
                };
                await _orderRepository.CreateOrderAsync(order);
                createdOrders.Add(order);
            }

            // Remove ordered items from cart only if it's a cart checkout
            if (string.IsNullOrEmpty(request.ProductId))
            {
                var cart = await _shoppingCartRepository.GetCartByUserIdAsync(userId);
                if (cart != null)
                {
                    // Instead of clearing everything blindly,
                    // remove only the items for the sellers in this checkout
                    var sellerIds = groupedBySeller.Select(g => g.Key).ToList();
                    cart.Items.RemoveAll(item => sellerIds.Contains(item.SellerId));

                    // Recalculate total
                    cart.TotalAmount = cart.Items.Sum(i => i.Price * i.Quantity);
                    cart.UpdatedAt = DateTime.UtcNow;

                    await _shoppingCartRepository.UpsertCartAsync(cart);
                }
            }

            // Payment Logic
            if (request.PaymentMethod.ToLower() != "cod")
            {
                var paymentUrl =
                    $"https://paymentgateway.com/pay?orderId={createdOrders.First().Id}";

                return Ok(new
                {
                    message = "Redirect to payment",
                    paymentUrl,
                    orders = createdOrders
                });
            }

            return Ok(new { message = "Order(s) created successfully (COD)", orders = createdOrders });
        }

        [HttpPost("{orderId}/payment-confirm")]
        public async Task<IActionResult> ConfirmPayment(string orderId, [FromBody] string transactionId)
        {
            //await _orderRepository.UpdatePaymentStatusAsync(orderId, "Paid");
            //await _orderRepository.UpdateOrderStatusAsync(orderId, "Confirmed");

            return Ok(new { message = "Payment confirmed", transactionId });
        }

        [HttpPost("{orderId}/payment")]
        public async Task<IActionResult> UpdatePayment(string orderId, [FromQuery] string paymentStatus, [FromBody] string? transactionId)
        {
            await _orderRepository.UpdatePaymentStatusAsync(orderId, paymentStatus, transactionId);
            return Ok(new { message = "Payment updated" });
        }


        // User gets their own orders with pagination
        [HttpGet("user")]
        [Authorize]
        public async Task<IActionResult> GetUserOrders([FromQuery] int page, [FromQuery] int pageSize)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId)) return Unauthorized();

            var filter = new OrderFilterDto { UserId = userId, Page = page, PageSize = pageSize };
            var (orders, total) = await _orderRepository.GetOrdersAsync(filter);
            return Ok(new { total, orders });
        }


        public class PagedOrderResponse
        {
            public long Total { get; set; }
            public List<OrderDto> Orders { get; set; } = new List<OrderDto>();
        }
        // Seller gets their own orders with pagination
        [Authorize(Roles = "Seller")]
        [HttpGet("seller")]
        public async Task<IActionResult> GetSellerOrders([FromQuery] int page, [FromQuery] int pageSize)
        {
            var sellerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(sellerId)) return Unauthorized();

            var filter = new OrderFilterDto { SellerId = sellerId, Page = page, PageSize = pageSize };
            var (orders, total) = await _orderRepository.GetOrdersAsync(filter);

            // Map from Order model OrderDto
            var orderDtos = orders.Select(o => new OrderDto
            {
                Id = o.Id,
                Items = o.Items.Select(i => new Models.Dtos.OrderItem
                {
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Color = i.Color,
                    Size = i.Size,
                    Price = i.Price,
                    Quantity = i.Quantity,
                    Image = i.Image,
                    Remark = i.Remark
                }).ToList(),
                SubTotal = o.SubTotal,
                ShippingCost = o.ShippingCost,
                TotalAmount = o.TotalAmount,
                PaymentMethod = o.Payment.Method,
                PaymentStatus = o.Payment.Status,
                OrderStatus = o.OrderStatus,
                ShippingAddress = new Models.Dtos.ShippingAddress
                {
                    FullName = o.ShippingAddress.FullName,
                    Phone = o.ShippingAddress.Phone,
                    Email = o.ShippingAddress.Email,
                    Address = o.ShippingAddress.Address,
                    City = o.ShippingAddress.City,
                    Country = o.ShippingAddress.Country
                },
                CreatedAt = o.CreatedAt,
                UpdatedAt = o.UpdatedAt
            }).ToList();


            //return Ok(new { total, orderDtos }); // Return total count along with orders
            return Ok(new PagedOrderResponse
            {
                Total = total,
                Orders = orderDtos
            });

        }

        [Authorize(Roles = "Seller")]
        [HttpPatch("{orderId}/status")]
        public async Task<IActionResult> UpdateStatus(string orderId, [FromQuery] string status)
        {
            await _orderRepository.UpdateOrderStatusAsync(orderId, status);
            return Ok(new { message = "Status updated" });
        }

        [Authorize(Roles = "Seller")]
        [HttpGet("orders/{id}")]
        public async Task<IActionResult> GetOrderById(string id)
        {
            var order = await _orderRepository.GetOrderByIdAsync(id);
            if (order == null)
                return NotFound("Order not found.");
            return Ok(order);
        }

        // dashboard stats for seller
        [Authorize(Roles = "Seller")]
        [HttpGet("dashboard")]
        public async Task<IActionResult> Dashboard([FromQuery] DateTime? from, [FromQuery] DateTime? to)
        {
            var sellerId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(sellerId)) return Unauthorized();

            var stats = await _orderRepository.GetDashboardStatsAsync(sellerId, from, to);
            return Ok(stats);
        }


        // used curiar update
        [HttpPost("{orderId}/status/add")]
        public async Task<IActionResult> AddTimeline(string orderId, [FromBody] StatusTimeline timeline)
        {
            if (timeline == null) return BadRequest();
            timeline.UpadateAt = DateTime.UtcNow;
            await _orderRepository.AddStatusTimelineAsync(orderId, timeline);
            return Ok(new { message = "Timeline added" });
        }













        // admin gets all orders with filtering and pagination
        [Authorize(Roles = "Admin")]
        [HttpGet("admin")]
        public async Task<IActionResult> GetAllOrders([FromQuery] OrderFilterDto filter)
        {
            var (orders, total) = await _orderRepository.GetOrdersAsync(filter);
            return Ok(new { total, orders });
        }


    }

}
