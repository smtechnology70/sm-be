using Microsoft.EntityFrameworkCore;
using SM_BE.Data;
using SM_BE.Dto;
using SM_BE.Models;

namespace SM_BE.Services
{
    public class MoneyTransactionService : IMoneyTransactionService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<MoneyTransactionService> _logger;

        public MoneyTransactionService(AppDbContext context, ILogger<MoneyTransactionService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<MoneyTransactionResultDto> DeductMoneyAsync(MoneyTransactionRequestDto request)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == request.UserId);

                if (profile == null)
                {
                    return new MoneyTransactionResultDto
                    {
                        Success = false,
                        Message = "User profile not found"
                    };
                }

                var totalAvailable = profile.InGameMoney + profile.RealMoney;
                
                if (totalAvailable < request.Amount)
                {
                    return new MoneyTransactionResultDto
                    {
                        Success = false,
                        Message = $"Insufficient funds. Required: {request.Amount:C}, Available: {totalAvailable:C}",
                        RemainingInGameMoney = profile.InGameMoney,
                        RemainingRealMoney = profile.RealMoney,
                        TotalRemainingMoney = totalAvailable
                    };
                }

                decimal amountFromInGameMoney = 0;
                decimal amountFromRealMoney = 0;
                decimal remainingToDeduct = request.Amount;
                var transactionIds = new List<int>();

                // First, deduct from InGameMoney
                if (profile.InGameMoney > 0 && remainingToDeduct > 0)
                {
                    amountFromInGameMoney = Math.Min(profile.InGameMoney, remainingToDeduct);
                    profile.InGameMoney -= amountFromInGameMoney;
                    remainingToDeduct -= amountFromInGameMoney;

                    // Log InGameMoney transaction
                    var inGameTransaction = new MoneyTransaction
                    {
                        UserId = request.UserId,
                        Amount = amountFromInGameMoney,
                        TransactionDirection = "DEBIT",
                        MoneyType = "InGameMoney",
                        TransactionType = request.TransactionType,
                        Description = request.Description,
                        GameType = request.GameType,
                        GameId = request.GameId,
                        ReferenceId = request.ReferenceId,
                        BalanceAfter = profile.InGameMoney + profile.RealMoney,
                        InGameMoneyAfter = profile.InGameMoney,
                        RealMoneyAfter = profile.RealMoney,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.MoneyTransactions.Add(inGameTransaction);
                    await _context.SaveChangesAsync();
                    transactionIds.Add(inGameTransaction.Id);
                }

                // Then, deduct remaining from RealMoney if needed
                if (remainingToDeduct > 0)
                {
                    amountFromRealMoney = remainingToDeduct;
                    profile.RealMoney -= amountFromRealMoney;

                    // Log RealMoney transaction
                    var realMoneyTransaction = new MoneyTransaction
                    {
                        UserId = request.UserId,
                        Amount = amountFromRealMoney,
                        TransactionDirection = "DEBIT",
                        MoneyType = "RealMoney",
                        TransactionType = request.TransactionType,
                        Description = request.Description,
                        GameType = request.GameType,
                        GameId = request.GameId,
                        ReferenceId = request.ReferenceId,
                        BalanceAfter = profile.InGameMoney + profile.RealMoney,
                        InGameMoneyAfter = profile.InGameMoney,
                        RealMoneyAfter = profile.RealMoney,
                        CreatedAt = DateTime.UtcNow
                    };

                    _context.MoneyTransactions.Add(realMoneyTransaction);
                    await _context.SaveChangesAsync();
                    transactionIds.Add(realMoneyTransaction.Id);
                }

                profile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Money deducted successfully for user {request.UserId}. " +
                    $"Amount: {request.Amount:C}, From InGame: {amountFromInGameMoney:C}, From Real: {amountFromRealMoney:C}. " +
                    $"Transaction: {request.TransactionType}");

                return new MoneyTransactionResultDto
                {
                    Success = true,
                    Message = "Money deducted successfully",
                    RemainingInGameMoney = profile.InGameMoney,
                    RemainingRealMoney = profile.RealMoney,
                    TotalRemainingMoney = profile.InGameMoney + profile.RealMoney,
                    AmountDeducted = request.Amount,
                    AmountFromInGameMoney = amountFromInGameMoney,
                    AmountFromRealMoney = amountFromRealMoney,
                    TransactionIds = transactionIds
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error deducting money for user {request.UserId}");
                
                return new MoneyTransactionResultDto
                {
                    Success = false,
                    Message = "An error occurred while processing the transaction"
                };
            }
        }

        public async Task<MoneyAddResultDto> AddMoneyAsync(int userId, decimal amount, MoneyType moneyType, string transactionType, string? description = null, string? gameType = null, string? gameId = null, string? referenceId = null)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (profile == null)
                {
                    return new MoneyAddResultDto
                    {
                        Success = false,
                        Message = "User profile not found"
                    };
                }

                if (moneyType == MoneyType.InGameMoney)
                {
                    profile.InGameMoney += amount;
                }
                else
                {
                    profile.RealMoney += amount;
                }

                // Log transaction
                var moneyTransaction = new MoneyTransaction
                {
                    UserId = userId,
                    Amount = amount,
                    TransactionDirection = "CREDIT",
                    MoneyType = moneyType.ToString(),
                    TransactionType = transactionType,
                    Description = description,
                    GameType = gameType,
                    GameId = gameId,
                    ReferenceId = referenceId,
                    BalanceAfter = profile.InGameMoney + profile.RealMoney,
                    InGameMoneyAfter = profile.InGameMoney,
                    RealMoneyAfter = profile.RealMoney,
                    CreatedAt = DateTime.UtcNow
                };

                _context.MoneyTransactions.Add(moneyTransaction);
                profile.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                _logger.LogInformation($"Money added successfully for user {userId}. " +
                    $"Amount: {amount:C}, Type: {moneyType}, Transaction: {transactionType}");

                return new MoneyAddResultDto
                {
                    Success = true,
                    Message = "Money added successfully",
                    NewInGameMoney = profile.InGameMoney,
                    NewRealMoney = profile.RealMoney,
                    TotalMoney = profile.InGameMoney + profile.RealMoney,
                    AmountAdded = amount,
                    TransactionId = moneyTransaction.Id
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, $"Error adding money for user {userId}");
                
                return new MoneyAddResultDto
                {
                    Success = false,
                    Message = "An error occurred while processing the transaction"
                };
            }
        }

        public async Task<GetUserMoneyDto?> GetUserMoneyAsync(int userId)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (profile == null)
                    return null;

                return new GetUserMoneyDto
                {
                    InGameMoney = profile.InGameMoney,
                    RealMoney = profile.RealMoney,
                    TotalMoney = profile.InGameMoney + profile.RealMoney
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting money balance for user {userId}");
                return null;
            }
        }

        public async Task<bool> HasSufficientFundsAsync(int userId, decimal amount)
        {
            try
            {
                var profile = await _context.UserProfiles
                    .FirstOrDefaultAsync(up => up.UserId == userId);

                if (profile == null)
                    return false;

                return (profile.InGameMoney + profile.RealMoney) >= amount;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking sufficient funds for user {userId}");
                return false;
            }
        }

        public async Task<MoneyTransactionResultDto> ProcessGameEntryAsync(int userId, decimal entryFee, string gameType, string? gameId = null)
        {
            var referenceId = Guid.NewGuid().ToString("N")[..10]; // Short reference ID for linking transactions
            
            var request = new MoneyTransactionRequestDto
            {
                UserId = userId,
                Amount = entryFee,
                TransactionType = "game_entry",
                Description = $"Entry fee for {gameType} game",
                GameType = gameType,
                GameId = gameId,
                ReferenceId = referenceId
            };

            return await DeductMoneyAsync(request);
        }

        public async Task<MoneyAddResultDto> ProcessGameWinAsync(int userId, decimal winAmount, string gameType, string? gameId = null)
        {
            var referenceId = Guid.NewGuid().ToString("N")[..10]; // Short reference ID for linking transactions
            
            return await AddMoneyAsync(userId, winAmount, MoneyType.RealMoney, "game_win", 
                $"Win amount for {gameType} game", gameType, gameId, referenceId);
        }

        public async Task<List<MoneyTransactionHistoryDto>> GetTransactionHistoryAsync(int userId, int page = 1, int pageSize = 20)
        {
            try
            {
                return await _context.MoneyTransactions
                    .Where(t => t.UserId == userId)
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new MoneyTransactionHistoryDto
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        TransactionDirection = t.TransactionDirection,
                        MoneyType = t.MoneyType,
                        TransactionType = t.TransactionType,
                        Description = t.Description,
                        GameType = t.GameType,
                        GameId = t.GameId,
                        BalanceAfter = t.BalanceAfter,
                        InGameMoneyAfter = t.InGameMoneyAfter,
                        RealMoneyAfter = t.RealMoneyAfter,
                        CreatedAt = t.CreatedAt,
                        ReferenceId = t.ReferenceId
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting transaction history for user {userId}");
                return new List<MoneyTransactionHistoryDto>();
            }
        }

        public async Task<List<MoneyTransactionHistoryDto>> GetFilteredTransactionsAsync(TransactionFilterDto filter)
        {
            try
            {
                var query = _context.MoneyTransactions.AsQueryable();

                if (filter.UserId.HasValue)
                    query = query.Where(t => t.UserId == filter.UserId.Value);

                if (!string.IsNullOrEmpty(filter.TransactionDirection))
                    query = query.Where(t => t.TransactionDirection == filter.TransactionDirection);

                if (!string.IsNullOrEmpty(filter.MoneyType))
                    query = query.Where(t => t.MoneyType == filter.MoneyType);

                if (!string.IsNullOrEmpty(filter.TransactionType))
                    query = query.Where(t => t.TransactionType == filter.TransactionType);

                if (!string.IsNullOrEmpty(filter.GameType))
                    query = query.Where(t => t.GameType == filter.GameType);

                if (filter.FromDate.HasValue)
                    query = query.Where(t => t.CreatedAt >= filter.FromDate.Value);

                if (filter.ToDate.HasValue)
                    query = query.Where(t => t.CreatedAt <= filter.ToDate.Value);

                return await query
                    .OrderByDescending(t => t.CreatedAt)
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .Select(t => new MoneyTransactionHistoryDto
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        TransactionDirection = t.TransactionDirection,
                        MoneyType = t.MoneyType,
                        TransactionType = t.TransactionType,
                        Description = t.Description,
                        GameType = t.GameType,
                        GameId = t.GameId,
                        BalanceAfter = t.BalanceAfter,
                        InGameMoneyAfter = t.InGameMoneyAfter,
                        RealMoneyAfter = t.RealMoneyAfter,
                        CreatedAt = t.CreatedAt,
                        ReferenceId = t.ReferenceId
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered transactions");
                return new List<MoneyTransactionHistoryDto>();
            }
        }

        public async Task<TransactionSummaryDto> GetTransactionSummaryAsync(int userId, DateTime? fromDate = null, DateTime? toDate = null)
        {
            try
            {
                var query = _context.MoneyTransactions.Where(t => t.UserId == userId);

                if (fromDate.HasValue)
                    query = query.Where(t => t.CreatedAt >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(t => t.CreatedAt <= toDate.Value);

                var transactions = await query.ToListAsync();
                
                var totalCredits = transactions.Where(t => t.TransactionDirection == "CREDIT").Sum(t => t.Amount);
                var totalDebits = transactions.Where(t => t.TransactionDirection == "DEBIT").Sum(t => t.Amount);

                var currentMoney = await GetUserMoneyAsync(userId);

                return new TransactionSummaryDto
                {
                    TotalCredits = totalCredits,
                    TotalDebits = totalDebits,
                    NetAmount = totalCredits - totalDebits,
                    TotalTransactions = transactions.Count,
                    CurrentInGameMoney = currentMoney?.InGameMoney ?? 0,
                    CurrentRealMoney = currentMoney?.RealMoney ?? 0,
                    CurrentTotalMoney = currentMoney?.TotalMoney ?? 0
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting transaction summary for user {userId}");
                return new TransactionSummaryDto();
            }
        }

        public async Task<MoneyTransactionHistoryDto?> GetTransactionByIdAsync(int transactionId)
        {
            try
            {
                return await _context.MoneyTransactions
                    .Where(t => t.Id == transactionId)
                    .Select(t => new MoneyTransactionHistoryDto
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        TransactionDirection = t.TransactionDirection,
                        MoneyType = t.MoneyType,
                        TransactionType = t.TransactionType,
                        Description = t.Description,
                        GameType = t.GameType,
                        GameId = t.GameId,
                        BalanceAfter = t.BalanceAfter,
                        InGameMoneyAfter = t.InGameMoneyAfter,
                        RealMoneyAfter = t.RealMoneyAfter,
                        CreatedAt = t.CreatedAt,
                        ReferenceId = t.ReferenceId
                    })
                    .FirstOrDefaultAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting transaction {transactionId}");
                return null;
            }
        }

        public async Task<List<MoneyTransactionHistoryDto>> GetTransactionsByReferenceIdAsync(string referenceId)
        {
            try
            {
                return await _context.MoneyTransactions
                    .Where(t => t.ReferenceId == referenceId)
                    .OrderBy(t => t.CreatedAt)
                    .Select(t => new MoneyTransactionHistoryDto
                    {
                        Id = t.Id,
                        Amount = t.Amount,
                        TransactionDirection = t.TransactionDirection,
                        MoneyType = t.MoneyType,
                        TransactionType = t.TransactionType,
                        Description = t.Description,
                        GameType = t.GameType,
                        GameId = t.GameId,
                        BalanceAfter = t.BalanceAfter,
                        InGameMoneyAfter = t.InGameMoneyAfter,
                        RealMoneyAfter = t.RealMoneyAfter,
                        CreatedAt = t.CreatedAt,
                        ReferenceId = t.ReferenceId
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting transactions by reference ID {referenceId}");
                return new List<MoneyTransactionHistoryDto>();
            }
        }
    }
}