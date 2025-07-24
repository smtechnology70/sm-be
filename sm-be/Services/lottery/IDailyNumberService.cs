using SM_BE.Models;
using SM_BE.Models.Lottery;

namespace sm_be.Services.lottery
{
    public interface IDailyNumberService
    {
        Task<DailyNumber> GetOrCreateTodaysNumberAsync();
        Task<DailyNumber?> GetTodaysNumberAsync();
        Task<PlayerEntry?> SubmitPlayerGuessAsync(int userId, int guessedNumber);
        Task<List<PlayerEntry>> GetTodaysWinnersAsync();
        Task<PlayerEntry?> GetPlayerTodaysEntryAsync(int userId);
        Task<bool> HasPlayerEnteredTodayAsync(int userId);
        Task ProcessDailyWinnersAsync();
        Task<List<DailyNumber>> GetRecentGamesAsync(int count = 10);
    }
}