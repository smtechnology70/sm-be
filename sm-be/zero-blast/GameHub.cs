using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using SM_BE.Services;
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

        public GameHub(IJwtService jwtService)
        {
            _jwtService = jwtService;
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

                // Add player to waiting queue
                var waitingPlayer = new WaitingPlayer
                {
                    PlayerId = playerId.Value,
                    ConnectionId = Context.ConnectionId,
                    JoinedAt = DateTime.UtcNow
                };

                _waitingQueue.Enqueue(waitingPlayer);
                _playerConnections[Context.ConnectionId] = playerId.Value;

                Console.WriteLine($"Player {playerId} added to queue. Queue size: {_waitingQueue.Count}");

                // Notify player they're in queue
                await Clients.Caller.SendAsync("MatchmakingStatus", "Searching for opponent...", _waitingQueue.Count);

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

                // If game is finished, log the winner
                if (game.Status == GameStatus.Finished)
                {
                    var winnerPlayerId = game.Winner == 1 ? room.Player1Id : room.Player2Id;
                    Console.WriteLine($"Game {room.GameId} finished! Winner: Player ID {winnerPlayerId}");
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
                IsGameFinished = game.Status == GameStatus.Finished
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
                    EndTime = DateTime.UtcNow
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
                // Method 1: Get from JWT claims in the authenticated context
                var userIdClaim = Context.User?.FindFirst("userId");
                if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
                {
                    Console.WriteLine($"Found userId in claims: {userId}");
                    return userId;
                }

                // Method 2: Try to get the token from the connection and validate it manually
                var accessToken = Context.GetHttpContext()?.Request.Cookies["access_token"];
                if (!string.IsNullOrEmpty(accessToken))
                {
                    Console.WriteLine("Found access token in cookies, validating...");
                    if (_jwtService.ValidateToken(accessToken))
                    {
                        var userIdFromToken = _jwtService.GetUserIdFromToken(accessToken);
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
            
            if (isAuthenticated)
            {
                var userId = GetPlayerIdFromToken();
                Console.WriteLine($"Authenticated user ID: {userId}");
            }
            
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            Console.WriteLine($"Client disconnected: {Context.ConnectionId}");
            
            // Remove from player connections
            if (_playerConnections.TryGetValue(Context.ConnectionId, out var playerId))
            {
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
