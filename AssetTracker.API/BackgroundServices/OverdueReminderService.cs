using AssetTracker.API.Data;
using AssetTracker.API.Models;
using AssetTracker.API.Services;
using Microsoft.EntityFrameworkCore;

namespace AssetTracker.API.BackgroundServices
{
    public class OverdueReminderService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OverdueReminderService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(24); // Run once daily

        public OverdueReminderService(
            IServiceProvider serviceProvider,
            ILogger<OverdueReminderService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Overdue Reminder Service started.");

            // Calculate next run time (daily at 8 AM UTC)
            var now = DateTime.UtcNow;
            var nextRun = new DateTime(now.Year, now.Month, now.Day, 8, 0, 0);
            if (now > nextRun)
            {
                nextRun = nextRun.AddDays(1);
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = nextRun - DateTime.UtcNow;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation($"Next overdue check scheduled for: {nextRun:yyyy-MM-dd HH:mm:ss} UTC");
                    await Task.Delay(delay, stoppingToken);
                }

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var processor = scope.ServiceProvider.GetRequiredService<IOverdueReminderProcessor>();
                    await processor.ProcessOverdueRemindersAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing overdue reminders.");
                }

                // Schedule next run for tomorrow at 8 AM
                nextRun = nextRun.AddDays(1);
            }

            _logger.LogInformation("Overdue Reminder Service stopped.");
        }
    }
}
