using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using SM_BE.Services;
using SM_BE.Models;
using sm_be.Services.lottery;

namespace SM_BE.Hubs
{
    [Authorize]
    public class DailyNumberHub : Hub
    {
        private readonly IDailyNumberService _dailyNumberService;
        private readonly IJwtService _jwtService;
        private readonly ILogger<DailyNumberHub> _logger;
        private static readonly Dictionary<string, int> _playerConnections = new(); // ConnectionId -> PlayerId

        public DailyNumberHub(IDailyNumberService dailyNumberService, IJwtService jwtService, ILogger<DailyNumberHub> logger)
        {
            _dailyNumberService = dailyNumberService;
            _jwtService = jwtService;
            _logger = logger;
        }

        // Join the daily game room
        public async Task JoinDailyGame()
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
                await Groups.AddToGroupAsync(Context.ConnectionId, "DailyNumberGame");

                // Send current game state
                await SendGameStateToPlayer(playerId.Value);

                _logger.LogInformation($"Player {playerId} joined daily number game");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in JoinDailyGame");
                await Clients.Caller.SendAsync("Error", $"Failed to join daily game: {ex.Message}");
            }
        }

        // Submit a number guess
        public async Task SubmitGuess(int guessedNumber)
        {
            try
            {
                var playerId = GetPlayerIdFromToken();
                if (playerId == null)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid or missing authentication token");
                    return;
                }

                if (guessedNumber < 0 || guessedNumber > 99)
                {
                    await Clients.Caller.SendAsync("Error", "Number must be between 0 and 99");
                    return;
                }

                // Check if player already submitted today
                var hasEntered = await _dailyNumberService.HasPlayerEnteredTodayAsync(playerId.Value);
                if (hasEntered)
                {
                    await Clients.Caller.SendAsync("Error", "You have already submitted your guess for today");
                    return;
                }

                // Submit the guess
                var playerEntry = await _dailyNumberService.SubmitPlayerGuessAsync(playerId.Value, guessedNumber);

                if (playerEntry != null)
                {
                    // Notify the player of successful submission
                    await Clients.Caller.SendAsync("GuessSubmitted", new
                    {
                        GuessedNumber = playerEntry.GuessedNumber,
                        IsWinner = playerEntry.IsWinner,
                        EntryTime = playerEntry.EntryTime,
                        Message =  "Guess submitted successfully!"
                    });

                    // Update game statistics for all players
                    await BroadcastGameStatistics();

                    _logger.LogInformation($"Player {playerId} submitted guess {guessedNumber}.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in SubmitGuess");
                await Clients.Caller.SendAsync("Error", $"Failed to submit guess: {ex.Message}");
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
                var winners = await _dailyNumberService.GetTodaysWinnersAsync();
                
                await Clients.Caller.SendAsync("TodaysWinners", winners.Select(w => new
                {
                    Username = w.User.Username,
                    GuessedNumber = w.GuessedNumber,
                    EntryTime = w.EntryTime
                }).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetTodaysWinners");
                await Clients.Caller.SendAsync("Error", $"Failed to get winners: {ex.Message}");
            }
        }

        private async Task SendGameStateToPlayer(int playerId)
        {
            var todaysGame = await _dailyNumberService.GetTodaysNumberAsync();
            var playerEntry = await _dailyNumberService.GetPlayerTodaysEntryAsync(playerId);

            var gameState = new
            {
                Date = todaysGame?.Date ?? DateTime.UtcNow.Date,
                HasPlayerEntered = playerEntry != null,
                PlayerGuess = playerEntry?.GuessedNumber,
                PlayerIsWinner = playerEntry?.IsWinner ?? false,
                PlayerEntryTime = playerEntry?.EntryTime,
                TotalEntries = todaysGame?.PlayerEntries.Count ?? 0,
                TotalWinners = todaysGame?.PlayerEntries.Count(pe => pe.IsWinner) ?? 0,
                GameActive = true,
                TimeRemaining = GetTimeRemainingToday()
            };

            await Clients.Caller.SendAsync("GameState", gameState);
        }

        private async Task BroadcastGameStatistics()
        {
            var todaysGame = await _dailyNumberService.GetTodaysNumberAsync();

            if (todaysGame != null)
            {
                var stats = new
                {
                    TotalEntries = todaysGame.PlayerEntries.Count,
                    TotalWinners = todaysGame.PlayerEntries.Count(pe => pe.IsWinner),
                    Date = todaysGame.Date
                };

                await Clients.Group("DailyNumberGame").SendAsync("GameStatistics", stats);
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
            _logger.LogInformation($"Daily number game client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            _logger.LogInformation($"Daily number game client disconnected: {Context.ConnectionId}");
            
            if (_playerConnections.TryGetValue(Context.ConnectionId, out var playerId))
            {
                _playerConnections.Remove(Context.ConnectionId);
                _logger.LogInformation($"Player {playerId} removed from daily number game connections");
            }

            if (exception != null)
            {
                _logger.LogError(exception, "Disconnect exception in daily number game");
            }
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}