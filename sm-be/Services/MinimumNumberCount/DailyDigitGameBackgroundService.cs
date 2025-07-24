using sm_be.Services.lottery;

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
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var now = DateTime.UtcNow;
                    
                    // Calculate time until midnight UTC (end of day)
                    var nextMidnight = now.Date.AddDays(1);
                    var timeUntilMidnight = nextMidnight - now;

                    // Wait until midnight
                    await Task.Delay(timeUntilMidnight, stoppingToken);

                    // Process winners at midnight
                    using var scope = _serviceProvider.CreateScope();
                    var digitGameService = scope.ServiceProvider.GetRequiredService<IDailyDigitGameService>();
                    
                    await digitGameService.ProcessDailyDigitWinnersAsync();
                    
                    _logger.LogInformation("Daily digit game winners processed successfully");
                }
                catch (OperationCanceledException)
                {
                    // Service is stopping
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing daily digit game winners");
                    
                    // Wait 1 hour before retrying if there's an error
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
            }
        }
    }
}