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

            // Verify the daily number is for today's date
            if (dailyNumber.Date.Date != today)
            {
                _logger.LogError($"Date mismatch! Expected: {today:yyyy-MM-dd}, DailyNumber.Date: {dailyNumber.Date.Date:yyyy-MM-dd}");
                throw new InvalidOperationException($"Daily number date mismatch. Expected: {today:yyyy-MM-dd}, Actual: {dailyNumber.Date.Date:yyyy-MM-dd}");
            }

            // Check if player already submitted today using date-based query
            var existingEntry = await _context.PlayerEntries
                .Include(pe => pe.DailyNumber)
                .FirstOrDefaultAsync(pe => pe.UserId == userId && pe.DailyNumber.Date == today);

            if (existingEntry != null)
            {
                throw new InvalidOperationException("Player has already submitted a guess for today");
            }

            // Create new entry with explicit date verification
            var playerEntry = new PlayerEntry
            {
                UserId = userId,
                DailyNumberId = dailyNumber.Id, // This links to today's daily number
                GuessedNumber = guessedNumber,
                EntryTime = DateTime.UtcNow,
                IsWinner = false // Always false at submission time
            };

            _context.PlayerEntries.Add(playerEntry);
            await _context.SaveChangesAsync();

            // Load the entry with navigation properties for verification
            var savedEntry = await _context.PlayerEntries
                .Include(pe => pe.User)
                .Include(pe => pe.DailyNumber)
                .FirstOrDefaultAsync(pe => pe.Id == playerEntry.Id);

            // Log for verification
            if (savedEntry != null)
            {
                _logger.LogInformation($"Player {userId} submitted guess {guessedNumber} for date {savedEntry.DailyNumber.Date:yyyy-MM-dd}");
            }

            return savedEntry;
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
            // Process winners for yesterday's game (after the day has ended)
            var yesterday = DateTime.UtcNow.Date.AddDays(-1);
            var dailyNumber = await _context.DailyNumbers
                .Include(d => d.PlayerEntries)
                .FirstOrDefaultAsync(d => d.Date == yesterday);

            if (dailyNumber == null) 
            {
                _logger.LogInformation($"No daily number found for {yesterday:yyyy-MM-dd}");
                return;
            }

            _logger.LogInformation($"Processing winners for {yesterday:yyyy-MM-dd}. Daily winning number: {dailyNumber.WinningNumber}");

            // Get all entries for yesterday's game that haven't been processed yet
            var unprocessedEntries = await _context.PlayerEntries
                .Include(pe => pe.DailyNumber)
                .Include(pe => pe.User)
                .Where(pe => pe.DailyNumber.Date == yesterday && 
                           !pe.IsWinner && 
                           pe.GuessedNumber == dailyNumber.WinningNumber)
                .ToListAsync();
            
            if (unprocessedEntries.Count == 0)
            {
                _logger.LogInformation($"No unprocessed winners found for {yesterday:yyyy-MM-dd}");
                return;
            }

            // Log each winner for verification
            foreach (var winner in unprocessedEntries)
            {
                winner.IsWinner = true;
                _logger.LogInformation($"Winner found: User {winner.UserId} guessed {winner.GuessedNumber} for date {winner.DailyNumber.Date:yyyy-MM-dd} (winning number: {dailyNumber.WinningNumber})");
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation($"Processed {unprocessedEntries.Count} winners for {yesterday:yyyy-MM-dd}");
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