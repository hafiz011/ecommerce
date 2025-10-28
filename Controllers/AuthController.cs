﻿using ecommerce.Helpers;
using ecommerce.Models;
using ecommerce.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.JsonWebTokens;
using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Security.Claims;

namespace ecommerce.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly RoleManager<ApplicationRole> _roleManager;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;
        private readonly ILogger<AuthController> _logger;


        public AuthController(UserManager<ApplicationUser> userManager, 
            SignInManager<ApplicationUser> signInManager, 
            IConfiguration configuration, 
            EmailService emailService,
            RoleManager<ApplicationRole> roleManager,
            ILogger<AuthController> logger
            )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _emailService = emailService;
            _roleManager = roleManager;
            _logger = logger;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return BadRequest(ModelState);

                var user = await _userManager.FindByEmailAsync(model.Email);
                if (user == null)
                    return Unauthorized(new { Message = "Invalid email or password" });

                if (!user.EmailConfirmed)
                {
                    return Unauthorized(new { Message = "You need to confirm your email before logging in." });
                }

                if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                    return Unauthorized(new { Message = "Invalid email or password" });

                if (user.LockoutEnd.HasValue && user.LockoutEnd > DateTime.UtcNow)
                    return Unauthorized(new { Message = "Your account is locked. Please try again later." });

                var roles = await _userManager.GetRolesAsync(user);
                var role = roles.Count > 0 ? roles[0] : "User";

                var token = JwtTokenHelper.GenerateToken(user.Id.ToString(), role, _configuration["JwtSettings:Key"], _configuration["JwtSettings:Issuer"], _configuration["JwtSettings:Audience"]);
                _logger.LogInformation($"User {user.Email} successfully logged in.");

                return Ok(new
                {
                    Token = token,
                    Role = role,
                    User = new
                    {
                        user.Id,
                        user.Email,
                        FullName = $"{user.FirstName} {user.LastName}"
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = "An unexpected error occurred. Please try again later." });
            }
        }

        // logout endpoint
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Ok(new { Message = "Logged out successfully." });
        }


        // POST: api/auth/register
        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
                return BadRequest(new { Message = "Email already in use" });

            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.Phone,
            };

            var result = await _userManager.CreateAsync(user, model.Password);
            if (!result.Succeeded)
                return BadRequest(new { Message = "User registration failed", Errors = result.Errors });

            const string defaultRole = "Admin";

            var roleExists = await _roleManager.RoleExistsAsync(defaultRole);
            if (!roleExists)
            {
                var roleResult = await _roleManager.CreateAsync(new ApplicationRole { Name = defaultRole });
                if (!roleResult.Succeeded)
                {
                    return BadRequest(new { Message = "Failed to create default role", Errors = roleResult.Errors });
                }
            }

            await _userManager.AddToRoleAsync(user, defaultRole);

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);

            // URL-encode the token and assign it back to the variable
            var encodedToken = WebUtility.UrlEncode(token);

            //var confirmationLink = Url.Action("ConfirmEmail", "Auth",
            //    new { userId = user.Id, token = encodedToken }, Request.Scheme);
            var confirmationLink = $"http://localhost:5051/confirm-email?userId={Uri.EscapeDataString(user.Id.ToString())}&token={encodedToken}";

            bool emailSent = await _emailService.SendEmailAsync(user.Email, "Confirm your email",
                $"Please confirm your account by clicking this link: <a href='{confirmationLink}'>Confirm Email</a>");

            if (!emailSent)
                return StatusCode(500, new { Message = "User registered, but failed to send confirmation email." });

            return Ok(new { Message = "User registered successfully! Please check your email for confirmation." });
        }

        public class ConfirmEmailModel
        {
            public string UserId { get; set; }
            public string Token { get; set; }
        }


        [HttpPost("confirm-email")]
        public async Task<IActionResult> ConfirmEmail([FromBody] ConfirmEmailModel model)
        {
            if (string.IsNullOrEmpty(model.UserId) || string.IsNullOrEmpty(model.Token))
            {
                return BadRequest(new { Message = "Invalid confirmation request. User ID and Token are required." });
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                return NotFound(new { Message = "User not found." });
            }

            if (user.EmailConfirmed)
            {
                return Ok(new { Message = "Your email is already confirmed. You can log in now." });
            }
            //var decodedToken = WebUtility.UrlDecode(model.Token);
            var result = await _userManager.ConfirmEmailAsync(user, model.Token);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "Email confirmation failed. Invalid or expired token." });
            }

            return Ok(new { Message = "Email confirmed successfully! You can now log in." });
        }


        public class ForgotPasswordModel
        {
            [Required]
            [EmailAddress]
            public string Email { get; set; }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    Message = "Invalid email"
                });
            }

            // Find the user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { Message = "User not found" });

            // Generate the reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var encodedToken = WebUtility.UrlEncode(token);
            // var resetUrl = $"{Request.Scheme}://{Request.Host}/reset-password?token={encodedToken}&email={model.Email}";
            var resetUrl = $"http://localhost:5051/reset-password?email={model.Email}&token={encodedToken}";

            _logger.LogInformation($"Generated password reset token for {model.Email}");
            bool emailSent = await _emailService.SendEmailAsync(user.Email, "Password Reset",
                $"Click here to reset your password: <a href='{resetUrl}'>Reset Password</a>");

            if (!emailSent)
                return StatusCode(500, new { Message = "Failed to send reset password email." });

            return Ok(new { Message = "Password reset link sent to your email." });
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordRequestModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Find the user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { Message = "User not found" });

            // Reset the password using the token
            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (!result.Succeeded)
                return BadRequest(new { Message = "Invalid or expired token." });

            _logger.LogInformation($"Password successfully reset for {model.Email}");
            return Ok(new { Message = "Password reset successfully." });
        }

        public class ChangePasswordModel
        {
            public string currentPassword { get; set; }
            public string newPassword { get; set; }
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordModel model)
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new { Message = "User not authenticated." });

                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return NotFound(new { Message = "User not found." });

                var result = await _userManager.ChangePasswordAsync(user, model.currentPassword, model.newPassword);
                if (!result.Succeeded)
                {
                    return BadRequest(new
                    {
                        Message = "Password change failed.",
                        Errors = result.Errors.Select(e => e.Description)
                    });
                }

                _logger.LogInformation($"Password successfully reset for user {user.Email}");

                return Ok(new { Message = "Password reset successfully." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password");
                return StatusCode(500, new { Message = "Internal server error", Error = ex.Message });
            }
        }

        [HttpGet("account")]
        public async Task<IActionResult> GetAccountDetails()
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            //var userId = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
    

            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            return Ok(new
            {
                user.FirstName,
                user.LastName,
                user.Email,
                user.EmailConfirmed,
                user.PhoneNumber,
                user.PhoneNumberConfirmed,
                user.Address
            });
        }

        public class UpdateAccountModel
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string Phone { get; set; }
            public Address Address { get; set; }
        }

        [HttpPut("account")]
        public async Task<IActionResult> UpdateAccount(UpdateAccountModel model)
        {
            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new { Message = "User not authenticated." });

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
                return NotFound(new { Message = "User not found." });

            user.FirstName = model.FirstName ?? user.FirstName;
            user.LastName = model.LastName ?? user.LastName;
            user.PhoneNumber = model.Phone ?? user.PhoneNumber;
            user.Address = model.Address ?? user.Address;
            var result = await _userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                return BadRequest(new { Message = "Failed to update account.", Errors = result.Errors });
            }

            return Ok(new { Message = "Account updated successfully."});
        }



    }

}

