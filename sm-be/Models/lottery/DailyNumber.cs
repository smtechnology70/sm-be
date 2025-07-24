using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SM_BE.Models.Lottery
{
    public class DailyNumber
    {
        public int Id { get; set; }
        
        [Required]
        public DateTime Date { get; set; }
        
        [Required]
        [Range(0, 99)]
        public int WinningNumber { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        // Navigation property for player entries
        public ICollection<PlayerEntry> PlayerEntries { get; set; } = new List<PlayerEntry>();
    }
}