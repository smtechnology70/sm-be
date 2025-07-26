using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sm_be.Services.lottery;
using sm_be.Services.MinimumNumberCount;
using SM_BE.Models;
using SM_BE.Services;
using System.Security.Claims;

namespace sm_be.Controllers.MinimumNumberCount
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DailyDigitGameController : ControllerBase
    {
        private readonly IDailyDigitGameService _dailyDigitGameService;
        private readonly ILogger<DailyDigitGameController> _logger;

        public DailyDigitGameController(IDailyDigitGameService dailyDigitGameService, ILogger<DailyDigitGameController> logger)
        {
            _dailyDigitGameService = dailyDigitGameService;
            _logger = logger;
        }

        [HttpGet("today")]
        public async Task<IActionResult> GetTodaysGame()
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized("Invalid user token");

                var todaysGame = await _dailyDigitGameService.GetTodaysGameAsync();
                var playerEntry = await _dailyDigitGameService.GetPlayerTodaysEntryAsync(userId.Value);
                var digitCounts = await _dailyDigitGameService.GetTodaysDigitCountsAsync();

                var response = new
                {
                    Date = todaysGame?.Date ?? DateTime.UtcNow.Date,
                    HasPlayerEntered = playerEntry != null,
                    PlayerSelectedDigit = playerEntry?.SelectedDigit,
                    PlayerEntryTime = playerEntry?.EntryTime,
                    IsGameCompleted = todaysGame?.IsCompleted ?? false,
                    todaysGame?.WinningDigit,
                    PlayerIsWinner = playerEntry?.IsWinner ?? false,
                    TotalEntries = todaysGame?.PlayerDigitEntries.Count ?? 0,
                    TotalWinners = todaysGame?.PlayerDigitEntries.Count(pde => pde.IsWinner) ?? 0,
                    DigitCounts = digitCounts,
                    TimeRemaining = GetTimeRemainingToday()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's digit game");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("submit-digit")]
        public async Task<IActionResult> SubmitDigit([FromBody] SubmitDigitRequest request)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized("Invalid user token");

                if (request.SelectedDigit < 0 || request.SelectedDigit > 9)
                    return BadRequest(new { 
                        message = "Digit must be between 0 and 9",
                        code = "INVALID_DIGIT_RANGE"
                    });

                // Check if player already has an entry for today
                var hasEntered = await _dailyDigitGameService.HasPlayerEnteredTodayAsync(userId.Value);
                if (hasEntered)
                {
                    var existingEntry = await _dailyDigitGameService.GetPlayerTodaysEntryAsync(userId.Value);
                    return BadRequest(new { 
                        message = "You have already selected your digit for today",
                        code = "ALREADY_SELECTED",
                        selectedDigit = existingEntry?.SelectedDigit,
                        selectionTime = existingEntry?.EntryTime
                    });
                }

                var playerEntry = await _dailyDigitGameService.SubmitPlayerDigitAsync(userId.Value, request.SelectedDigit);

                return Ok(new
                {
                    selectedDigit = playerEntry.SelectedDigit,
                    entryTime = playerEntry.EntryTime,
                    message = "Digit selected successfully! Results will be announced at the end of the day.",
                    code = "SUCCESS"
                });
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("already selected"))
                {
                    return BadRequest(new { 
                        message = ex.Message,
                        code = "ALREADY_SELECTED"
                    });
                }
                return BadRequest(new { 
                    message = ex.Message,
                    code = "INVALID_OPERATION"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting digit for user");
                return StatusCode(500, new { 
                    message = "Internal server error",
                    code = "SERVER_ERROR"
                });
            }
        }

        [HttpGet("winners/today")]
        public async Task<IActionResult> GetTodaysWinners()
        {
            try
            {
                var winners = await _dailyDigitGameService.GetTodaysWinnersAsync();
                
                var response = winners.Select(w => new
                {
                    w.User.Username,
                    w.SelectedDigit,
                    w.EntryTime
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's winners");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("digit-counts")]
        public async Task<IActionResult> GetDigitCounts()
        {
            try
            {
                var digitCounts = await _dailyDigitGameService.GetTodaysDigitCountsAsync();
                return Ok(digitCounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting digit counts");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("recent-games")]
        public async Task<IActionResult> GetRecentGames([FromQuery] int count = 10)
        {
            try
            {
                var recentGames = await _dailyDigitGameService.GetRecentGamesAsync(count);
                
                var response = recentGames.Select(g => new
                {
                    g.Date,
                    g.WinningDigit,
                    g.IsCompleted,
                    TotalEntries = g.PlayerDigitEntries.Count,
                    TotalWinners = g.PlayerDigitEntries.Count(pde => pde.IsWinner),
                    Winners = g.PlayerDigitEntries.Where(pde => pde.IsWinner).Select(w => new
                    {
                        w.User.Username,
                        w.EntryTime
                    }).ToList()
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent games");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("my-selection")]
        public async Task<IActionResult> GetMySelection()
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized("Invalid user token");

                var playerEntry = await _dailyDigitGameService.GetPlayerTodaysEntryAsync(userId.Value);
                
                if (playerEntry == null)
                {
                    return Ok(new
                    {
                        hasSelected = false,
                        selectedDigit = (int?)null,
                        entryTime = (DateTime?)null
                    });
                }

                return Ok(new
                {
                    hasSelected = true,
                    selectedDigit = playerEntry.SelectedDigit,
                    entryTime = playerEntry.EntryTime,
                    isWinner = playerEntry.IsWinner
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user selection");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("process-winners")]
        public async Task<IActionResult> ProcessWinnersManually()
        {
            try
            {
                _logger.LogInformation("Manual winner processing triggered");
                await _dailyDigitGameService.ProcessDailyDigitWinnersAsync();
                
                return Ok(new { 
                    message = "Winners processed successfully",
                    timestamp = DateTime.UtcNow 
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error manually processing winners");
                return StatusCode(500, new { 
                    message = "Error processing winners",
                    error = ex.Message 
                });
            }
        }

        private int? GetUserIdFromClaims()
        {
            var userIdClaim = User.FindFirst("userId");
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return null;
        }

        private TimeSpan GetTimeRemainingToday()
        {
            var now = DateTime.UtcNow;
            var endOfDay = now.Date.AddDays(1);
            return endOfDay - now;
        }
    }

    public class SubmitDigitRequest
    {
        public int SelectedDigit { get; set; }
    }
}