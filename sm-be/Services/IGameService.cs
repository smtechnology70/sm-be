using SM_BE.Models;

namespace SM_BE.Services
{
    public interface IGameService
    {
        Task<Game> CreateGameRecordAsync(string gameId, string gameType, int player1Id, int? player2Id = null, decimal entryFee = 0);
        Task<Game?> UpdateGameRecordAsync(string gameId, int? winnerId, string status, decimal? winAmount = null);
        Task<Game?> GetGameByIdAsync(string gameId);
        Task<List<Game>> GetPlayerGamesAsync(int playerId, string? gameType = null);
        Task<Game?> SetGameDataAsync(string gameId, string gameData);
    }
}