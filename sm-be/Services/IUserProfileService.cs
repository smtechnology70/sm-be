using SM_BE.Dto;
using SM_BE.Models;

namespace SM_BE.Services
{
    public interface IUserProfileService
    {
        Task<UserProfileDto?> GetUserProfileAsync(int userId);
        Task<UserProfile> CreateUserProfileAsync(int userId, CreateUserProfileDto createDto);
        Task<UserProfile> UpdateUserProfileAsync(int userId, UpdateUserProfileDto updateDto);
        Task<bool> UpdatePrivacySettingsAsync(int userId, UserPrivacySettingsDto privacyDto);
        Task<UserGameStatsDto> GetUserGameStatsAsync(int userId);
        Task<bool> UpdateGameStatsAsync(int userId, string gameType, bool isWinner);
        Task<PublicUserProfileDto?> GetPublicUserProfileAsync(int userId);
        Task<List<UserProfileSummaryDto>> GetUserProfileSummariesAsync(List<int> userIds);
        Task<bool> UpdateProfilePictureAsync(int userId, string profilePictureUrl);
        Task<bool> UpdateLastActiveAsync(int userId);
        Task<bool> DeleteUserProfileAsync(int userId);
        Task<GetUserMoneyDto?> GetUserMoneyAsync(int userId);
        Task<bool> UpdateUserMoneyAsync(int userId, UpdateUserMoneyDto updateUserMoneyDto);
    }
}