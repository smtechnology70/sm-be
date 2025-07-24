namespace sm_be.Services.lottery
{
    public class DailyNumberBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<DailyNumberBackgroundService> _logger;

        public DailyNumberBackgroundService(IServiceProvider serviceProvider, ILogger<DailyNumberBackgroundService> logger)
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
                    using var scope = _serviceProvider.CreateScope();
                    var dailyNumberService = scope.ServiceProvider.GetRequiredService<IDailyNumberService>();

                    // Ensure today's number exists
                    await dailyNumberService.GetOrCreateTodaysNumberAsync();

                    // Process winners (mark them as winners in the database)
                    await dailyNumberService.ProcessDailyWinnersAsync();

                    _logger.LogInformation("Daily number maintenance completed");

                    // Wait 1 hour before next check
                    await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in daily number background service");
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken); // Retry after 5 minutes on error
                }
            }
        }
    }
}