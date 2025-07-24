using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using SM_BE.Services;
using SM_BE.Models;
using sm_be.Services.lottery;
using sm_be.Services.MinimumNumberCount;

namespace sm_be.MinimumNumberCount
{
    [Authorize]
    public class DailyDigitGameHub : Hub
    {
        private readonly IDailyDigitGameService _dailyDigitGameService;
        private readonly IJwtService _jwtService;
        private readonly ILogger<DailyDigitGameHub> _logger;
        private static readonly Dictionary<string, int> _playerConnections = new(); // ConnectionId -> PlayerId

        public DailyDigitGameHub(IDailyDigitGameService dailyDigitGameService, IJwtService jwtService, ILogger<DailyDigitGameHub> logger)
        {
            _dailyDigitGameService = dailyDigitGameService;
            _jwtService = jwtService;
            _logger = logger;
        }

        // Join the daily digit game room
        public async Task JoinDailyDigitGame()
        {
            try
            {
                var playerId = GetPlayerIdFromToken();
                if (playerId == null)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid or missing authentication token");
                    return;
                }

                _playerConnections[Context.ConnectionId] = playerId.Value;
                await Groups.AddToGroupAsync(Context.ConnectionId, "DailyDigitGame");

                // Send current game state
                await SendGameStateToPlayer(playerId.Value);

                _logger.LogInformation($"Player {playerId} joined daily digit game");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JoinDailyDigitGame");
                await Clients.Caller.SendAsync("Error", $"Failed to join daily digit game: {ex.Message}");
            }
        }

        // Submit a digit selection
        public async Task SubmitDigit(int selectedDigit)
        {
            try
            {
                var playerId = GetPlayerIdFromToken();
                if (playerId == null)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid or missing authentication token");
                    return;
                }

                if (selectedDigit < 0 || selectedDigit > 9)
                {
                    await Clients.Caller.SendAsync("Error", "Digit must be between 0 and 9");
                    return;
                }

                // Check if player already submitted today
                var hasEntered = await _dailyDigitGameService.HasPlayerEnteredTodayAsync(playerId.Value);
                if (hasEntered)
                {
                    await Clients.Caller.SendAsync("Error", "You have already selected your digit for today");
                    return;
                }

                // Submit the digit
                var playerEntry = await _dailyDigitGameService.SubmitPlayerDigitAsync(playerId.Value, selectedDigit);

                if (playerEntry != null)
                {
                    // Notify the player of successful submission
                    await Clients.Caller.SendAsync("DigitSubmitted", new
                    {
                        playerEntry.SelectedDigit,
                        playerEntry.EntryTime,
                        Message = "Digit selected successfully! Results will be announced at the end of the day."
                    });

                    // Update game statistics for all players
                    await BroadcastGameStatistics();

                    _logger.LogInformation($"Player {playerId} submitted digit {selectedDigit}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitDigit");
                await Clients.Caller.SendAsync("Error", $"Failed to submit digit: {ex.Message}");
            }
        }

        // Get current game state
        public async Task GetGameState()
        {
            try
            {
                var playerId = GetPlayerIdFromToken();
                if (playerId == null)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid or missing authentication token");
                    return;
                }

                await SendGameStateToPlayer(playerId.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetGameState");
                await Clients.Caller.SendAsync("Error", $"Failed to get game state: {ex.Message}");
            }
        }

        // Get today's winners
        public async Task GetTodaysWinners()
        {
            try
            {
                var winners = await _dailyDigitGameService.GetTodaysWinnersAsync();
                
                await Clients.Caller.SendAsync("TodaysWinners", winners.Select(w => new
                {
                    w.User.Username,
                    w.SelectedDigit,
                    w.EntryTime
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTodaysWinners");
                await Clients.Caller.SendAsync("Error", $"Failed to get winners: {ex.Message}");
            }
        }

        // Get digit counts
        public async Task GetDigitCounts()
        {
            try
            {
                var digitCounts = await _dailyDigitGameService.GetTodaysDigitCountsAsync();
                await Clients.Caller.SendAsync("DigitCounts", digitCounts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetDigitCounts");
                await Clients.Caller.SendAsync("Error", $"Failed to get digit counts: {ex.Message}");
            }
        }

        private async Task SendGameStateToPlayer(int playerId)
        {
            var todaysGame = await _dailyDigitGameService.GetTodaysGameAsync();
            var playerEntry = await _dailyDigitGameService.GetPlayerTodaysEntryAsync(playerId);
            var digitCounts = await _dailyDigitGameService.GetTodaysDigitCountsAsync();

            var gameState = new
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

            await Clients.Caller.SendAsync("GameState", gameState);
        }

        private async Task BroadcastGameStatistics()
        {
            var todaysGame = await _dailyDigitGameService.GetTodaysGameAsync();
            var digitCounts = await _dailyDigitGameService.GetTodaysDigitCountsAsync();

            if (todaysGame != null)
            {
                var stats = new
                {
                    TotalEntries = todaysGame.PlayerDigitEntries.Count,
                    todaysGame.Date,
                    DigitCounts = digitCounts
                };

                await Clients.Group("DailyDigitGame").SendAsync("GameStatistics", stats);
            }
        }

        private TimeSpan GetTimeRemainingToday()
        {
            var now = DateTime.UtcNow;
            var endOfDay = now.Date.AddDays(1);
            return endOfDay - now;
        }

        private int? GetPlayerIdFromToken()
        {
            try
            {
                var userIdClaim = Context.User?.FindFirst("userId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    return userId;
                }

                var accessToken = Context.GetHttpContext()?.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    var isValid = _jwtService.ValidateToken(accessToken);
                    if (isValid)
                    {
                        var userIdFromToken = _jwtService.GetUserIdFromToken(accessToken);
                        if (userIdFromToken.HasValue)
                        {
                            return userIdFromToken;
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting user ID from token");
                return null;
            }
        }

        public override async Task OnConnectedAsync()
        {
            _logger.LogInformation($"Daily digit game client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"Daily digit game client disconnected: {Context.ConnectionId}");
            
            if (_playerConnections.TryGetValue(Context.ConnectionId, out var playerId))
            {
                _playerConnections.Remove(Context.ConnectionId);
                _logger.LogInformation($"Player {playerId} removed from daily digit game connections");
            }

            if (exception != null)
            {
                _logger.LogError(exception, "Disconnect exception in daily digit game");
            }
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}