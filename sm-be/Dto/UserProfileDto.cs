using System.ComponentModel.DataAnnotations;

namespace SM_BE.Dto
{
    public class UserProfileDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? Username { get; set; } // From User model
        public string? Name { get; set; } // From User model
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public string? Bio { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Gender { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public string? Occupation { get; set; }
        public string? Website { get; set; }
        public string? FacebookUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        public bool IsEmailVerified { get; set; }
        public bool IsPhoneVerified { get; set; }
        public bool IsProfileComplete { get; set; }
        public string? PreferredGameMode { get; set; }
        public string? PreferredLanguage { get; set; }
        public string? TimeZone { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastActiveAt { get; set; }
        
        // Gaming Statistics
        public UserGameStatsDto GameStats { get; set; } = new();
        
        // Privacy Settings
        public UserPrivacySettingsDto PrivacySettings { get; set; } = new();
        public decimal Money { get; set; }
    }

    public class CreateUserProfileDto
    {
        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }
        
        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        [StringLength(500)]
        public string? Bio { get; set; }
        
        [StringLength(100)]
        public string? FirstName { get; set; }
        
        [StringLength(100)]
        public string? LastName { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        
        [StringLength(10)]
        public string? Gender { get; set; }
        
        [StringLength(100)]
        public string? Country { get; set; }
        
        [StringLength(100)]
        public string? State { get; set; }
        
        [StringLength(100)]
        public string? City { get; set; }
        
        [StringLength(100)]
        public string? Occupation { get; set; }
        
        [StringLength(255)]
        public string? Website { get; set; }
        
        [StringLength(20)]
        public string? PreferredLanguage { get; set; } = "en";
        
        [StringLength(10)]
        public string? TimeZone { get; set; }
    }

    public class UpdateUserProfileDto
    {
        [EmailAddress]
        [StringLength(255)]
        public string? Email { get; set; }
        
        [Phone]
        [StringLength(20)]
        public string? PhoneNumber { get; set; }
        
        [StringLength(500)]
        public string? Bio { get; set; }
        
        [StringLength(100)]
        public string? FirstName { get; set; }
        
        [StringLength(100)]
        public string? LastName { get; set; }
        
        public DateTime? DateOfBirth { get; set; }
        
        [StringLength(10)]
        public string? Gender { get; set; }
        
        [StringLength(100)]
        public string? Country { get; set; }
        
        [StringLength(100)]
        public string? State { get; set; }
        
        [StringLength(100)]
        public string? City { get; set; }
        
        [StringLength(100)]
        public string? Occupation { get; set; }
        
        [StringLength(255)]
        public string? Website { get; set; }
        
        [StringLength(100)]
        public string? FacebookUrl { get; set; }
        
        [StringLength(100)]
        public string? TwitterUrl { get; set; }
        
        [StringLength(100)]
        public string? InstagramUrl { get; set; }
        
        [StringLength(100)]
        public string? LinkedInUrl { get; set; }
        
        [StringLength(50)]
        public string? PreferredGameMode { get; set; }
        
        [StringLength(20)]
        public string? PreferredLanguage { get; set; }
        
        [StringLength(10)]
        public string? TimeZone { get; set; }
    }

    public class UserPrivacySettingsDto
    {
        public bool ShowEmail { get; set; }
        public bool ShowPhoneNumber { get; set; }
        public bool ShowDateOfBirth { get; set; }
        public bool ShowLocation { get; set; }
        public bool ShowSocialLinks { get; set; }
        public bool AllowMessaging { get; set; }
        public bool ShowOnlineStatus { get; set; }
        public bool ReceiveNotifications { get; set; }
        public bool ReceiveEmailNotifications { get; set; }
        public bool ReceiveSmsNotifications { get; set; }
    }

    public class UserGameStatsDto
    {
        public int TotalGamesPlayed { get; set; }
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }
        public decimal WinPercentage { get; set; }
        public int CurrentStreak { get; set; }
        public int LongestWinStreak { get; set; }
        public int LongestLoseStreak { get; set; }
        public int DailyNumberGamesPlayed { get; set; }
        public int DailyNumberWins { get; set; }
        public int DigitGamesPlayed { get; set; }
        public int DigitGameWins { get; set; }
        public DateTime? LastGamePlayedAt { get; set; }
        public int TotalAchievements { get; set; }
        public int ExperiencePoints { get; set; }
        public int Level { get; set; }
    }

    public class PublicUserProfileDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Name { get; set; }
        public string? Bio { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public string? CoverImageUrl { get; set; }
        public DateTime CreatedAt { get; set; }
        
        // Conditional fields based on privacy settings
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string? Country { get; set; }
        public string? State { get; set; }
        public string? City { get; set; }
        public string? Website { get; set; }
        public string? FacebookUrl { get; set; }
        public string? TwitterUrl { get; set; }
        public string? InstagramUrl { get; set; }
        public string? LinkedInUrl { get; set; }
        
        // Gaming Statistics (always visible)
        public UserGameStatsDto GameStats { get; set; } = new();
    }

    public class UserProfileSummaryDto
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string? Username { get; set; }
        public string? Name { get; set; }
        public string? ProfilePictureUrl { get; set; }
        public bool IsOnline { get; set; }
        public DateTime? LastActiveAt { get; set; }
        public int Level { get; set; }
        public int TotalWins { get; set; }
        public decimal WinPercentage { get; set; }
    }
    public class GetUserMoneyDto
    {
        public decimal InGameMoney { get; set; }
        public decimal RealMoney { get; set; }
        public decimal TotalMoney { get; set; }
    }

    public class UpdateUserMoneyDto
    {
        [Range(0, double.MaxValue, ErrorMessage = "In-game money cannot be negative")]
        public decimal InGameMoney { get; set; } = 100;

        [Range(0, double.MaxValue, ErrorMessage = "Real money cannot be negative")]
        public decimal RealMoney { get; set; }
    }

    public class UploadFileResponseDto
    {
        public string FileName { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }
}