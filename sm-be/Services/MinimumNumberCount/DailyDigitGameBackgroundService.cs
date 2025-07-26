using sm_be.Services.MinimumNumberCount;
using Microsoft.EntityFrameworkCore;
using SM_BE.Data;

namespace sm_be.Services.MinimumNumberCount
{
    public class DailyDigitGameBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyDigitGameBackgroundService> _logger;

        public DailyDigitGameBackgroundService(IServiceProvider serviceProvider, ILogger<DailyDigitGameBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Daily Digit Game Background Service started");

            // Wait a bit for the application to fully start
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    _logger.LogInformation($"Current time: {now:yyyy-MM-dd HH:mm:ss} UTC");
                    
                    // Check yesterday's game first
                    var yesterday = now.Date.AddDays(-1);
                    var shouldProcessNow = await ShouldProcessYesterdaysGameAsync(yesterday);
                    
                    if (shouldProcessNow)
                    {
                        _logger.LogInformation($"Found incomplete game for {yesterday:yyyy-MM-dd}. Processing winners now...");
                        
                        using var scope = _serviceProvider.CreateScope();
                        var digitGameService = scope.ServiceProvider.GetRequiredService<IDailyDigitGameService>();
                        
                        await digitGameService.ProcessDailyDigitWinnersAsync();
                        
                        _logger.LogInformation("Daily digit game winners processed successfully");
                        
                        // Wait 30 minutes before next check
                        await Task.Delay(TimeSpan.FromMinutes(30), stoppingToken);
                    }
                    else
                    {
                        // No incomplete game found, wait for midnight to come
                        var nextMidnight = now.Date.AddDays(1);
                        var timeUntilMidnight = nextMidnight - now;

                        _logger.LogInformation($"Yesterday's game already completed. Next midnight: {nextMidnight:yyyy-MM-dd HH:mm:ss} UTC");
                        _logger.LogInformation($"Time until midnight: {timeUntilMidnight.TotalHours:F2} hours");

                        // Wait until just after midnight when there might be a new incomplete game
                        var waitTimeUntilMidnight = timeUntilMidnight.Add(TimeSpan.FromMinutes(1)); // Wait 1 minute past midnight
                        _logger.LogInformation($"No incomplete games to process. Waiting {waitTimeUntilMidnight.TotalHours:F2} hours until {nextMidnight.AddMinutes(1):yyyy-MM-dd HH:mm:ss} UTC");
                        
                        await Task.Delay(waitTimeUntilMidnight, stoppingToken);

                        // After midnight, check again for yesterday's game (which will be the previous day now)
                        _logger.LogInformation("Midnight passed. Will check for incomplete games in next iteration.");
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Daily Digit Game Background Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in daily digit game background service");
                    
                    // Wait 10 minutes before retrying if there's an error
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                }
            }
            
            _logger.LogInformation("Daily Digit Game Background Service stopped");
        }

        private async Task<bool> ShouldProcessYesterdaysGameAsync(DateTime yesterday)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                
                var yesterdaysGame = await context.DailyDigitGames
                    .FirstOrDefaultAsync(d => d.Date == yesterday);
                
                if (yesterdaysGame == null)
                {
                    _logger.LogInformation($"No game found for {yesterday:yyyy-MM-dd}");
                    return false;
                }
                
                if (yesterdaysGame.IsCompleted)
                {
                    _logger.LogInformation($"Game for {yesterday:yyyy-MM-dd} is already completed");
                    return false;
                }
                
                _logger.LogInformation($"Game for {yesterday:yyyy-MM-dd} exists but not completed. Ready to process.");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error checking yesterday's game for {yesterday:yyyy-MM-dd}");
                return false;
            }
        }

        public override async Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Daily Digit Game Background Service is starting");
            await base.StartAsync(cancellationToken);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Daily Digit Game Background Service is stopping");
            await base.StopAsync(cancellationToken);
        }
    }
}