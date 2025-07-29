using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SM_BE.Dto;
using SM_BE.Services;
using System.Security.Claims;

namespace SM_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserProfileController : ControllerBase
    {
        private readonly IUserProfileService _userProfileService;
        private readonly ILogger<UserProfileController> _logger;

        public UserProfileController(IUserProfileService userProfileService, ILogger<UserProfileController> logger)
        {
            _userProfileService = userProfileService;
            _logger = logger;
        }

        /// <summary>
        /// Get current user's profile
        /// </summary>
        [HttpGet("me")]
        public async Task<IActionResult> GetMyProfile()
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized("Invalid user token");

                var profile = await _userProfileService.GetUserProfileAsync(userId.Value);
                if (profile == null)
                    return NotFound("User profile not found");

                return Ok(profile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Create user profile
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CreateProfile([FromBody] CreateUserProfileDto createDto)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized("Invalid user token");

                var profile = await _userProfileService.CreateUserProfileAsync(userId.Value, createDto);

                return CreatedAtAction(nameof(GetMyProfile), new { id = profile.Id }, new
                {
                    message = "Profile created successfully",
                    profileId = profile.Id
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating user profile");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update current user's profile
        /// </summary>
        [HttpPut("me")]
        public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateUserProfileDto updateDto)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized("Invalid user token");

                var updatedProfile = await _userProfileService.UpdateUserProfileAsync(userId.Value, updateDto);

                return Ok(new
                {
                    message = "Profile updated successfully",
                    profileId = updatedProfile.Id
                });
            }
            catch (InvalidOperationException ex)
            {
                return NotFound(new { message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user profile");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update privacy settings
        /// </summary>
        [HttpPut("privacy")]
        public async Task<IActionResult> UpdatePrivacySettings([FromBody] UserPrivacySettingsDto privacyDto)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized("Invalid user token");

                var success = await _userProfileService.UpdatePrivacySettingsAsync(userId.Value, privacyDto);
                if (!success)
                    return NotFound("User profile not found");

                return Ok(new { message = "Privacy settings updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating privacy settings");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get current user's game statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetMyGameStats()
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized("Invalid user token");

                var stats = await _userProfileService.GetUserGameStatsAsync(userId.Value);
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user game stats");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update game statistics (typically called internally by game services)
        /// </summary>
        [HttpPost("stats/update")]
        public async Task<IActionResult> UpdateGameStats([FromBody] UpdateGameStatsRequest request)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized("Invalid user token");

                var success = await _userProfileService.UpdateGameStatsAsync(userId.Value, request.GameType, request.IsWinner);
                if (!success)
                    return NotFound("User profile not found");

                return Ok(new { message = "Game stats updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating game stats");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get public profile of any user
        /// </summary>
        [HttpGet("public/{userId}")]
        [AllowAnonymous]
        public async Task<IActionResult> GetPublicProfile(int userId)
        {
            try
            {
                var publicProfile = await _userProfileService.GetPublicUserProfileAsync(userId);
                if (publicProfile == null)
                    return NotFound("User profile not found");

                return Ok(publicProfile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting public profile for user {UserId}", userId);
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get profile summaries for multiple users
        /// </summary>
        [HttpPost("summaries")]
        public async Task<IActionResult> GetProfileSummaries([FromBody] GetProfileSummariesRequest request)
        {
            try
            {
                if (request.UserIds == null || !request.UserIds.Any())
                    return BadRequest("User IDs are required");

                var summaries = await _userProfileService.GetUserProfileSummariesAsync(request.UserIds);
                return Ok(summaries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile summaries");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Upload profile picture
        /// </summary>
        [HttpPost("upload-avatar")]
        public async Task<IActionResult> UploadAvatar(IFormFile file)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized("Invalid user token");

                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                // Validate file type and size
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                
                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest("Only image files (jpg, jpeg, png, gif, webp) are allowed");

                if (file.Length > 5 * 1024 * 1024) // 5MB limit
                    return BadRequest("File size cannot exceed 5MB");

                // Generate unique filename
                var fileName = $"avatar_{userId}_{Guid.NewGuid()}{fileExtension}";
                var uploadsFolder = Path.Combine("wwwroot", "uploads", "avatars");
                
                // Create directory if it doesn't exist
                Directory.CreateDirectory(uploadsFolder);
                
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Update profile picture URL
                var profilePictureUrl = $"/uploads/avatars/{fileName}";
                var success = await _userProfileService.UpdateProfilePictureAsync(userId.Value, profilePictureUrl);
                
                if (!success)
                    return NotFound("User profile not found");

                return Ok(new UploadFileResponseDto
                {
                    FileName = fileName,
                    FileUrl = profilePictureUrl,
                    Message = "Avatar uploaded successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading avatar");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Upload cover image
        /// </summary>
        [HttpPost("upload-cover")]
        public async Task<IActionResult> UploadCoverImage(IFormFile file)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized("Invalid user token");

                if (file == null || file.Length == 0)
                    return BadRequest("No file uploaded");

                // Validate file type and size
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
                var fileExtension = Path.GetExtension(file.FileName).ToLower();
                
                if (!allowedExtensions.Contains(fileExtension))
                    return BadRequest("Only image files (jpg, jpeg, png, gif, webp) are allowed");

                if (file.Length > 10 * 1024 * 1024) // 10MB limit for cover images
                    return BadRequest("File size cannot exceed 10MB");

                // Generate unique filename
                var fileName = $"cover_{userId}_{Guid.NewGuid()}{fileExtension}";
                var uploadsFolder = Path.Combine("wwwroot", "uploads", "covers");
                
                // Create directory if it doesn't exist
                Directory.CreateDirectory(uploadsFolder);
                
                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save file
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // For cover image, we'd need to add a method to update it in the service
                // For now, returning the URL
                var coverImageUrl = $"/uploads/covers/{fileName}";

                return Ok(new UploadFileResponseDto
                {
                    FileName = fileName,
                    FileUrl = coverImageUrl,
                    Message = "Cover image uploaded successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading cover image");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update last active timestamp
        /// </summary>
        [HttpPost("activity")]
        public async Task<IActionResult> UpdateActivity()
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized("Invalid user token");

                var success = await _userProfileService.UpdateLastActiveAsync(userId.Value);
                if (!success)
                    return NotFound("User profile not found");

                return Ok(new { message = "Activity updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user activity");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get current user's money balance
        /// </summary>
        [HttpGet("money")]
        public async Task<IActionResult> GetMyMoney()
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized("Invalid user token");

                var money = await _userProfileService.GetUserMoneyAsync(userId.Value);
                if (money == null)
                    return NotFound("User profile not found");

                return Ok(money);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user money");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Update current user's money balance
        /// </summary>
        [HttpPut("money")]
        public async Task<IActionResult> UpdateMyMoney([FromBody] UpdateUserMoneyDto updateMoneyDto)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized("Invalid user token");

                var success = await _userProfileService.UpdateUserMoneyAsync(userId.Value, updateMoneyDto);
                if (!success)
                    return NotFound("User profile not found");

                return Ok(new { message = "Money balance updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating user money");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Delete current user's profile
        /// </summary>
        [HttpDelete("me")]
        public async Task<IActionResult> DeleteMyProfile()
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized("Invalid user token");

                var success = await _userProfileService.DeleteUserProfileAsync(userId.Value);
                if (!success)
                    return NotFound("User profile not found");

                return Ok(new { message = "Profile deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting user profile");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Search user profiles by criteria
        /// </summary>
        [HttpGet("search")]
        public async Task<IActionResult> SearchProfiles([FromQuery] string? query = null, 
                                                        [FromQuery] string? country = null, 
                                                        [FromQuery] int page = 1, 
                                                        [FromQuery] int pageSize = 20)
        {
            try
            {
                // This would require implementing a search method in the service
                // For now, return a placeholder response
                return Ok(new { message = "Search functionality not yet implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching profiles");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        /// <summary>
        /// Get leaderboard based on various criteria
        /// </summary>
        [HttpGet("leaderboard")]
        [AllowAnonymous]
        public async Task<IActionResult> GetLeaderboard([FromQuery] string type = "wins", 
                                                       [FromQuery] int limit = 50)
        {
            try
            {
                // This would require implementing a leaderboard method in the service
                // For now, return a placeholder response
                return Ok(new { message = "Leaderboard functionality not yet implemented" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting leaderboard");
                return StatusCode(500, new { message = "Internal server error" });
            }
        }

        private int? GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }
    }

    // Request DTOs for the controller
    public class UpdateGameStatsRequest
    {
        public string GameType { get; set; } = string.Empty;
        public bool IsWinner { get; set; }
    }

    public class GetProfileSummariesRequest
    {
        public List<int> UserIds { get; set; } = new();
    }
}