using System.ComponentModel.DataAnnotations;

namespace SM_BE.Models
{
    public class UserProfile
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; } // Foreign key to User
        
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
        public string? Gender { get; set; } // Male, Female, Other
        
        [StringLength(100)]
        public string? Country { get; set; }
        
        [StringLength(100)]
        public string? State { get; set; }
        
        [StringLength(100)]
        public string? City { get; set; }
        
        [StringLength(255)]
        public string? ProfilePictureUrl { get; set; }
        
        [StringLength(255)]
        public string? CoverImageUrl { get; set; }
        
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
        
        // Verification Status
        public bool IsEmailVerified { get; set; } = false;
        public bool IsPhoneVerified { get; set; } = false;
        public bool IsProfileComplete { get; set; } = false;
        
        // Privacy Settings
        public bool ShowEmail { get; set; } = false;
        public bool ShowPhoneNumber { get; set; } = false;
        public bool ShowDateOfBirth { get; set; } = false;
        public bool ShowLocation { get; set; } = true;
        public bool ShowSocialLinks { get; set; } = true;
        public bool AllowMessaging { get; set; } = true;
        public bool ShowOnlineStatus { get; set; } = true;
        
        // Gaming Preferences
        [StringLength(50)]
        public string? PreferredGameMode { get; set; }
        
        [StringLength(20)]
        public string? PreferredLanguage { get; set; } = "en";
        
        [StringLength(10)]
        public string? TimeZone { get; set; }
        
        public bool ReceiveNotifications { get; set; } = true;
        public bool ReceiveEmailNotifications { get; set; } = true;
        public bool ReceiveSmsNotifications { get; set; } = false;
        
        // Gaming Statistics
        public int TotalGamesPlayed { get; set; } = 0;
        public int TotalWins { get; set; } = 0;
        public int TotalLosses { get; set; } = 0;
        public decimal WinPercentage { get; set; } = 0;
        public int CurrentStreak { get; set; } = 0;
        public int LongestWinStreak { get; set; } = 0;
        public int LongestLoseStreak { get; set; } = 0;
        
        // Game-specific statistics
        public int DailyNumberGamesPlayed { get; set; } = 0;
        public int DailyNumberWins { get; set; } = 0;
        public int DigitGamesPlayed { get; set; } = 0;
        public int DigitGameWins { get; set; } = 0;
        
        // Activity tracking
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? LastActiveAt { get; set; }
        public DateTime? LastGamePlayedAt { get; set; }
        
        // Achievements and badges
        public int TotalAchievements { get; set; } = 0;
        public int ExperiencePoints { get; set; } = 0;
        public int Level { get; set; } = 1;
        
        // Navigation property
        public User User { get; set; } = null!;

        // Money Management
        [Range(0, double.MaxValue)]
        public decimal InGameMoney { get; set; } = 0;

        [Range(0, double.MaxValue)]
        public decimal RealMoney { get; set; } = 0;
    }
}