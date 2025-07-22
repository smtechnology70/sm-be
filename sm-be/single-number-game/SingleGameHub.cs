using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using SM_BE.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace SM_BE.Hubs
{
    [Authorize]
    public class SingleGameHub : Hub
    {
        private static readonly Dictionary<string, SingleGameState> _games = new();
        private static readonly Dictionary<string, int> _playerConnections = new(); // ConnectionId -> PlayerId
        private readonly IJwtService _jwtService;

        public SingleGameHub(IJwtService jwtService)
        {
            _jwtService = jwtService;
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

                // Create unique game ID
                var gameId = Guid.NewGuid().ToString();

                // Create new game state
                var gameState = SingleGameState.CreateNew(playerId.Value);
                _games[gameId] = gameState;
                _playerConnections[Context.ConnectionId] = playerId.Value;

                // Add player to SignalR group
                await Groups.AddToGroupAsync(Context.ConnectionId, gameId);

                // Notify player that game started
                await Clients.Caller.SendAsync("GameStarted", gameId);

                // Send initial game state
                await BroadcastGameState(gameId);

                Console.WriteLine($"Created single game {gameId} for player {playerId}");
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

                // If game is finished, send game over event
                if (game.Status != SingleGameStatus.Playing)
                {
                    await Clients.Group(gameId).SendAsync("GameOver", new
                    {
                        GameId = gameId,
                        Status = game.Status.ToString(),
                        FinalSum = game.CurrentSum,
                        BoxesRevealed = game.RevealedBoxesCount,
                        Won = game.Status == SingleGameStatus.Won,
                        EndTime = DateTime.UtcNow
                    });

                    Console.WriteLine($"Single game {gameId} finished! Status: {game.Status}, Final sum: {game.CurrentSum}");
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

                await Clients.Group(gameId).SendAsync("GameOver", new
                {
                    GameId = gameId,
                    Status = game.Status.ToString(),
                    FinalSum = game.CurrentSum,
                    BoxesRevealed = game.RevealedBoxesCount,
                    Won = game.Status == SingleGameStatus.Won,
                    StoppedEarly = true,
                    EndTime = DateTime.UtcNow
                });

                Console.WriteLine($"Player {playerId} stopped game {gameId} early. Final sum: {game.CurrentSum}, Status: {game.Status}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in StopGame: {ex}");
                await Clients.Caller.SendAsync("Error", $"Failed to stop game: {ex.Message}");
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
                IsGameFinished = game.Status != SingleGameStatus.Playing
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
                _games.Remove(game.Key);
                Console.WriteLine($"Cleaned up single game {game.Key} due to player disconnection");
            }

            if (exception != null)
            {
                Console.WriteLine($"Disconnect exception: {exception}");
            }
            
            await base.OnDisconnectedAsync(exception);
        }
    }
}