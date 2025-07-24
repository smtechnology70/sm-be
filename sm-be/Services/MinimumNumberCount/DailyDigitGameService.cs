using Microsoft.EntityFrameworkCore;
using sm_be.Models.MinimumNumberCount;
using SM_BE.Data;
using SM_BE.Models;
using SM_BE.Models.Lottery;

namespace sm_be.Services.MinimumNumberCount
{
    public class DailyDigitGameService : IDailyDigitGameService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<DailyDigitGameService> _logger;

        public DailyDigitGameService(AppDbContext context, ILogger<DailyDigitGameService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<DailyDigitGame> GetOrCreateTodaysGameAsync()
        {
            var today = DateTime.UtcNow.Date;
            
            var existingGame = await _context.DailyDigitGames
                .FirstOrDefaultAsync(d => d.Date == today);

            if (existingGame != null)
            {
                return existingGame;
            }

            // Create a new game for today
            var dailyDigitGame = new DailyDigitGame
            {
                Date = today,
                CreatedAt = DateTime.UtcNow
            };

            _context.DailyDigitGames.Add(dailyDigitGame);
            await _context.SaveChangesAsync();

            _logger.LogInformation($"Created new daily digit game for {today:yyyy-MM-dd}");

            return dailyDigitGame;
        }

        public async Task<DailyDigitGame?> GetTodaysGameAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.DailyDigitGames
                .Include(d => d.PlayerDigitEntries)
                .ThenInclude(pde => pde.User)
                .FirstOrDefaultAsync(d => d.Date == today);
        }

        public async Task<PlayerDigitEntry?> SubmitPlayerDigitAsync(int userId, int selectedDigit)
        {
            if (selectedDigit < 0 || selectedDigit > 9)
            {
                throw new ArgumentException("Selected digit must be between 0 and 9");
            }

            var today = DateTime.UtcNow.Date;
            var dailyDigitGame = await GetOrCreateTodaysGameAsync();

            // Check if game is already completed
            if (dailyDigitGame.IsCompleted)
            {
                throw new InvalidOperationException("Today's game has already ended");
            }

            // Check if player already submitted today
            var existingEntry = await _context.PlayerDigitEntries
                .FirstOrDefaultAsync(pde => pde.UserId == userId && pde.DailyDigitGameId == dailyDigitGame.Id);

            if (existingEntry != null)
            {
                throw new InvalidOperationException("Player has already selected a digit for today");
            }

            // Create new entry
            var playerDigitEntry = new PlayerDigitEntry
            {
                UserId = userId,
                DailyDigitGameId = dailyDigitGame.Id,
                SelectedDigit = selectedDigit,
                EntryTime = DateTime.UtcNow
            };

            _context.PlayerDigitEntries.Add(playerDigitEntry);
            await _context.SaveChangesAsync();

            // Load the entry with navigation properties
            return await _context.PlayerDigitEntries
                .Include(pde => pde.User)
                .Include(pde => pde.DailyDigitGame)
                .FirstOrDefaultAsync(pde => pde.Id == playerDigitEntry.Id);
        }

        public async Task<List<PlayerDigitEntry>> GetTodaysWinnersAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _context.PlayerDigitEntries
                .Include(pde => pde.User)
                .Include(pde => pde.DailyDigitGame)
                .Where(pde => pde.DailyDigitGame.Date == today && pde.IsWinner)
                .OrderBy(pde => pde.EntryTime)
                .ToListAsync();
        }

        public async Task<PlayerDigitEntry?> GetPlayerTodaysEntryAsync(int userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.PlayerDigitEntries
                .Include(pde => pde.User)
                .Include(pde => pde.DailyDigitGame)
                .FirstOrDefaultAsync(pde => pde.UserId == userId && pde.DailyDigitGame.Date == today);
        }

        public async Task<bool> HasPlayerEnteredTodayAsync(int userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _context.PlayerDigitEntries
                .AnyAsync(pde => pde.UserId == userId && pde.DailyDigitGame.Date == today);
        }

        public async Task<Dictionary<int, int>> GetTodaysDigitCountsAsync()
        {
            var today = DateTime.UtcNow.Date;
            var entries = await _context.PlayerDigitEntries
                .Where(pde => pde.DailyDigitGame.Date == today)
                .GroupBy(pde => pde.SelectedDigit)
                .Select(g => new { Digit = g.Key, Count = g.Count() })
                .ToListAsync();

            // Initialize all digits (0-9) with count 0
            var digitCounts = new Dictionary<int, int>();
            for (int i = 0; i <= 9; i++)
            {
                digitCounts[i] = 0;
            }

            // Update with actual counts
            foreach (var entry in entries)
            {
                digitCounts[entry.Digit] = entry.Count;
            }

            return digitCounts;
        }

        public async Task ProcessDailyDigitWinnersAsync()
        {
            var today = DateTime.UtcNow.Date;
            var dailyDigitGame = await _context.DailyDigitGames
                .Include(d => d.PlayerDigitEntries)
                .FirstOrDefaultAsync(d => d.Date == today);

            if (dailyDigitGame == null || dailyDigitGame.IsCompleted) return;

            // Get digit counts
            var digitCounts = await GetTodaysDigitCountsAsync();
            
            // Find the digit(s) with the lowest count (excluding digits with 0 entries)
            var nonZeroCounts = digitCounts.Where(dc => dc.Value > 0);
            if (!nonZeroCounts.Any())
            {
                _logger.LogInformation($"No entries found for {today:yyyy-MM-dd}, no winners to process");
                return;
            }

            var minCount = nonZeroCounts.Min(dc => dc.Value);
            var winningDigits = nonZeroCounts.Where(dc => dc.Value == minCount).Select(dc => dc.Key).ToList();

            // If there are multiple digits with the same lowest count, pick one randomly
            var random = new Random();
            var winningDigit = winningDigits[random.Next(winningDigits.Count)];

            // Update the game with winning digit
            dailyDigitGame.WinningDigit = winningDigit;
            dailyDigitGame.IsCompleted = true;
            dailyDigitGame.CompletedAt = DateTime.UtcNow;

            // Mark winners
            var winners = dailyDigitGame.PlayerDigitEntries
                .Where(pde => pde.SelectedDigit == winningDigit)
                .ToList();
            
            foreach (var winner in winners)
            {
                winner.IsWinner = true;
            }

            await _context.SaveChangesAsync();
            
            _logger.LogInformation($"Processed daily digit game for {today:yyyy-MM-dd}. Winning digit: {winningDigit}, Winners: {winners.Count}, Min count: {minCount}");
        }

        public async Task<List<DailyDigitGame>> GetRecentGamesAsync(int count = 10)
        {
            return await _context.DailyDigitGames
                .Include(d => d.PlayerDigitEntries)
                .ThenInclude(pde => pde.User)
                .OrderByDescending(d => d.Date)
                .Take(count)
                .ToListAsync();
        }
    }
}