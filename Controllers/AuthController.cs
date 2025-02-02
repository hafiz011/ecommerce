using ecommerce.Models;
using ecommerce.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity.Data;

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

        public AuthController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager, IConfiguration configuration, RoleManager<ApplicationRole> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
            _roleManager = roleManager;
        }

        // POST: api/auth/login
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginRequestModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null || !await _userManager.CheckPasswordAsync(user, model.Password))
                return Unauthorized(new { Message = "Invalid email or password" });

            var roles = await _userManager.GetRolesAsync(user);
            var role = roles.Count > 0 ? roles[0] : "User"; // Default to "User" if no roles are assigned

            // Use the JwtTokenHelper to generate the token
            var token = JwtTokenHelper.GenerateToken(user.Id.ToString(), role, _configuration["JwtSettings:Key"], _configuration["JwtSettings:Issuer"], _configuration["JwtSettings:Audience"]);

            return Ok(new
            {
                Token = token,
                User = new
                {
                    user.Id,
                    user.FirstName,
                    user.LastName,
                    user.Email,
                    user.PhoneNumber,
                }
            });
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            // Create a new ApplicationUser object
            var user = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                FirstName = model.FirstName,
                LastName = model.LastName,
                PhoneNumber = model.Phone,
                Address = model.Address
            };

            // Create the user in the database
            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
                return BadRequest(result.Errors);

            // Assign the "User" role to the new account
            const string defaultRole = "User";

            // Check if the role exists; if not, create it
            if (!await _roleManager.RoleExistsAsync(defaultRole))
            {
                var roleResult = await _roleManager.CreateAsync(new ApplicationRole { Name = defaultRole });
                if (!roleResult.Succeeded)
                {
                    return BadRequest(new { Message = "Failed to create default role." });
                }
            }

            // Add the user to the "User" role
            await _userManager.AddToRoleAsync(user, defaultRole);

            return Ok(new { Message = "User registered successfully!" });
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword(RegisterModel model)
        {
            if (ModelState.IsNullOrEmpty())
                return BadRequest(ModelState);

            // Find the user by email
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
                return BadRequest(new { Message = "User not found" });

            // Generate the reset token
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);

            // You should send the token to the user's email, but for now, just return it for testing
            // In a production app, use an email service like SendGrid, SMTP, etc.
            // EmailService.SendResetPasswordEmail(user.Email, token);

            return Ok(new { Message = "Password reset token sent to your email." });
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

            return Ok(new { Message = "Password reset successfully." });
        }



    }

}

