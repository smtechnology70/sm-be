using System.ComponentModel.DataAnnotations;

namespace SM_BE.Dto
{
    public class MoneyTransactionRequestDto
    {
        [Required]
        public int UserId { get; set; }
        
        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }
        
        [Required]
        [StringLength(100)]
        public string TransactionType { get; set; } = string.Empty; // e.g., "game_entry", "game_win", "purchase"
        
        [StringLength(255)]
        public string? Description { get; set; }
        
        [StringLength(50)]
        public string? GameType { get; set; } // e.g., "zero-blast", "daily-number", "digit-game"
        
        [StringLength(100)]
        public string? GameId { get; set; } // For tracking specific game instances
        
        [StringLength(50)]
        public string? ReferenceId { get; set; } // For linking related transactions
    }

    public class MoneyTransactionResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal RemainingInGameMoney { get; set; }
        public decimal RemainingRealMoney { get; set; }
        public decimal TotalRemainingMoney { get; set; }
        public decimal AmountDeducted { get; set; }
        public decimal AmountFromInGameMoney { get; set; }
        public decimal AmountFromRealMoney { get; set; }
        public List<int> TransactionIds { get; set; } = new(); // IDs of created transaction records
    }

    public class MoneyAddResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal NewInGameMoney { get; set; }
        public decimal NewRealMoney { get; set; }
        public decimal TotalMoney { get; set; }
        public decimal AmountAdded { get; set; }
        public int TransactionId { get; set; } // ID of created transaction record
    }

    public class MoneyTransactionHistoryDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public string TransactionDirection { get; set; } = string.Empty; // "DEBIT" or "CREDIT"
        public string MoneyType { get; set; } = string.Empty; // "InGameMoney" or "RealMoney"
        public string TransactionType { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? GameType { get; set; }
        public string? GameId { get; set; }
        public decimal BalanceAfter { get; set; }
        public decimal InGameMoneyAfter { get; set; }
        public decimal RealMoneyAfter { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ReferenceId { get; set; }
    }

    public class TransactionFilterDto
    {
        public int? UserId { get; set; }
        public string? TransactionDirection { get; set; } // "DEBIT", "CREDIT", or null for all
        public string? MoneyType { get; set; } // "InGameMoney", "RealMoney", or null for all
        public string? TransactionType { get; set; }
        public string? GameType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class TransactionSummaryDto
    {
        public decimal TotalCredits { get; set; }
        public decimal TotalDebits { get; set; }
        public decimal NetAmount { get; set; }
        public int TotalTransactions { get; set; }
        public decimal CurrentInGameMoney { get; set; }
        public decimal CurrentRealMoney { get; set; }
        public decimal CurrentTotalMoney { get; set; }
    }

    public enum MoneyType
    {
        InGameMoney,
        RealMoney
    }
}