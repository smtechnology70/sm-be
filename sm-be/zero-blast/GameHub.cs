using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using SM_BE.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace SM_BE.Hubs
{
    [Authorize] // Require authentication for all hub methods
    public class GameHub : Hub
    {
        // --- in‑memory store (replace with DB/Redis in prod) ---
        private static readonly Dictionary<string, GameState> _games = new();
        private readonly IJwtService _jwtService;

        public GameHub(IJwtService jwtService)
        {
            _jwtService = jwtService;
        }

        // ➊  A player joins or creates a game - get player ID from JWT token
        public async Task JoinGame(string gameId)
        {
            try
            {
                Console.WriteLine($"JoinGame called with gameId: '{gameId}'");

                // Basic validation
                if (string.IsNullOrEmpty(gameId))
                {
                    await Clients.Caller.SendAsync("Error", "Game ID is required");
                    return;
                }

                // Get player ID from JWT token
                var playerId = GetPlayerIdFromToken();
                if (playerId == null)
                {
                    await Clients.Caller.SendAsync("Error", "Invalid or missing authentication token");
                    return;
                }

                Console.WriteLine($"Extracted player ID from token: {playerId}");

                // Create game if it doesn't exist
                if (!_games.ContainsKey(gameId))
                {
                    _games[gameId] = GameState.CreateNew();
                    Console.WriteLine($"Created new game: {gameId}");
                }

                // Add to SignalR group
                await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

                // Send current state to the caller
                var gameState = _games[gameId];
                await Clients.Caller.SendAsync("State", gameState);

                // Notify the group that someone joined
                await Clients.Group(gameId).SendAsync("Joined", playerId.Value, Context.ConnectionId);

                // Log successful join
                Console.WriteLine($"Player {playerId} joined game {gameId} successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in JoinGame: {ex}");
                await Clients.Caller.SendAsync("Error", $"Failed to join game: {ex.Message}");
            }
        }

        // ➋  A player clicks a box - get player ID from JWT token
        public async Task Move(string gameId, int index)
        {
            try
            {
                Console.WriteLine($"Move called with gameId: '{gameId}', index: {index}");

                // Basic validation
                if (string.IsNullOrEmpty(gameId))
                {
                    await Clients.Caller.SendAsync("Error", "Game ID is required");
                    return;
                }

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

                if (!_games.TryGetValue(gameId, out var game))
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                if (game.Status == GameStatus.Finished)
                {
                    await Clients.Caller.SendAsync("Error", "Game is already finished");
                    return;
                }

                if (game.CurrentPlayer != playerId.Value)
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

                // Broadcast updated state to everyone in the game
                await Clients.Group(gameId).SendAsync("State", game);

                // Log successful move
                Console.WriteLine($"Player {playerId} moved in game {gameId}, index {index}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Move: {ex}");
                await Clients.Caller.SendAsync("Error", $"Failed to make move: {ex.Message}");
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
            if (exception != null)
            {
                Console.WriteLine($"Disconnect exception: {exception}");
            }
            await base.OnDisconnectedAsync(exception);
        }
    }
}
