using System.ComponentModel.DataAnnotations;

namespace SM_BE.Models.Lottery
{
    public class PlayerEntry
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        public int DailyNumberId { get; set; }
        
        [Required]
        [Range(0, 99)]
        public int GuessedNumber { get; set; }
        
        public DateTime EntryTime { get; set; } = DateTime.UtcNow;
        
        public bool IsWinner { get; set; } = false;
        
        // Navigation properties
        public User User { get; set; } = null!;
        public DailyNumber DailyNumber { get; set; } = null!;
    }
}