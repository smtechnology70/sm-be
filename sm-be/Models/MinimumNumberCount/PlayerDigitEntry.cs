using SM_BE.Models;
using System.ComponentModel.DataAnnotations;

namespace sm_be.Models.MinimumNumberCount
{
    public class PlayerDigitEntry
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int DailyDigitGameId { get; set; }
        
        [Required]
        [Range(0, 9)]
        public int SelectedDigit { get; set; }
        
        public DateTime EntryTime { get; set; } = DateTime.UtcNow;
        
        public bool IsWinner { get; set; } = false;
        
        // Navigation properties
        public User User { get; set; } = null!;
        public DailyDigitGame DailyDigitGame { get; set; } = null!;
    }
}