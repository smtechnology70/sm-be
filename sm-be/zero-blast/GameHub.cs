using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SM_BE.Services;
using SM_BE.Dto;
using SM_BE.Data;
using SM_BE.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SM_BE.Hubs
{
    [Authorize] // Require authentication for all hub methods
    public class GameHub : Hub
    {
        // --- in‑memory store (replace with DB/Redis in prod) ---
        private static readonly Dictionary<string, GameState> _games = new();
        private static readonly Dictionary<string, GameRoom> _rooms = new();
        private static readonly Queue<WaitingPlayer> _waitingQueue = new();
        private static readonly Dictionary<string, int> _playerConnections = new(); // ConnectionId -> PlayerId
        private readonly IJwtService _jwtService;
        private readonly IMoneyTransactionService _moneyTransactionService;
        private readonly AppDbContext _dbContext;
        private const decimal GAME_ENTRY_FEE = 50m;

        public GameHub(IJwtService jwtService, IMoneyTransactionService moneyTransactionService, AppDbContext dbContext)
        {
            _jwtService = jwtService;
            _moneyTransactionService = moneyTransactionService;
            _dbContext = dbContext;
        }

        // ➊ Player joins matchmaking queue
        public async Task JoinMatchmaking()
        {
            try
            {
                Console.WriteLine("JoinMatchmaking called");

                // Get player ID from JWT token
                var playerId = GetPlayerIdFromToken();
                if (playerId == null)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid or missing authentication token");
                    return;
                }

                Console.WriteLine($"Player {playerId} joining matchmaking");

                // Check if player has sufficient funds
                var hasSufficientFunds = await _moneyTransactionService.HasSufficientFundsAsync(playerId.Value, GAME_ENTRY_FEE);
                if (!hasSufficientFunds)
                {
                    var userMoney = await _moneyTransactionService.GetUserMoneyAsync(playerId.Value);
                    await Clients.Caller.SendAsync("InsufficientFunds", 
                        $"Insufficient funds to join the game. Required: ${GAME_ENTRY_FEE}, Available: ${userMoney?.TotalMoney ?? 0}");
                    return;
                }

                // Check if player is already in queue
                if (_waitingQueue.Any(p => p.PlayerId == playerId.Value))
                {
                    await Clients.Caller.SendAsync("Error", "You are already in the matchmaking queue");
                    return;
                }

                // Check if player is already in a game
                if (_playerConnections.ContainsKey(Context.ConnectionId))
                {
                    await Clients.Caller.SendAsync("Error", "You are already in a game");
                    return;
                }

                // Generate a temporary game ID for the entry fee transaction
                var tempGameId = Guid.NewGuid().ToString();

                // Deduct entry fee with the temporary game ID
                var deductionResult = await _moneyTransactionService.ProcessGameEntryAsync(playerId.Value, GAME_ENTRY_FEE, "zero-blast", tempGameId);
                if (!deductionResult.Success)
                {
                    await Clients.Caller.SendAsync("Error", $"Failed to process entry fee: {deductionResult.Message}");
                    return;
                }

                // Add player to waiting queue
                var waitingPlayer = new WaitingPlayer
                {
                    PlayerId = playerId.Value,
                    ConnectionId = Context.ConnectionId,
                    JoinedAt = DateTime.UtcNow,
                    TempGameId = tempGameId // Store temp game ID for potential refund
                };

                _waitingQueue.Enqueue(waitingPlayer);
                _playerConnections[Context.ConnectionId] = playerId.Value;

                Console.WriteLine($"Player {playerId} added to queue. Queue size: {_waitingQueue.Count}");

                // Notify player they're in queue and about the deduction
                await Clients.Caller.SendAsync("MatchmakingStatus", "Searching for opponent...", _waitingQueue.Count);
                await Clients.Caller.SendAsync("MoneyDeducted", new
                {
                    Amount = GAME_ENTRY_FEE,
                    RemainingMoney = deductionResult.TotalRemainingMoney,
                    Message = "Entry fee deducted successfully"
                });

                // Try to create a match
                await TryCreateMatch();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in JoinMatchmaking: {ex}");
                await Clients.Caller.SendAsync("Error", $"Failed to join matchmaking: {ex.Message}");
            }
        }

        // ➋ Player leaves matchmaking queue
        public async Task LeaveMatchmaking()
        {
            try
            {
                var playerId = GetPlayerIdFromToken();
                if (playerId == null)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid or missing authentication token");
                    return;
                }

                // Check if player is in queue and refund entry fee
                var playerInQueue = _waitingQueue.FirstOrDefault(p => p.PlayerId == playerId.Value);
                if (playerInQueue != null)
                {
                    // Refund the entry fee with the same temp game ID for tracking
                    var refundResult = await _moneyTransactionService.AddMoneyAsync(
                        playerId.Value, GAME_ENTRY_FEE, MoneyType.InGameMoney, "game_entry_refund", 
                        "Refund for leaving matchmaking queue", "zero-blast", playerInQueue.TempGameId);

                    if (refundResult.Success)
                    {
                        await Clients.Caller.SendAsync("MoneyRefunded", new
                        {
                            Amount = GAME_ENTRY_FEE,
                            NewBalance = refundResult.TotalMoney,
                            Message = "Entry fee refunded"
                        });
                    }
                }

                // Remove from queue
                RemovePlayerFromQueue(playerId.Value);
                _playerConnections.Remove(Context.ConnectionId);

                await Clients.Caller.SendAsync("MatchmakingStatus", "Left matchmaking", 0);
                Console.WriteLine($"Player {playerId} left matchmaking");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in LeaveMatchmaking: {ex}");
                await Clients.Caller.SendAsync("Error", $"Failed to leave matchmaking: {ex.Message}");
            }
        }

        // ➌ Player makes a move in their game
        public async Task Move(int index)
        {
            try
            {
                Console.WriteLine($"Move called with index: {index}");

                // Get player ID from JWT token
                var playerId = GetPlayerIdFromToken();
                if (playerId == null)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid or missing authentication token");
                    return;
                }

                if (index < 0)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid move index");
                    return;
                }

                // Find player's current game
                var room = _rooms.Values.FirstOrDefault(r => 
                    r.Player1Id == playerId.Value || r.Player2Id == playerId.Value);

                if (room == null)
                {
                    await Clients.Caller.SendAsync("Error", "You are not in a game");
                    return;
                }

                if (!_games.TryGetValue(room.GameId, out var game))
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                if (game.Status == GameStatus.Finished)
                {
                    await Clients.Caller.SendAsync("Error", "Game is already finished");
                    return;
                }

                // Determine player number based on room entry order
                // Player1 (first to enter room) = 1, Player2 (second to enter) = 2
                var playerNumber = room.Player1Id == playerId.Value ? 1 : 2;
                
                Console.WriteLine($"Player {playerId} is Player{playerNumber}, CurrentPlayer: {game.CurrentPlayer}");
                
                if (game.CurrentPlayer != playerNumber)
                {
                    await Clients.Caller.SendAsync("Error", "Not your turn");
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

                // Broadcast enhanced game state with turn information
                await BroadcastGameState(room.GameId);

                // Log successful move
                Console.WriteLine($"Player {playerId} (Player{playerNumber}) moved in game {room.GameId}, index {index}");

                // If game is finished, handle winner rewards and record game completion
                if (game.Status == GameStatus.Finished)
                {
                    var winnerPlayerId = game.Winner == 1 ? room.Player1Id : room.Player2Id;
                    Console.WriteLine($"Game {room.GameId} finished! Winner: Player ID {winnerPlayerId}");

                    // Update game record in database
                    await UpdateGameRecord(room.GameId, winnerPlayerId, "Finished");

                    // Award double the entry fee to the winner as real money with game ID
                    var winAmount = GAME_ENTRY_FEE * 2;
                    var winResult = await _moneyTransactionService.ProcessGameWinAsync(winnerPlayerId, winAmount, "zero-blast", room.GameId);

                    if (winResult.Success)
                    {
                        // Notify winner about the reward
                        var winnerConnectionId = game.Winner == 1 ? room.Player1ConnectionId : room.Player2ConnectionId;
                        await Clients.Client(winnerConnectionId).SendAsync("GameWinReward", new
                        {
                            WinAmount = winAmount,
                            NewBalance = winResult.TotalMoney,
                            Message = $"Congratulations! You won ${winAmount}!"
                        });

                        Console.WriteLine($"Winner {winnerPlayerId} received ${winAmount} reward for game {room.GameId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Move: {ex}");
                await Clients.Caller.SendAsync("Error", $"Failed to make move: {ex.Message}");
            }
        }

        private async Task TryCreateMatch()
        {
            // Need at least 2 players to create a match
            if (_waitingQueue.Count < 2)
                return;

            // First player dequeued becomes Player1 (gets first move)
            var player1 = _waitingQueue.Dequeue(); // First to enter room
            var player2 = _waitingQueue.Dequeue(); // Second to enter room

            // Create unique game ID
            var gameId = Guid.NewGuid().ToString();

            // Create game room - Player1 is the first to enter, Player2 is second
            var room = new GameRoom
            {
                GameId = gameId,
                Player1Id = player1.PlayerId,     // First player gets first move
                Player2Id = player2.PlayerId,     // Second player goes second
                Player1ConnectionId = player1.ConnectionId,
                Player2ConnectionId = player2.ConnectionId,
                CreatedAt = DateTime.UtcNow
            };

            _rooms[gameId] = room;

            // Create game state - CurrentPlayer starts at 1 (Player1 gets first move)
            var gameState = GameState.CreateNew();
            gameState.SetPlayers(player1.PlayerId, player2.PlayerId);
            _games[gameId] = gameState;

            // Record game in database
            await CreateGameRecord(gameId, player1.PlayerId, player2.PlayerId, GAME_ENTRY_FEE);

            // Add both players to SignalR group
            await Groups.AddToGroupAsync(player1.ConnectionId, gameId);
            await Groups.AddToGroupAsync(player2.ConnectionId, gameId);

            // Notify both players that match was found
            // Player1 (first to enter) gets playerNumber 1 and goes first
            // Player2 (second to enter) gets playerNumber 2 and goes second
            await Clients.Client(player1.ConnectionId).SendAsync("MatchFound", gameId, 1, player2.PlayerId, true);  // true = your turn
            await Clients.Client(player2.ConnectionId).SendAsync("MatchFound", gameId, 2, player1.PlayerId, false); // false = wait for turn

            // Send enhanced initial game state to both players
            await BroadcastGameState(gameId);

            Console.WriteLine($"Created match {gameId} between players {player1.PlayerId} (Player1 - first move) and {player2.PlayerId} (Player2 - second move)");

            // Update queue status for remaining players
            await UpdateQueueStatus();
        }

        private async Task CreateGameRecord(string gameId, int player1Id, int player2Id, decimal entryFee)
        {
            try
            {
                var game = new Game
                {
                    GameId = gameId,
                    GameType = "zero-blast",
                    Player1Id = player1Id,
                    Player2Id = player2Id,
                    Status = "Playing",
                    EntryFee = entryFee,
                    StartedAt = DateTime.UtcNow
                };

                _dbContext.Games.Add(game);
                await _dbContext.SaveChangesAsync();

                Console.WriteLine($"Game record created for game {gameId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error creating game record for {gameId}: {ex}");
            }
        }

        private async Task UpdateGameRecord(string gameId, int winnerId, string status)
        {
            try
            {
                var game = await _dbContext.Games.FirstOrDefaultAsync(g => g.GameId == gameId);
                if (game != null)
                {
                    game.WinnerId = winnerId;
                    game.Status = status;
                    game.FinishedAt = DateTime.UtcNow;
                    game.WinAmount = GAME_ENTRY_FEE * 2;

                    _dbContext.Games.Update(game);
                    await _dbContext.SaveChangesAsync();

                    Console.WriteLine($"Game record updated for game {gameId}, winner: {winnerId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error updating game record for {gameId}: {ex}");
            }
        }

        private async Task BroadcastGameState(string gameId)
        {
            if (!_games.TryGetValue(gameId, out var game) || !_rooms.TryGetValue(gameId, out var room))
                return;

            // Determine whose turn it is
            var currentPlayerId = game.CurrentPlayer == 1 ? room.Player1Id : room.Player2Id;

            // Determine winner's player ID if game is finished
            int? winnerPlayerId = null;
            if (game.Status == GameStatus.Finished && game.Winner.HasValue)
            {
                winnerPlayerId = game.Winner == 1 ? room.Player1Id : room.Player2Id;
            }

            // Create enhanced game state for frontend
            var gameStateForFrontend = new
            {
                // Original game state
                Boxes = game.Boxes,
                CurrentPlayer = game.CurrentPlayer,
                Winner = game.Winner, // Keep the original winner number for backward compatibility
                Status = game.Status,
                Player1Id = game.Player1Id,
                Player2Id = game.Player2Id,
                
                // Enhanced turn information
                CurrentPlayerId = currentPlayerId,
                IsPlayer1Turn = game.CurrentPlayer == 1,
                IsPlayer2Turn = game.CurrentPlayer == 2,
                
                // Enhanced winner information
                WinnerPlayerId = winnerPlayerId, // The actual player ID of the winner
                IsGameFinished = game.Status == GameStatus.Finished,
                
                // Game details
                GameId = gameId, // Include game ID in the state
                EntryFee = GAME_ENTRY_FEE,
                WinAmount = GAME_ENTRY_FEE * 2
            };

            // Send to each player with personalized turn and winner info
            await Clients.Client(room.Player1ConnectionId).SendAsync("State", gameStateForFrontend, new
            {
                IsYourTurn = game.CurrentPlayer == 1,
                YourPlayerNumber = 1,
                OpponentPlayerId = room.Player2Id,
                DidYouWin = winnerPlayerId == room.Player1Id,
                DidYouLose = winnerPlayerId == room.Player2Id
            });

            await Clients.Client(room.Player2ConnectionId).SendAsync("State", gameStateForFrontend, new
            {
                IsYourTurn = game.CurrentPlayer == 2,
                YourPlayerNumber = 2,
                OpponentPlayerId = room.Player1Id,
                DidYouWin = winnerPlayerId == room.Player2Id,
                DidYouLose = winnerPlayerId == room.Player1Id
            });

            // If game is finished, send additional game over event with winner details
            if (game.Status == GameStatus.Finished && winnerPlayerId.HasValue)
            {
                await Clients.Group(gameId).SendAsync("GameOver", new
                {
                    WinnerPlayerId = winnerPlayerId.Value,
                    WinnerPlayerNumber = game.Winner.Value,
                    GameId = gameId,
                    EndTime = DateTime.UtcNow,
                    WinAmount = GAME_ENTRY_FEE * 2
                });
            }
        }

        private void RemovePlayerFromQueue(int playerId)
        {
            var tempQueue = new Queue<WaitingPlayer>();
            
            while (_waitingQueue.Count > 0)
            {
                var player = _waitingQueue.Dequeue();
                if (player.PlayerId != playerId)
                {
                    tempQueue.Enqueue(player);
                }
            }

            while (tempQueue.Count > 0)
            {
                _waitingQueue.Enqueue(tempQueue.Dequeue());
            }
        }

        private async Task UpdateQueueStatus()
        {
            var queueSize = _waitingQueue.Count;
            foreach (var player in _waitingQueue)
            {
                await Clients.Client(player.ConnectionId).SendAsync("MatchmakingStatus", 
                    "Searching for opponent...", queueSize);
            }
        }

        private int? GetPlayerIdFromToken()
        {
            try
            {
                Console.WriteLine("=== GetPlayerIdFromToken Debug ===");
                
                // Method 1: Get from JWT claims in the authenticated context
                var userIdClaim = Context.User?.FindFirst("userId");
                Console.WriteLine($"User claims count: {Context.User?.Claims?.Count() ?? 0}");
                Console.WriteLine($"UserId claim found: {userIdClaim != null}");
                
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    Console.WriteLine($"Found userId in claims: {userId}");
                    return userId;
                }

                // Method 2: Try to get the token from the query string and validate it manually
                var accessToken = Context.GetHttpContext()?.Request.Query["access_token"];
                Console.WriteLine($"Access token from query: {!string.IsNullOrEmpty(accessToken)}");
                
                if (!string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("Found access token in query string, validating...");
                    var isValid = _jwtService.ValidateToken(accessToken);
                    Console.WriteLine($"Token validation result: {isValid}");
                    
                    if (isValid)
                    {
                        var userIdFromToken = _jwtService.GetUserIdFromToken(accessToken);
                        Console.WriteLine($"UserId from token: {userIdFromToken}");
                        
                        if (userIdFromToken.HasValue)
                        {
                            Console.WriteLine($"Extracted userId from token: {userIdFromToken}");
                            return userIdFromToken;
                        }
                    }
                }

                Console.WriteLine("No valid user ID found in token or claims");
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
            Console.WriteLine($"Client connected: {Context.ConnectionId}");
            
            // Log authentication status
            var isAuthenticated = Context.User?.Identity?.IsAuthenticated ?? false;
            Console.WriteLine($"User authenticated: {isAuthenticated}");
            
            // Check for query string token
            var queryToken = Context.GetHttpContext()?.Request.Query["access_token"];
            Console.WriteLine($"Query token present: {!string.IsNullOrEmpty(queryToken)}");
            
            if (isAuthenticated)
            {
                var userId = GetPlayerIdFromToken();
                Console.WriteLine($"Authenticated user ID: {userId}");
                
                // Log all claims
                if (Context.User?.Claims != null)
                {
                    foreach (var claim in Context.User.Claims)
                    {
                        Console.WriteLine($"Claim: {claim.Type} = {claim.Value}");
                    }
                }
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
            
            // Remove from player connections
            if (_playerConnections.TryGetValue(Context.ConnectionId, out var playerId))
            {
                // Check if player was in queue and refund entry fee
                var playerInQueue = _waitingQueue.FirstOrDefault(p => p.PlayerId == playerId);
                if (playerInQueue != null)
                {
                    try
                    {
                        // Refund the entry fee with the temp game ID for tracking
                        await _moneyTransactionService.AddMoneyAsync(
                            playerId, GAME_ENTRY_FEE, MoneyType.InGameMoney, "game_entry_refund", 
                            "Refund due to disconnection during matchmaking", "zero-blast", playerInQueue.TempGameId);
                        
                        Console.WriteLine($"Refunded ${GAME_ENTRY_FEE} to player {playerId} due to disconnection for temp game {playerInQueue.TempGameId}");
                    }
                    catch (Exception refundEx)
                    {
                        Console.WriteLine($"Error refunding money to player {playerId}: {refundEx}");
                    }
                }

                // Remove from waiting queue if present
                RemovePlayerFromQueue(playerId);
                _playerConnections.Remove(Context.ConnectionId);
                
                // Update queue status for remaining players
                await UpdateQueueStatus();
                
                Console.WriteLine($"Player {playerId} removed from matchmaking due to disconnection");
            }

            // Find and handle game room cleanup
            var room = _rooms.Values.FirstOrDefault(r => 
                r.Player1ConnectionId == Context.ConnectionId || 
                r.Player2ConnectionId == Context.ConnectionId);

            if (room != null)
            {
                // Update game record as abandoned
                await UpdateGameRecord(room.GameId, 0, "Abandoned");

                // Notify the other player
                var otherConnectionId = room.Player1ConnectionId == Context.ConnectionId 
                    ? room.Player2ConnectionId 
                    : room.Player1ConnectionId;

                await Clients.Client(otherConnectionId).SendAsync("OpponentDisconnected");
                
                // Clean up room and game
                _rooms.Remove(room.GameId);
                _games.Remove(room.GameId);
                
                Console.WriteLine($"Cleaned up game room {room.GameId} due to player disconnection");
            }

            if (exception != null)
            {
                Console.WriteLine($"Disconnect exception: {exception}");
            }
            
            await base.OnDisconnectedAsync(exception);
        }
    }

    // Supporting classes
    public class WaitingPlayer
    {
        public int PlayerId { get; set; }
        public string ConnectionId { get; set; } = string.Empty;
        public DateTime JoinedAt { get; set; }
        public string TempGameId { get; set; } = string.Empty; // For tracking refunds
    }

    public class GameRoom
    {
        public string GameId { get; set; } = string.Empty;
        public int Player1Id { get; set; }  // First player to enter room (gets first move)
        public int Player2Id { get; set; }  // Second player to enter room (goes second)
        public string Player1ConnectionId { get; set; } = string.Empty;
        public string Player2ConnectionId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
