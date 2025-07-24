using sm_be.Models.MinimumNumberCount;
using SM_BE.Models;
using SM_BE.Models.Lottery;

namespace sm_be.Services.MinimumNumberCount
{
    public interface IDailyDigitGameService
    {
        Task<DailyDigitGame> GetOrCreateTodaysGameAsync();
        Task<DailyDigitGame?> GetTodaysGameAsync();
        Task<PlayerDigitEntry?> SubmitPlayerDigitAsync(int userId, int selectedDigit);
        Task<List<PlayerDigitEntry>> GetTodaysWinnersAsync();
        Task<PlayerDigitEntry?> GetPlayerTodaysEntryAsync(int userId);
        Task<bool> HasPlayerEnteredTodayAsync(int userId);
        Task ProcessDailyDigitWinnersAsync();
        Task<List<DailyDigitGame>> GetRecentGamesAsync(int count = 10);
        Task<Dictionary<int, int>> GetTodaysDigitCountsAsync();
    }
}