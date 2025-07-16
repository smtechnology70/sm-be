using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SM_BE.Data;
using SM_BE.Dto;
using SM_BE.Models;
using SM_BE.Services;
using System.Security.Cryptography;
using System.Text;

namespace SM_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;

        public AuthController(AppDbContext context, IJwtService jwtService, IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto register)
        {
            if (register.Username == null || register.Password == null)
                return BadRequest("Username and password are required.");

            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == register.Username);

            if (existingUser != null)
                return BadRequest("Username already exists.");

            // Hash the password before storing
            register.Password = ComputeSha256Hash(register.Password);
            User user = new User
            {
                Username = register.Username,
                Name = register.Name,
                PasswordHash = register.Password
            };

            // Save user to DB
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            return Ok("User registered successfully!");
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            if (loginDto.Username == null || loginDto.Password == null)
                return BadRequest("Username and password are required.");

            // Fetch user from DB
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Username == loginDto.Username);

            if (existingUser == null)
                return NotFound("User not found.");

            // Compare hashes
            var incomingHash = ComputeSha256Hash(loginDto.Password);
            if (incomingHash != existingUser.PasswordHash)
                return Unauthorized("Invalid username or password.");

            // Generate tokens
            var accessToken = _jwtService.GenerateAccessToken(existingUser);
            var refreshToken = _jwtService.GenerateRefreshToken();

            // Update user with refresh token
            existingUser.RefreshToken = refreshToken;
            existingUser.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
                int.Parse(_configuration.GetSection("JwtSettings")["RefreshTokenExpirationDays"] ?? "7"));

            await _context.SaveChangesAsync();

            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration.GetSection("JwtSettings")["AccessTokenExpirationMinutes"] ?? "30"));

            var response = new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiration = accessTokenExpiry,
            };

            return Ok(response);
        }

        [HttpGet("me")]
        [Authorize]
        public IActionResult GetCurrentUser()
        {
            // Get user ID and username from JWT claims
            var userIdClaim = User.FindFirst("userId");
            var userNameClaim = User.FindFirst("userName");

            if (userIdClaim == null || userNameClaim == null)
                return Unauthorized("Invalid token claims.");

            if (!int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized("Invalid user ID in token.");

            var response = new
            {
                UserId = userId,
                UserName = userNameClaim.Value
            };

            return Ok(response);
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            if (string.IsNullOrEmpty(refreshTokenDto.RefreshToken))
                return BadRequest("Refresh token is required.");

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.RefreshToken == refreshTokenDto.RefreshToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return Unauthorized("Invalid or expired refresh token.");

            // Generate new tokens
            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            // Update user with new refresh token
            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(
                int.Parse(_configuration.GetSection("JwtSettings")["RefreshTokenExpirationDays"] ?? "7"));

            await _context.SaveChangesAsync();

            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration.GetSection("JwtSettings")["AccessTokenExpirationMinutes"] ?? "30"));

            var response = new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiration = accessTokenExpiry,
            };

            return Ok(response);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                return Unauthorized();

            var user = await _context.Users.FindAsync(userId);
            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiryTime = null;
                await _context.SaveChangesAsync();
            }

            return Ok("Logged out successfully!");
        }

        // Example SHA-256 hashing
        private static string ComputeSha256Hash(string rawData)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(rawData));
            return Convert.ToBase64String(bytes);
        }
    }
}