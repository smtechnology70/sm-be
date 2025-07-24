using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SM_BE.Services;
using System.Security.Claims;
using sm_be.Services.lottery;

namespace sm_be.Controllers.lottery
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DailyNumberController : ControllerBase
    {
        private readonly IDailyNumberService _dailyNumberService;
        private readonly ILogger<DailyNumberController> _logger;

        public DailyNumberController(IDailyNumberService dailyNumberService, ILogger<DailyNumberController> logger)
        {
            _dailyNumberService = dailyNumberService;
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

                var todaysGame = await _dailyNumberService.GetTodaysNumberAsync();
                var playerEntry = await _dailyNumberService.GetPlayerTodaysEntryAsync(userId.Value);

                var response = new
                {
                    Date = todaysGame?.Date ?? DateTime.UtcNow.Date,
                    HasPlayerEntered = playerEntry != null,
                    PlayerGuess = playerEntry?.GuessedNumber,
                    PlayerIsWinner = playerEntry?.IsWinner ?? false,
                    PlayerEntryTime = playerEntry?.EntryTime,
                    TotalEntries = todaysGame?.PlayerEntries.Count ?? 0,
                    TotalWinners = todaysGame?.PlayerEntries.Count(pe => pe.IsWinner) ?? 0,
                    TimeRemaining = GetTimeRemainingToday()
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting today's game");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpPost("submit-guess")]
        public async Task<IActionResult> SubmitGuess([FromBody] SubmitGuessRequest request)
        {
            try
            {
                var userId = GetUserIdFromClaims();
                if (userId == null)
                    return Unauthorized("Invalid user token");

                if (request.GuessedNumber < 0 || request.GuessedNumber > 99)
                    return BadRequest("Number must be between 0 and 99");

                var playerEntry = await _dailyNumberService.SubmitPlayerGuessAsync(userId.Value, request.GuessedNumber);

                return Ok(new
                {
                    playerEntry.GuessedNumber,
                    playerEntry.IsWinner,
                    playerEntry.EntryTime,
                    Message = playerEntry.IsWinner ? "Congratulations! You guessed correctly!" : "Guess submitted successfully!"
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error submitting guess");
                return StatusCode(500, "Internal server error");
            }
        }

        [HttpGet("winners/today")]
        public async Task<IActionResult> GetTodaysWinners()
        {
            try
            {
                var winners = await _dailyNumberService.GetTodaysWinnersAsync();
                
                var response = winners.Select(w => new
                {
                    w.User.Username,
                    w.GuessedNumber,
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

        [HttpGet("recent-games")]
        public async Task<IActionResult> GetRecentGames([FromQuery] int count = 10)
        {
            try
            {
                var recentGames = await _dailyNumberService.GetRecentGamesAsync(count);
                
                var response = recentGames.Select(g => new
                {
                    g.Date,
                    g.WinningNumber,
                    TotalEntries = g.PlayerEntries.Count,
                    TotalWinners = g.PlayerEntries.Count(pe => pe.IsWinner),
                    Winners = g.PlayerEntries.Where(pe => pe.IsWinner).Select(w => new
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

    public class SubmitGuessRequest
    {
        public int GuessedNumber { get; set; }
    }
}