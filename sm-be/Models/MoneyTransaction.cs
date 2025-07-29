using System.ComponentModel.DataAnnotations;

namespace SM_BE.Models
{
    public class MoneyTransaction
    {
        public int Id { get; set; }
        
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [Range(0, double.MaxValue)]
        public decimal Amount { get; set; }
        
        [Required]
        [StringLength(20)]
        public string TransactionDirection { get; set; } = string.Empty; // "DEBIT" or "CREDIT"
        
        [Required]
        [StringLength(20)]
        public string MoneyType { get; set; } = string.Empty; // "InGameMoney" or "RealMoney"
        
        [Required]
        [StringLength(100)]
        public string TransactionType { get; set; } = string.Empty; // "game_entry", "game_win", "purchase", etc.
        
        [StringLength(255)]
        public string? Description { get; set; }
        
        [StringLength(50)]
        public string? GameType { get; set; } // "zero-blast", "daily-number", "digit-game"
        
        [StringLength(100)]
        public string? GameId { get; set; } // For tracking specific game instances
        
        public decimal BalanceAfter { get; set; } // Balance after this transaction
        
        public decimal InGameMoneyAfter { get; set; } // InGameMoney balance after transaction
        
        public decimal RealMoneyAfter { get; set; } // RealMoney balance after transaction
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        
        [StringLength(50)]
        public string? ReferenceId { get; set; } // For linking related transactions
        
        // Navigation property
        public User User { get; set; } = null!;
    }
}