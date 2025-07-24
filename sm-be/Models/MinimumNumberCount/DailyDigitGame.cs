using System.ComponentModel.DataAnnotations;

namespace sm_be.Models.MinimumNumberCount
{
    public class DailyDigitGame
    {
        public int Id { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Range(0, 9)]
        public int? WinningDigit { get; set; } // Will be set at end of day
        
        public bool IsCompleted { get; set; } = false;
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? CompletedAt { get; set; }
        
        // Navigation property for player digit entries
        public ICollection<PlayerDigitEntry> PlayerDigitEntries { get; set; } = new List<PlayerDigitEntry>();
    }
}