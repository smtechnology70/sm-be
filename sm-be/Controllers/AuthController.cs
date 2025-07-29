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
        private readonly IUserProfileService _userProfileService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            AppDbContext context, 
            IJwtService jwtService, 
            IConfiguration configuration,
            IUserProfileService userProfileService,
            ILogger<AuthController> logger)
        {
            _context = context;
            _jwtService = jwtService;
            _configuration = configuration;
            _userProfileService = userProfileService;
            _logger = logger;
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

            // Check if email already exists (if provided)
            if (!string.IsNullOrEmpty(register.Email))
            {
                var existingProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.Email == register.Email);
                
                if (existingProfile != null)
                    return BadRequest("Email address is already registered.");
            }

            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Hash the password before storing
                var hashedPassword = ComputeSha256Hash(register.Password);
                var user = new User
                {
                    Username = register.Username,
                    Name = register.Name,
                    PasswordHash = hashedPassword
                };

                // Save user to DB
                _context.Users.Add(user);
                await _context.SaveChangesAsync();

                // Create user profile automatically
                var createProfileDto = new CreateUserProfileDto
                {
                    Email = register.Email,
                    FirstName = register.FirstName,
                    LastName = register.LastName,
                    PreferredLanguage = "en"
                };

                var userProfile = await _userProfileService.CreateUserProfileAsync(user.Id, createProfileDto);

                await transaction.CommitAsync();

                _logger.LogInformation($"User {user.Username} (ID: {user.Id}) registered successfully with profile created");

                return Ok(new 
                { 
                    message = "User registered successfully!",
                    userId = user.Id,
                    profileId = userProfile.Id
                });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error during user registration");
                return StatusCode(500, "An error occurred during registration. Please try again.");
            }
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

            // Update last active in user profile if exists
            try
            {
                await _userProfileService.UpdateLastActiveAsync(existingUser.Id);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, $"Could not update last active for user {existingUser.Id}");
                // Don't fail login if profile update fails
            }

            var accessTokenExpiry = DateTime.UtcNow.AddMinutes(
                int.Parse(_configuration.GetSection("JwtSettings")["AccessTokenExpirationMinutes"] ?? "30"));

            // Get user profile info for response
            var userProfile = await _userProfileService.GetUserProfileAsync(existingUser.Id);

            var response = new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiration = accessTokenExpiry,
                User = new UserDto
                {
                    Id = existingUser.Id,
                    Username = existingUser.Username!,
                    Name = existingUser.Name!,
                    Email = userProfile?.Email,
                    ProfilePictureUrl = userProfile?.ProfilePictureUrl,
                    IsEmailVerified = userProfile?.IsEmailVerified ?? false
                }
            };

            return Ok(response);
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            try
            {
                // Get user ID and username from JWT claims
                var userIdClaim = User.FindFirst("userId");
                var userNameClaim = User.FindFirst("userName");

                if (userIdClaim == null || userNameClaim == null)
                    return Unauthorized("Invalid token claims.");

                if (!int.TryParse(userIdClaim.Value, out int userId))
                    return Unauthorized("Invalid user ID in token.");

                // Get user with profile info
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                    return NotFound("User not found.");

                var userProfile = await _userProfileService.GetUserProfileAsync(userId);

                var response = new
                {
                    UserId = userId,
                    UserName = userNameClaim.Value,
                    Name = user.Name,
                    Profile = userProfile != null ? new
                    {
                        Email = userProfile.Email,
                        FirstName = userProfile.FirstName,
                        LastName = userProfile.LastName,
                        ProfilePictureUrl = userProfile.ProfilePictureUrl,
                        IsProfileComplete = userProfile.IsProfileComplete,
                        Level = userProfile.GameStats?.Level ?? 1,
                        TotalWins = userProfile.GameStats?.TotalWins ?? 0
                    } : null
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting current user info");
                return StatusCode(500, "Internal server error");
            }
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

            // Get updated user profile info
            var userProfile = await _userProfileService.GetUserProfileAsync(user.Id);

            var response = new AuthResponseDto
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken,
                AccessTokenExpiration = accessTokenExpiry,
                User = new UserDto
                {
                    Id = user.Id,
                    Username = user.Username!,
                    Name = user.Name!,
                    Email = userProfile?.Email,
                    ProfilePictureUrl = userProfile?.ProfilePictureUrl,
                    IsEmailVerified = userProfile?.IsEmailVerified ?? false
                }
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