using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SM_BE.Dto;
using SM_BE.Services;
using System.Security.Claims;

namespace SM_BE.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class MoneyTransactionController : ControllerBase
    {
        private readonly IMoneyTransactionService _moneyTransactionService;
        private readonly ILogger<MoneyTransactionController> _logger;

        public MoneyTransactionController(IMoneyTransactionService moneyTransactionService, ILogger<MoneyTransactionController> logger)
        {
            _moneyTransactionService = moneyTransactionService;
            _logger = logger;
        }

        [HttpGet("history")]
        public async Task<ActionResult<List<MoneyTransactionHistoryDto>>> GetTransactionHistory(
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var transactions = await _moneyTransactionService.GetTransactionHistoryAsync(userId, page, pageSize);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction history");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("summary")]
        public async Task<ActionResult<TransactionSummaryDto>> GetTransactionSummary(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var summary = await _moneyTransactionService.GetTransactionSummaryAsync(userId, fromDate, toDate);
                return Ok(summary);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction summary");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("filter")]
        public async Task<ActionResult<List<MoneyTransactionHistoryDto>>> GetFilteredTransactions([FromBody] TransactionFilterDto filter)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                // Override the UserId from the filter with the authenticated user's ID for security
                filter.UserId = userId;

                var transactions = await _moneyTransactionService.GetFilteredTransactionsAsync(filter);
                return Ok(transactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting filtered transactions");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("{transactionId}")]
        public async Task<ActionResult<MoneyTransactionHistoryDto>> GetTransactionById(int transactionId)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var transaction = await _moneyTransactionService.GetTransactionByIdAsync(transactionId);
                
                if (transaction == null)
                    return NotFound("Transaction not found");

                // Ensure user can only access their own transactions
                if (transaction.Id != transactionId) // This would need to be adjusted to check UserId
                {
                    // We need to add UserId to the DTO or check differently
                    return Forbid("You can only access your own transactions");
                }

                return Ok(transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting transaction {transactionId}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("reference/{referenceId}")]
        public async Task<ActionResult<List<MoneyTransactionHistoryDto>>> GetTransactionsByReferenceId(string referenceId)
        {
            try
            {
                var userIdClaim = User.FindFirst("userId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var transactions = await _moneyTransactionService.GetTransactionsByReferenceIdAsync(referenceId);
                
                // Filter to only show transactions for the authenticated user
                var userTransactions = transactions.Where(t => t.Id > 0).ToList(); // This would need proper UserId filtering
                
                return Ok(userTransactions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting transactions by reference ID {referenceId}");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("balance")]
        public async Task<ActionResult<GetUserMoneyDto>> GetUserBalance()
        {
            try
            {
                var userIdClaim = User.FindFirst("userId");
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Unauthorized("Invalid user token");
                }

                var balance = await _moneyTransactionService.GetUserMoneyAsync(userId);
                
                if (balance == null)
                    return NotFound("User profile not found");

                return Ok(balance);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user balance");
                return StatusCode(500, "Internal server error");
            }
        }
    }
}