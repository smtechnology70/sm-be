using SM_BE.Dto;

namespace SM_BE.Services
{
    public interface IMoneyTransactionService
    {
        Task<MoneyTransactionResultDto> DeductMoneyAsync(MoneyTransactionRequestDto request);
        Task<MoneyAddResultDto> AddMoneyAsync(int userId, decimal amount, MoneyType moneyType, string transactionType, string? description = null, string? gameType = null, string? gameId = null, string? referenceId = null);
        Task<GetUserMoneyDto?> GetUserMoneyAsync(int userId);
        Task<bool> HasSufficientFundsAsync(int userId, decimal amount);
        Task<MoneyTransactionResultDto> ProcessGameEntryAsync(int userId, decimal entryFee, string gameType, string? gameId = null);
        Task<MoneyAddResultDto> ProcessGameWinAsync(int userId, decimal winAmount, string gameType, string? gameId = null);
        
        // Transaction history methods
        Task<List<MoneyTransactionHistoryDto>> GetTransactionHistoryAsync(int userId, int page = 1, int pageSize = 20);
        Task<List<MoneyTransactionHistoryDto>> GetFilteredTransactionsAsync(TransactionFilterDto filter);
        Task<TransactionSummaryDto> GetTransactionSummaryAsync(int userId, DateTime? fromDate = null, DateTime? toDate = null);
        Task<MoneyTransactionHistoryDto?> GetTransactionByIdAsync(int transactionId);
        Task<List<MoneyTransactionHistoryDto>> GetTransactionsByReferenceIdAsync(string referenceId);
    }
}