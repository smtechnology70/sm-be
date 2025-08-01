using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using SM_BE.Services;
using SM_BE.Dto;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using System.Text.Json;

namespace SM_BE.Hubs
{
    [Authorize]
    public class SingleGameHub : Hub
    {
        private static readonly Dictionary<string, SingleGameState> _games = new();
        private static readonly Dictionary<string, GameMoneyInfo> _gameMoneyInfo = new(); // Track money deduction details
        private static readonly Dictionary<string, int> _playerConnections = new(); // ConnectionId -> PlayerId
        private readonly IJwtService _jwtService;
        private readonly IGameService _gameService;
        private readonly IMoneyTransactionService _moneyTransactionService;
        private const decimal GAME_ENTRY_FEE = 50m;

        public SingleGameHub(IJwtService jwtService, IGameService gameService, IMoneyTransactionService moneyTransactionService)
        {
            _jwtService = jwtService;
            _gameService = gameService;
            _moneyTransactionService = moneyTransactionService;
        }

        // Start a new single-player game
        public async Task StartNewGame()
        {
            try
            {
                Console.WriteLine("StartNewGame called");

                var playerId = GetPlayerIdFromToken();
                if (playerId == null)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid or missing authentication token");
                    return;
                }

                Console.WriteLine($"Player {playerId} starting new single game");

                // Check if player has sufficient funds
                var hasSufficientFunds = await _moneyTransactionService.HasSufficientFundsAsync(playerId.Value, GAME_ENTRY_FEE);
                if (!hasSufficientFunds)
                {
                    var userMoney = await _moneyTransactionService.GetUserMoneyAsync(playerId.Value);
                    await Clients.Caller.SendAsync("InsufficientFunds", 
                        $"Insufficient funds to start the game. Required: ${GAME_ENTRY_FEE}, Available: ${userMoney?.TotalMoney ?? 0}");
                    return;
                }

                // Create unique game ID
                var gameId = Guid.NewGuid().ToString();

                // Deduct entry fee using the existing service that handles in_game_money first, then real_money
                var deductionResult = await _moneyTransactionService.ProcessGameEntryAsync(playerId.Value, GAME_ENTRY_FEE, "single-number", gameId);
                if (!deductionResult.Success)
                {
                    await Clients.Caller.SendAsync("Error", $"Failed to process entry fee: {deductionResult.Message}");
                    return;
                }

                // Store money deduction details for proper refund later
                _gameMoneyInfo[gameId] = new GameMoneyInfo
                {
                    PlayerId = playerId.Value,
                    TotalDeducted = GAME_ENTRY_FEE,
                    FromInGameMoney = deductionResult.AmountFromInGameMoney,
                    FromRealMoney = deductionResult.AmountFromRealMoney,
                    TransactionIds = deductionResult.TransactionIds
                };

                // Create new game state
                var gameState = SingleGameState.CreateNew(playerId.Value);
                _games[gameId] = gameState;
                _playerConnections[Context.ConnectionId] = playerId.Value;

                // Record game in database
                await _gameService.CreateGameRecordAsync(gameId, "single-number", playerId.Value, playerId.Value, GAME_ENTRY_FEE);

                // Save initial game data as JSON
                var initialGameData = new
                {
                    BoxCount = gameState.Boxes.Length,
                    MaxBoxes = gameState.MaxBoxes,
                    InitialBoxValues = gameState.Boxes.Select(b => b.Value).ToArray(),
                    EntryFee = GAME_ENTRY_FEE,
                    MoneyDeduction = new
                    {
                        TotalDeducted = GAME_ENTRY_FEE,
                        FromInGameMoney = deductionResult.AmountFromInGameMoney,
                        FromRealMoney = deductionResult.AmountFromRealMoney
                    }
                };
                await _gameService.SetGameDataAsync(gameId, JsonSerializer.Serialize(initialGameData));

                // Add player to SignalR group
                await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

                // Notify player that game started and about the deduction
                await Clients.Caller.SendAsync("GameStarted", gameId);
                await Clients.Caller.SendAsync("MoneyDeducted", new
                {
                    Amount = GAME_ENTRY_FEE,
                    FromInGameMoney = deductionResult.AmountFromInGameMoney,
                    FromRealMoney = deductionResult.AmountFromRealMoney,
                    RemainingMoney = deductionResult.TotalRemainingMoney,
                    Message = "Entry fee deducted successfully"
                });

                // Send initial game state
                await BroadcastGameState(gameId);

                Console.WriteLine($"Created single game {gameId} for player {playerId}, entry fee ${GAME_ENTRY_FEE} deducted (InGame: ${deductionResult.AmountFromInGameMoney}, Real: ${deductionResult.AmountFromRealMoney})");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in StartNewGame: {ex}");
                await Clients.Caller.SendAsync("Error", $"Failed to start new game: {ex.Message}");
            }
        }

        // Player makes a move by revealing a box
        public async Task RevealBox(string gameId, int index)
        {
            try
            {
                Console.WriteLine($"RevealBox called with gameId: {gameId}, index: {index}");

                var playerId = GetPlayerIdFromToken();
                if (playerId == null)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid or missing authentication token");
                    return;
                }

                if (index < 0)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid box index");
                    return;
                }

                if (!_games.TryGetValue(gameId, out var game))
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                if (game.PlayerId != playerId.Value)
                {
                    await Clients.Caller.SendAsync("Error", "This is not your game");
                    return;
                }

                if (game.Status != SingleGameStatus.Playing)
                {
                    await Clients.Caller.SendAsync("Error", "Game is already finished");
                    return;
                }

                if (index >= game.Boxes.Length)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid box index");
                    return;
                }

                if (game.Boxes[index].Revealed)
                {
                    await Clients.Caller.SendAsync("Error", "Box already revealed");
                    return;
                }

                // Apply the move
                game.ApplyMove(index);

                // Broadcast game state
                await BroadcastGameState(gameId);

                Console.WriteLine($"Player {playerId} revealed box {index} in game {gameId}. Current sum: {game.CurrentSum}, Status: {game.Status}");

                // If game is finished, record the result and process money
                if (game.Status != SingleGameStatus.Playing)
                {
                    await ProcessGameCompletion(gameId, game);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RevealBox: {ex}");
                await Clients.Caller.SendAsync("Error", $"Failed to reveal box: {ex.Message}");
            }
        }

        // Player can choose to stop early and take current result
        public async Task StopGame(string gameId)
        {
            try
            {
                var playerId = GetPlayerIdFromToken();
                if (playerId == null)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid or missing authentication token");
                    return;
                }

                if (!_games.TryGetValue(gameId, out var game))
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                if (game.PlayerId != playerId.Value)
                {
                    await Clients.Caller.SendAsync("Error", "This is not your game");
                    return;
                }

                if (game.Status != SingleGameStatus.Playing)
                {
                    await Clients.Caller.SendAsync("Error", "Game is already finished");
                    return;
                }

                // Stop the game early
                game.StopGameEarly();

                await BroadcastGameState(gameId);

                // Process the game completion with money logic
                await ProcessGameCompletion(gameId, game, true);

                Console.WriteLine($"Player {playerId} stopped game {gameId} early. Final sum: {game.CurrentSum}, Status: {game.Status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in StopGame: {ex}");
                await Clients.Caller.SendAsync("Error", $"Failed to stop game: {ex.Message}");
            }
        }

        private async Task ProcessGameCompletion(string gameId, SingleGameState game, bool stoppedEarly = false)
        {
            try
            {
                if (!_gameMoneyInfo.TryGetValue(gameId, out var moneyInfo))
                {
                    Console.WriteLine($"Warning: Money info not found for game {gameId}");
                    return;
                }

                // Calculate money outcome based on sum
                var moneyOutcome = CalculateMoneyOutcome(game.CurrentSum, GAME_ENTRY_FEE);
                
                // Determine winner (player wins if sum > 0)
                var winnerId = game.Status == SingleGameStatus.Won ? game.PlayerId : (int?)null;
                var status = game.Status == SingleGameStatus.Won ? "Won" : "Lost";

                // Update game record
                await _gameService.UpdateGameRecordAsync(gameId, winnerId, status, moneyOutcome.TotalReturnAmount);

                // Process money returns based on original deduction
                var moneyResults = new List<MoneyAddResultDto>();
                var penaltyResults = new List<MoneyTransactionResultDto>();

                // 1. Return the original entry fee to the same money types it was deducted from
                if (moneyInfo.FromInGameMoney > 0)
                {
                    var inGameReturn = await _moneyTransactionService.AddMoneyAsync(
                        game.PlayerId,
                        moneyInfo.FromInGameMoney,
                        MoneyType.InGameMoney,
                        "single_game_return_entry",
                        $"Return entry fee to InGameMoney. Sum: {game.CurrentSum}",
                        "single-number",
                        gameId);
                    
                    if (inGameReturn.Success)
                        moneyResults.Add(inGameReturn);
                }

                if (moneyInfo.FromRealMoney > 0)
                {
                    var realMoneyReturn = await _moneyTransactionService.AddMoneyAsync(
                        game.PlayerId,
                        moneyInfo.FromRealMoney,
                        MoneyType.RealMoney,
                        "single_game_return_entry",
                        $"Return entry fee to RealMoney. Sum: {game.CurrentSum}",
                        "single-number",
                        gameId);
                    
                    if (realMoneyReturn.Success)
                        moneyResults.Add(realMoneyReturn);
                }

                // 2. Handle bonus/penalty
                if (moneyOutcome.BonusAmount > 0)
                {
                    // Positive bonus: Add to real money
                    var bonusResult = await _moneyTransactionService.AddMoneyAsync(
                        game.PlayerId,
                        moneyOutcome.BonusAmount,
                        MoneyType.RealMoney,
                        "single_game_bonus",
                        $"Game bonus. Sum: {game.CurrentSum}, +{moneyOutcome.BonusPercentage:F1}% of entry fee",
                        "single-number",
                        gameId);
                    
                    if (bonusResult.Success)
                        moneyResults.Add(bonusResult);
                }
                else if (moneyOutcome.BonusAmount < 0)
                {
                    // Negative penalty: Deduct from in_game_money first, then real_money
                    var penaltyAmount = Math.Abs(moneyOutcome.BonusAmount);
                    
                    var penaltyRequest = new MoneyTransactionRequestDto
                    {
                        UserId = game.PlayerId,
                        Amount = penaltyAmount,
                        TransactionType = "single_game_penalty",
                        Description = $"Game penalty. Sum: {game.CurrentSum}, {moneyOutcome.BonusPercentage:F1}% penalty",
                        GameType = "single-number",
                        GameId = gameId
                    };

                    var penaltyResult = await _moneyTransactionService.DeductMoneyAsync(penaltyRequest);
                    if (penaltyResult.Success)
                    {
                        penaltyResults.Add(penaltyResult);
                        Console.WriteLine($"Penalty deducted: ${penaltyAmount} (InGame: ${penaltyResult.AmountFromInGameMoney}, Real: ${penaltyResult.AmountFromRealMoney})");
                    }
                    else
                    {
                        Console.WriteLine($"Failed to deduct penalty: {penaltyResult.Message}");
                        // If penalty can't be deducted, log it but continue
                    }
                }

                // Get final balance
                var finalBalance = await _moneyTransactionService.GetUserMoneyAsync(game.PlayerId);

                // Calculate net effect including penalty deductions
                var totalPenaltyFromInGame = penaltyResults.Sum(p => p.AmountFromInGameMoney);
                var totalPenaltyFromReal = penaltyResults.Sum(p => p.AmountFromRealMoney);

                // Save final game data
                var finalGameData = new
                {
                    FinalSum = game.CurrentSum,
                    RevealedBoxesCount = game.RevealedBoxesCount,
                    RemainingBoxes = game.RemainingBoxes,
                    StoppedEarly = stoppedEarly,
                    RevealedBoxes = game.Boxes.Where((box, index) => box.Revealed)
                                              .Select((box, revealedIndex) => new { Index = Array.IndexOf(game.Boxes, box), Value = box.Value })
                                              .ToArray(),
                    FinalStatus = game.Status.ToString(),
                    MoneyCalculation = new
                    {
                        InitialEntry = GAME_ENTRY_FEE,
                        CurrentSum = game.CurrentSum,
                        BonusPercentage = moneyOutcome.BonusPercentage,
                        BonusAmount = moneyOutcome.BonusAmount,
                        TotalReturnAmount = moneyOutcome.TotalReturnAmount,
                        NetGainLoss = moneyOutcome.NetGainLoss,
                        OriginalDeduction = new
                        {
                            FromInGameMoney = moneyInfo.FromInGameMoney,
                            FromRealMoney = moneyInfo.FromRealMoney
                        },
                        PenaltyDeduction = new
                        {
                            TotalPenalty = totalPenaltyFromInGame + totalPenaltyFromReal,
                            FromInGameMoney = totalPenaltyFromInGame,
                            FromRealMoney = totalPenaltyFromReal
                        }
                    }
                };

                await _gameService.SetGameDataAsync(gameId, JsonSerializer.Serialize(finalGameData));

                // Send game over event with money details
                await Clients.Group(gameId).SendAsync("GameOver", new
                {
                    GameId = gameId,
                    Status = game.Status.ToString(),
                    FinalSum = game.CurrentSum,
                    BoxesRevealed = game.RevealedBoxesCount,
                    Won = game.Status == SingleGameStatus.Won,
                    StoppedEarly = stoppedEarly,
                    EndTime = DateTime.UtcNow,
                    MoneyResult = new
                    {
                        InitialEntry = GAME_ENTRY_FEE,
                        TotalReturnAmount = moneyOutcome.TotalReturnAmount,
                        BonusAmount = moneyOutcome.BonusAmount,
                        BonusPercentage = moneyOutcome.BonusPercentage,
                        NetGainLoss = moneyOutcome.NetGainLoss,
                        NewInGameMoney = finalBalance?.InGameMoney ?? 0,
                        NewRealMoney = finalBalance?.RealMoney ?? 0,
                        NewTotalBalance = finalBalance?.TotalMoney ?? 0,
                        Message = GetMoneyResultMessage(moneyOutcome, totalPenaltyFromInGame, totalPenaltyFromReal),
                        MoneyFlow = new
                        {
                            EntryFeeReturnedToInGameMoney = moneyInfo.FromInGameMoney,
                            EntryFeeReturnedToRealMoney = moneyInfo.FromRealMoney,
                            BonusAddedToRealMoney = moneyOutcome.BonusAmount > 0 ? moneyOutcome.BonusAmount : 0,
                            PenaltyDeductedFromInGameMoney = totalPenaltyFromInGame,
                            PenaltyDeductedFromRealMoney = totalPenaltyFromReal
                        }
                    }
                });

                // Clean up money info
                _gameMoneyInfo.Remove(gameId);

                Console.WriteLine($"Single game {gameId} completed! Sum: {game.CurrentSum}, Entry returned: ${GAME_ENTRY_FEE}, Bonus/Penalty: {moneyOutcome.BonusAmount:+#.##;-#.##;0}, Net: {moneyOutcome.NetGainLoss:+#.##;-#.##;0}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error processing game completion for {gameId}: {ex}");
            }
        }

        private MoneyOutcome CalculateMoneyOutcome(int currentSum, decimal entryFee)
        {
            decimal bonusAmount = 0;
            decimal bonusPercentage = 0;
            
            if (currentSum > 0)
            {
                // Positive sum: bonus = percentage of entry fee
                bonusPercentage = Math.Abs(currentSum); // Each point = 1% bonus
                bonusAmount = entryFee * (bonusPercentage / 100m);
            }
            else if (currentSum < 0)
            {
                // Negative sum: penalty = negative percentage of entry fee
                bonusPercentage = -Math.Abs(currentSum); // Each negative point = 1% penalty
                bonusAmount = entryFee * (bonusPercentage / 100m); // This will be negative
            }
            // If currentSum == 0, bonus remains 0

            return new MoneyOutcome
            {
                BonusAmount = bonusAmount,
                BonusPercentage = bonusPercentage,
                TotalReturnAmount = entryFee + bonusAmount,
                NetGainLoss = bonusAmount // Since entry fee is returned separately
            };
        }

        private string GetMoneyResultMessage(MoneyOutcome outcome, decimal penaltyFromInGame, decimal penaltyFromReal)
        {
            if (outcome.NetGainLoss > 0)
            {
                return $"Congratulations! You earned a ${outcome.BonusAmount:F2} bonus (+{outcome.BonusPercentage:F1}%) plus your entry fee back!";
            }
            else if (outcome.NetGainLoss < 0)
            {
                var totalPenalty = penaltyFromInGame + penaltyFromReal;
                var penaltyDetails = "";
                if (penaltyFromInGame > 0 && penaltyFromReal > 0)
                {
                    penaltyDetails = $" (${penaltyFromInGame:F2} from in-game money, ${penaltyFromReal:F2} from real money)";
                }
                else if (penaltyFromInGame > 0)
                {
                    penaltyDetails = $" (from in-game money)";
                }
                else if (penaltyFromReal > 0)
                {
                    penaltyDetails = $" (from real money)";
                }
                
                return $"You received your entry fee back, but a ${totalPenalty:F2} penalty was deducted{penaltyDetails} ({outcome.BonusPercentage:F1}% penalty)";
            }
            else
            {
                return $"You received your entry fee back with no bonus or penalty";
            }
        }

        private async Task BroadcastGameState(string gameId)
        {
            if (!_games.TryGetValue(gameId, out var game))
                return;

            var gameStateForFrontend = new
            {
                GameId = gameId,
                Boxes = game.Boxes,
                PlayerId = game.PlayerId,
                Status = game.Status.ToString(),
                RevealedBoxesCount = game.RevealedBoxesCount,
                CurrentSum = game.CurrentSum,
                MaxBoxes = game.MaxBoxes,
                RemainingBoxes = game.RemainingBoxes,
                CanRevealMore = game.CanRevealMoreBoxes,
                IsGameFinished = game.Status != SingleGameStatus.Playing,
                EntryFee = GAME_ENTRY_FEE,
                // Show potential outcome based on current sum
                PotentialOutcome = game.Status == SingleGameStatus.Playing ? 
                    CalculateMoneyOutcome(game.CurrentSum, GAME_ENTRY_FEE) : null
            };

            await Clients.Group(gameId).SendAsync("GameState", gameStateForFrontend);
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
                Console.WriteLine($"Error extracting user ID from token: {ex}");
                return null;
            }
        }

        public override async Task OnConnectedAsync()
        {
            Console.WriteLine($"Single game client connected: {Context.ConnectionId}");
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"Single game client disconnected: {Context.ConnectionId}");
            
            if (_playerConnections.TryGetValue(Context.ConnectionId, out var playerId))
            {
                _playerConnections.Remove(Context.ConnectionId);
                Console.WriteLine($"Player {playerId} removed from single game connections");
            }

            // Find and clean up any active games for this connection
            var gamesToCleanup = _games.Where(kvp => kvp.Value.PlayerId == playerId).ToList();
            foreach (var game in gamesToCleanup)
            {
                try
                {
                    // Record game as abandoned if it was still playing
                    if (game.Value.Status == SingleGameStatus.Playing)
                    {
                        await _gameService.UpdateGameRecordAsync(game.Key, null, "Abandoned");
                        
                        // If player disconnects, they forfeit the entry fee (no refund)
                        Console.WriteLine($"Marked single game {game.Key} as abandoned due to disconnection - no refund");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error marking game {game.Key} as abandoned: {ex}");
                }

                _games.Remove(game.Key);
                _gameMoneyInfo.Remove(game.Key); // Clean up money info
                Console.WriteLine($"Cleaned up single game {game.Key} due to player disconnection");
            }

            if (exception != null)
            {
                Console.WriteLine($"Disconnect exception: {exception}");
            }
            
            await base.OnDisconnectedAsync(exception);
        }
    }

    // Helper class for tracking money deduction details
    public class GameMoneyInfo
    {
        public int PlayerId { get; set; }
        public decimal TotalDeducted { get; set; }
        public decimal FromInGameMoney { get; set; }
        public decimal FromRealMoney { get; set; }
        public List<int> TransactionIds { get; set; } = new();
    }

    // Helper class for money calculations
    public class MoneyOutcome
    {
        public decimal BonusAmount { get; set; } // Can be positive (bonus) or negative (penalty)
        public decimal BonusPercentage { get; set; }
        public decimal TotalReturnAmount { get; set; } // Entry fee + bonus/penalty
        public decimal NetGainLoss { get; set; } // Just the bonus/penalty amount
    }
}