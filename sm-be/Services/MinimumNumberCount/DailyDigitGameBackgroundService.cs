using sm_be.Services.MinimumNumberCount;

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
                    
                    // Calculate time until midnight UTC (end of day)
                    var nextMidnight = now.Date.AddDays(1);
                    var timeUntilMidnight = nextMidnight - now;

                    _logger.LogInformation($"Next midnight: {nextMidnight:yyyy-MM-dd HH:mm:ss} UTC");
                    _logger.LogInformation($"Time until midnight: {timeUntilMidnight.TotalHours:F2} hours");

                    // For testing purposes, also run every hour to check
                    var waitTime = timeUntilMidnight;
                    
                    // If more than 1 hour until midnight, wait 1 hour and check again
                    if (waitTime.TotalHours > 1)
                    {
                        waitTime = TimeSpan.FromHours(1);
                        _logger.LogInformation("Waiting 1 hour before next check");
                    }
                    else
                    {
                        _logger.LogInformation($"Waiting {waitTime.TotalMinutes:F0} minutes until midnight to process winners");
                    }

                    // Wait until the calculated time
                    await Task.Delay(waitTime, stoppingToken);

                    // If we're close to midnight (within 5 minutes), process winners
                    now = DateTime.UtcNow;
                    var minutesUntilMidnight = (now.Date.AddDays(1) - now).TotalMinutes;
                    
                    if (minutesUntilMidnight <= 5) // Within 5 minutes of midnight
                    {
                        _logger.LogInformation("Processing daily digit game winners...");
                        
                        // Process winners at midnight
                        using var scope = _serviceProvider.CreateScope();
                        var digitGameService = scope.ServiceProvider.GetRequiredService<IDailyDigitGameService>();
                        
                        await digitGameService.ProcessDailyDigitWinnersAsync();
                        
                        _logger.LogInformation("Daily digit game winners processed successfully");
                        
                        // Wait a bit after processing to avoid running again immediately
                        await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Daily Digit Game Background Service is stopping");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing daily digit game winners");
                    
                    // Wait 10 minutes before retrying if there's an error
                    await Task.Delay(TimeSpan.FromMinutes(10), stoppingToken);
                }
            }
            
            _logger.LogInformation("Daily Digit Game Background Service stopped");
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