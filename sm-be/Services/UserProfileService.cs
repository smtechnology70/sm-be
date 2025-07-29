using Microsoft.EntityFrameworkCore;
using SM_BE.Data;
using SM_BE.Dto;
using SM_BE.Models;

namespace SM_BE.Services
{
    public class UserProfileService : IUserProfileService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UserProfileService> _logger;

        public UserProfileService(AppDbContext context, ILogger<UserProfileService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<UserProfileDto?> GetUserProfileAsync(int userId)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .Include(up => up.User)
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (profile == null)
                    return null;

                return new UserProfileDto
                {
                    Id = profile.Id,
                    UserId = profile.UserId,
                    Username = profile.User?.Username,
                    Name = profile.User?.Name,
                    Email = profile.Email,
                    PhoneNumber = profile.PhoneNumber,
                    Bio = profile.Bio,
                    FirstName = profile.FirstName,
                    LastName = profile.LastName,
                    DateOfBirth = profile.DateOfBirth,
                    Gender = profile.Gender,
                    Country = profile.Country,
                    State = profile.State,
                    City = profile.City,
                    ProfilePictureUrl = profile.ProfilePictureUrl,
                    CoverImageUrl = profile.CoverImageUrl,
                    Occupation = profile.Occupation,
                    Website = profile.Website,
                    FacebookUrl = profile.FacebookUrl,
                    TwitterUrl = profile.TwitterUrl,
                    InstagramUrl = profile.InstagramUrl,
                    LinkedInUrl = profile.LinkedInUrl,
                    IsEmailVerified = profile.IsEmailVerified,
                    IsPhoneVerified = profile.IsPhoneVerified,
                    IsProfileComplete = profile.IsProfileComplete,
                    PreferredGameMode = profile.PreferredGameMode,
                    PreferredLanguage = profile.PreferredLanguage,
                    TimeZone = profile.TimeZone,
                    CreatedAt = profile.CreatedAt,
                    LastActiveAt = profile.LastActiveAt,
                    GameStats = new UserGameStatsDto
                    {
                        TotalGamesPlayed = profile.TotalGamesPlayed,
                        TotalWins = profile.TotalWins,
                        TotalLosses = profile.TotalLosses,
                        WinPercentage = profile.WinPercentage,
                        CurrentStreak = profile.CurrentStreak,
                        LongestWinStreak = profile.LongestWinStreak,
                        LongestLoseStreak = profile.LongestLoseStreak,
                        DailyNumberGamesPlayed = profile.DailyNumberGamesPlayed,
                        DailyNumberWins = profile.DailyNumberWins,
                        DigitGamesPlayed = profile.DigitGamesPlayed,
                        DigitGameWins = profile.DigitGameWins,
                        LastGamePlayedAt = profile.LastGamePlayedAt,
                        TotalAchievements = profile.TotalAchievements,
                        ExperiencePoints = profile.ExperiencePoints,
                        Level = profile.Level
                    },
                    PrivacySettings = new UserPrivacySettingsDto
                    {
                        ShowEmail = profile.ShowEmail,
                        ShowPhoneNumber = profile.ShowPhoneNumber,
                        ShowDateOfBirth = profile.ShowDateOfBirth,
                        ShowLocation = profile.ShowLocation,
                        ShowSocialLinks = profile.ShowSocialLinks,
                        AllowMessaging = profile.AllowMessaging,
                        ShowOnlineStatus = profile.ShowOnlineStatus,
                        ReceiveNotifications = profile.ReceiveNotifications,
                        ReceiveEmailNotifications = profile.ReceiveEmailNotifications,
                        ReceiveSmsNotifications = profile.ReceiveSmsNotifications
                    },
                    Money=profile.RealMoney+ profile.InGameMoney

                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting user profile for user {userId}");
                return null;
            }
        }

        public async Task<UserProfile> CreateUserProfileAsync(int userId, CreateUserProfileDto createDto)
        {
            try
            {
                var existingProfile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (existingProfile != null)
                    throw new InvalidOperationException("User profile already exists");

                var profile = new UserProfile
                {
                    UserId = userId,
                    Email = createDto.Email,
                    PhoneNumber = createDto.PhoneNumber,
                    Bio = createDto.Bio,
                    FirstName = createDto.FirstName,
                    LastName = createDto.LastName,
                    DateOfBirth = createDto.DateOfBirth,
                    Gender = createDto.Gender,
                    Country = createDto.Country,
                    State = createDto.State,
                    City = createDto.City,
                    Occupation = createDto.Occupation,
                    Website = createDto.Website,
                    PreferredLanguage = createDto.PreferredLanguage ?? "en",
                    TimeZone = createDto.TimeZone,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    
                    // Set default money values
                    InGameMoney = 100, // Default starting money
                    RealMoney = 0      // Default real money
                };

                // Check profile completion
                profile.IsProfileComplete = IsProfileComplete(profile);

                _context.UserProfiles.Add(profile);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Created user profile for user {userId} with default InGameMoney: 100");
                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating user profile for user {userId}");
                throw;
            }
        }

        public async Task<UserProfile> UpdateUserProfileAsync(int userId, UpdateUserProfileDto updateDto)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (profile == null)
                    throw new InvalidOperationException("User profile not found");

                // Update fields
                if (!string.IsNullOrEmpty(updateDto.Email))
                    profile.Email = updateDto.Email;
                
                profile.PhoneNumber = updateDto.PhoneNumber;
                profile.Bio = updateDto.Bio;
                profile.FirstName = updateDto.FirstName;
                profile.LastName = updateDto.LastName;
                profile.DateOfBirth = updateDto.DateOfBirth;
                profile.Gender = updateDto.Gender;
                profile.Country = updateDto.Country;
                profile.State = updateDto.State;
                profile.City = updateDto.City;
                profile.Occupation = updateDto.Occupation;
                profile.Website = updateDto.Website;
                profile.FacebookUrl = updateDto.FacebookUrl;
                profile.TwitterUrl = updateDto.TwitterUrl;
                profile.InstagramUrl = updateDto.InstagramUrl;
                profile.LinkedInUrl = updateDto.LinkedInUrl;
                profile.PreferredGameMode = updateDto.PreferredGameMode;
                profile.PreferredLanguage = updateDto.PreferredLanguage;
                profile.TimeZone = updateDto.TimeZone;
                profile.UpdatedAt = DateTime.UtcNow;

                // Check profile completion
                profile.IsProfileComplete = IsProfileComplete(profile);

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Updated user profile for user {userId}");
                return profile;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating user profile for user {userId}");
                throw;
            }
        }

        public async Task<bool> UpdatePrivacySettingsAsync(int userId, UserPrivacySettingsDto privacyDto)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (profile == null)
                    return false;

                profile.ShowEmail = privacyDto.ShowEmail;
                profile.ShowPhoneNumber = privacyDto.ShowPhoneNumber;
                profile.ShowDateOfBirth = privacyDto.ShowDateOfBirth;
                profile.ShowLocation = privacyDto.ShowLocation;
                profile.ShowSocialLinks = privacyDto.ShowSocialLinks;
                profile.AllowMessaging = privacyDto.AllowMessaging;
                profile.ShowOnlineStatus = privacyDto.ShowOnlineStatus;
                profile.ReceiveNotifications = privacyDto.ReceiveNotifications;
                profile.ReceiveEmailNotifications = privacyDto.ReceiveEmailNotifications;
                profile.ReceiveSmsNotifications = privacyDto.ReceiveSmsNotifications;
                profile.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating privacy settings for user {userId}");
                return false;
            }
        }

        public async Task<UserGameStatsDto> GetUserGameStatsAsync(int userId)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (profile == null)
                    return new UserGameStatsDto();

                return new UserGameStatsDto
                {
                    TotalGamesPlayed = profile.TotalGamesPlayed,
                    TotalWins = profile.TotalWins,
                    TotalLosses = profile.TotalLosses,
                    WinPercentage = profile.WinPercentage,
                    CurrentStreak = profile.CurrentStreak,
                    LongestWinStreak = profile.LongestWinStreak,
                    LongestLoseStreak = profile.LongestLoseStreak,
                    DailyNumberGamesPlayed = profile.DailyNumberGamesPlayed,
                    DailyNumberWins = profile.DailyNumberWins,
                    DigitGamesPlayed = profile.DigitGamesPlayed,
                    DigitGameWins = profile.DigitGameWins,
                    LastGamePlayedAt = profile.LastGamePlayedAt,
                    TotalAchievements = profile.TotalAchievements,
                    ExperiencePoints = profile.ExperiencePoints,
                    Level = profile.Level
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting game stats for user {userId}");
                return new UserGameStatsDto();
            }
        }

        public async Task<bool> UpdateGameStatsAsync(int userId, string gameType, bool isWinner)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (profile == null)
                    return false;

                // Update general stats
                profile.TotalGamesPlayed++;
                if (isWinner)
                {
                    profile.TotalWins++;
                    profile.CurrentStreak++;
                    if (profile.CurrentStreak > profile.LongestWinStreak)
                        profile.LongestWinStreak = profile.CurrentStreak;
                }
                else
                {
                    profile.TotalLosses++;
                    profile.CurrentStreak = 0;
                }

                // Update game-specific stats
                switch (gameType.ToLower())
                {
                    case "dailynumber":
                        profile.DailyNumberGamesPlayed++;
                        if (isWinner) profile.DailyNumberWins++;
                        break;
                    case "digit":
                        profile.DigitGamesPlayed++;
                        if (isWinner) profile.DigitGameWins++;
                        break;
                }

                // Calculate win percentage
                if (profile.TotalGamesPlayed > 0)
                    profile.WinPercentage = Math.Round((decimal)profile.TotalWins / profile.TotalGamesPlayed * 100, 2);

                // Update experience and level (simple calculation)
                profile.ExperiencePoints += isWinner ? 10 : 5;
                profile.Level = (profile.ExperiencePoints / 100) + 1;

                profile.LastGamePlayedAt = DateTime.UtcNow;
                profile.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating game stats for user {userId}");
                return false;
            }
        }

        public async Task<PublicUserProfileDto?> GetPublicUserProfileAsync(int userId)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .Include(up => up.User)
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (profile == null)
                    return null;

                return new PublicUserProfileDto
                {
                    Id = profile.Id,
                    UserId = profile.UserId,
                    Username = profile.User?.Username,
                    Name = profile.User?.Name,
                    Bio = profile.Bio,
                    ProfilePictureUrl = profile.ProfilePictureUrl,
                    CoverImageUrl = profile.CoverImageUrl,
                    CreatedAt = profile.CreatedAt,
                    
                    // Conditional fields based on privacy settings
                    Email = profile.ShowEmail ? profile.Email : null,
                    PhoneNumber = profile.ShowPhoneNumber ? profile.PhoneNumber : null,
                    DateOfBirth = profile.ShowDateOfBirth ? profile.DateOfBirth : null,
                    Country = profile.ShowLocation ? profile.Country : null,
                    State = profile.ShowLocation ? profile.State : null,
                    City = profile.ShowLocation ? profile.City : null,
                    Website = profile.ShowSocialLinks ? profile.Website : null,
                    FacebookUrl = profile.ShowSocialLinks ? profile.FacebookUrl : null,
                    TwitterUrl = profile.ShowSocialLinks ? profile.TwitterUrl : null,
                    InstagramUrl = profile.ShowSocialLinks ? profile.InstagramUrl : null,
                    LinkedInUrl = profile.ShowSocialLinks ? profile.LinkedInUrl : null,
                    
                    GameStats = new UserGameStatsDto
                    {
                        TotalGamesPlayed = profile.TotalGamesPlayed,
                        TotalWins = profile.TotalWins,
                        TotalLosses = profile.TotalLosses,
                        WinPercentage = profile.WinPercentage,
                        CurrentStreak = profile.CurrentStreak,
                        LongestWinStreak = profile.LongestWinStreak,
                        LongestLoseStreak = profile.LongestLoseStreak,
                        DailyNumberGamesPlayed = profile.DailyNumberGamesPlayed,
                        DailyNumberWins = profile.DailyNumberWins,
                        DigitGamesPlayed = profile.DigitGamesPlayed,
                        DigitGameWins = profile.DigitGameWins,
                        LastGamePlayedAt = profile.LastGamePlayedAt,
                        TotalAchievements = profile.TotalAchievements,
                        ExperiencePoints = profile.ExperiencePoints,
                        Level = profile.Level
                    }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting public profile for user {userId}");
                return null;
            }
        }

        public async Task<List<UserProfileSummaryDto>> GetUserProfileSummariesAsync(List<int> userIds)
        {
            try
            {
                return await _context.UserProfiles
                    .Include(up => up.User)
                    .Where(up => userIds.Contains(up.UserId))
                    .Select(up => new UserProfileSummaryDto
                    {
                        Id = up.Id,
                        UserId = up.UserId,
                        Username = up.User!.Username,
                        Name = up.User.Name,
                        ProfilePictureUrl = up.ProfilePictureUrl,
                        IsOnline = up.LastActiveAt.HasValue && up.LastActiveAt > DateTime.UtcNow.AddMinutes(-15),
                        LastActiveAt = up.LastActiveAt,
                        Level = up.Level,
                        TotalWins = up.TotalWins,
                        WinPercentage = up.WinPercentage
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user profile summaries");
                return new List<UserProfileSummaryDto>();
            }
        }

        public async Task<bool> UpdateProfilePictureAsync(int userId, string profilePictureUrl)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (profile == null)
                    return false;

                profile.ProfilePictureUrl = profilePictureUrl;
                profile.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating profile picture for user {userId}");
                return false;
            }
        }

        public async Task<bool> UpdateLastActiveAsync(int userId)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (profile == null)
                    return false;

                profile.LastActiveAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating last active for user {userId}");
                return false;
            }
        }

        public async Task<bool> DeleteUserProfileAsync(int userId)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (profile == null)
                    return false;

                _context.UserProfiles.Remove(profile);
                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting user profile for user {userId}");
                return false;
            }
        }

        public async Task<GetUserMoneyDto?> GetUserMoneyAsync(int userId)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);
                if (profile == null)
                    throw new InvalidOperationException("User profile not found");

                return new GetUserMoneyDto
                {
                    InGameMoney=profile.InGameMoney,
                    RealMoney=profile.RealMoney,
                    TotalMoney = profile.InGameMoney + profile.RealMoney
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting money balance for user {userId}");
                return null;
            }
        }

        public async Task<bool> UpdateUserMoneyAsync(int userId , UpdateUserMoneyDto updateUserMoneyDto)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);
                if (profile == null)
                    throw new InvalidOperationException("User profile not found");


                profile.InGameMoney = updateUserMoneyDto.InGameMoney;
                profile.RealMoney = updateUserMoneyDto.RealMoney;

                await _context.SaveChangesAsync();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting money balance for user {userId}");
                return false;
            }
        }

        private static bool IsProfileComplete(UserProfile profile)
        {
            return !string.IsNullOrEmpty(profile.FirstName) &&
                   !string.IsNullOrEmpty(profile.LastName) &&
                   !string.IsNullOrEmpty(profile.Email) &&
                   profile.DateOfBirth.HasValue &&
                   !string.IsNullOrEmpty(profile.Country);
        }
    }
}