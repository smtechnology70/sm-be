using Microsoft.EntityFrameworkCore;
using SM_BE.Data;
using SM_BE.Models;

namespace SM_BE.Services
{
    public class GameService : IGameService
    {
        private readonly AppDbContext _dbContext;
        private readonly ILogger<GameService> _logger;

        public GameService(AppDbContext dbContext, ILogger<GameService> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task<Game> CreateGameRecordAsync(string gameId, string gameType, int player1Id, int? player2Id = null, decimal entryFee = 0)
        {
            try
            {
                var game = new Game
                {
                    GameId = gameId,
                    GameType = gameType,
                    Player1Id = player1Id,
                    Player2Id = player2Id ?? player1Id, // For single-player games, both players are the same
                    Status = "Playing",
                    EntryFee = entryFee,
                    StartedAt = DateTime.UtcNow
                };

                _dbContext.Games.Add(game);
                await _dbContext.SaveChangesAsync();

                _logger.LogInformation($"Game record created: {gameId} ({gameType}) for player {player1Id}");
                return game;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error creating game record for {gameId}");
                throw;
            }
        }

        public async Task<Game?> UpdateGameRecordAsync(string gameId, int? winnerId, string status, decimal? winAmount = null)
        {
            try
            {
                var game = await _dbContext.Games.FirstOrDefaultAsync(g => g.GameId == gameId);
                if (game != null)
                {
                    game.WinnerId = winnerId;
                    game.Status = status;
                    game.FinishedAt = DateTime.UtcNow;
                    if (winAmount.HasValue)
                        game.WinAmount = winAmount;

                    _dbContext.Games.Update(game);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation($"Game record updated: {gameId}, winner: {winnerId}, status: {status}");
                    return game;
                }
                
                _logger.LogWarning($"Game record not found for update: {gameId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating game record for {gameId}");
                throw;
            }
        }

        public async Task<Game?> GetGameByIdAsync(string gameId)
        {
            try
            {
                return await _dbContext.Games
                    .Include(g => g.Player1)
                    .Include(g => g.Player2)
                    .Include(g => g.Winner)
                    .FirstOrDefaultAsync(g => g.GameId == gameId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting game record for {gameId}");
                throw;
            }
        }

        public async Task<List<Game>> GetPlayerGamesAsync(int playerId, string? gameType = null)
        {
            try
            {
                var query = _dbContext.Games
                    .Include(g => g.Player1)
                    .Include(g => g.Player2)
                    .Include(g => g.Winner)
                    .Where(g => g.Player1Id == playerId || g.Player2Id == playerId);

                if (!string.IsNullOrEmpty(gameType))
                {
                    query = query.Where(g => g.GameType == gameType);
                }

                return await query
                    .OrderByDescending(g => g.StartedAt)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error getting games for player {playerId}");
                throw;
            }
        }

        public async Task<Game?> SetGameDataAsync(string gameId, string gameData)
        {
            try
            {
                var game = await _dbContext.Games.FirstOrDefaultAsync(g => g.GameId == gameId);
                if (game != null)
                {
                    game.GameData = gameData;
                    _dbContext.Games.Update(game);
                    await _dbContext.SaveChangesAsync();

                    _logger.LogInformation($"Game data updated for {gameId}");
                    return game;
                }

                _logger.LogWarning($"Game record not found for data update: {gameId}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error updating game data for {gameId}");
                throw;
            }
        }
    }
}