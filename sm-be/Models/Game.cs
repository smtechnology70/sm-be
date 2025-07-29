using System;
using System.ComponentModel.DataAnnotations;

namespace SM_BE.Models
{
    public class Game
    {
        public int Id { get; set; }
        
        [Required]
        [StringLength(100)]
        public string GameId { get; set; } = string.Empty; // The unique game identifier (GUID)
        
        [Required]
        [StringLength(50)]
        public string GameType { get; set; } = string.Empty; // "zero-blast", "daily-number", etc.
        
        [Required]
        public int Player1Id { get; set; }
        
        [Required]
        public int Player2Id { get; set; }
        
        public int? WinnerId { get; set; } // Null if game didn't finish or was a draw
        
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = "Playing"; // "Playing", "Finished", "Abandoned"
        
        [Required]
        public decimal EntryFee { get; set; }
        
        public decimal? WinAmount { get; set; } // Amount won by the winner
        
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        
        public DateTime? FinishedAt { get; set; }
        
        [StringLength(500)]
        public string? GameData { get; set; } // JSON data for game-specific information
        
        // Navigation properties
        public User Player1 { get; set; } = null!;
        public User Player2 { get; set; } = null!;
        public User? Winner { get; set; }
    }
}