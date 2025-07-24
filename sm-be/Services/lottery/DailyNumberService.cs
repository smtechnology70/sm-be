using Microsoft.EntityFrameworkCore;
using SM_BE.Data;
using SM_BE.Models;
using SM_BE.Models.Lottery;

namespace sm_be.Services.lottery
{
    public class DailyNumberService : IDailyNumberService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DailyNumberService> _logger;

        public DailyNumberService(AppDbContext context, ILogger<DailyNumberService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DailyNumber> GetOrCreateTodaysNumberAsync()
        {
            var today = DateTime.UtcNow.Date;
            
            var existingNumber = await _context.DailyNumbers
                .FirstOrDefaultAsync(d => d.Date == today);

            if (existingNumber != null)
            {
                return existingNumber;
            }

            // Generate a new random number for today
            var random = new Random();
            var winningNumber = random.Next(0, 100); // 0-99

            var dailyNumber = new DailyNumber
            {
                Date = today,
                WinningNumber = winningNumber,
                CreatedAt = DateTime.UtcNow
            };

            _context.DailyNumbers.Add(dailyNumber);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Generated daily number {winningNumber} for {today:yyyy-MM-dd}");

            return dailyNumber;
        }

        public async Task<DailyNumber?> GetTodaysNumberAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.DailyNumbers
                .Include(d => d.PlayerEntries)
                .ThenInclude(pe => pe.User)
                .FirstOrDefaultAsync(d => d.Date == today);
        }

        public async Task<PlayerEntry?> SubmitPlayerGuessAsync(int userId, int guessedNumber)
        {
            if (guessedNumber < 0 || guessedNumber > 99)
            {
                throw new ArgumentException("Guessed number must be between 0 and 99");
            }

            var today = DateTime.UtcNow.Date;
            var dailyNumber = await GetOrCreateTodaysNumberAsync();

            // Check if player already submitted today
            var existingEntry = await _context.PlayerEntries
                .FirstOrDefaultAsync(pe => pe.UserId == userId && pe.DailyNumberId == dailyNumber.Id);

            if (existingEntry != null)
            {
                throw new InvalidOperationException("Player has already submitted a guess for today");
            }

            // Create new entry
            var playerEntry = new PlayerEntry
            {
                UserId = userId,
                DailyNumberId = dailyNumber.Id,
                GuessedNumber = guessedNumber,
                EntryTime = DateTime.UtcNow,
                IsWinner = guessedNumber == dailyNumber.WinningNumber
            };

            _context.PlayerEntries.Add(playerEntry);
            await _context.SaveChangesAsync();

            // Load the entry with navigation properties
            return await _context.PlayerEntries
                .Include(pe => pe.User)
                .Include(pe => pe.DailyNumber)
                .FirstOrDefaultAsync(pe => pe.Id == playerEntry.Id);
        }

        public async Task<List<PlayerEntry>> GetTodaysWinnersAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.PlayerEntries
                .Include(pe => pe.User)
                .Include(pe => pe.DailyNumber)
                .Where(pe => pe.DailyNumber.Date == today && pe.IsWinner)
                .OrderBy(pe => pe.EntryTime)
                .ToListAsync();
        }

        public async Task<PlayerEntry?> GetPlayerTodaysEntryAsync(int userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.PlayerEntries
                .Include(pe => pe.User)
                .Include(pe => pe.DailyNumber)
                .FirstOrDefaultAsync(pe => pe.UserId == userId && pe.DailyNumber.Date == today);
        }

        public async Task<bool> HasPlayerEnteredTodayAsync(int userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.PlayerEntries
                .AnyAsync(pe => pe.UserId == userId && pe.DailyNumber.Date == today);
        }

        public async Task ProcessDailyWinnersAsync()
        {
            var today = DateTime.UtcNow.Date;
            var dailyNumber = await _context.DailyNumbers
                .Include(d => d.PlayerEntries)
                .FirstOrDefaultAsync(d => d.Date == today);

            if (dailyNumber == null) return;

            var winners = dailyNumber.PlayerEntries.Where(pe => pe.GuessedNumber == dailyNumber.WinningNumber).ToList();
            
            foreach (var winner in winners)
            {
                winner.IsWinner = true;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Processed {winners.Count} winners for {today:yyyy-MM-dd}");
        }

        public async Task<List<DailyNumber>> GetRecentGamesAsync(int count = 10)
        {
            return await _context.DailyNumbers
                .Include(d => d.PlayerEntries)
                .ThenInclude(pe => pe.User)
                .OrderByDescending(d => d.Date)
                .Take(count)
                .ToListAsync();
        }
    }
}