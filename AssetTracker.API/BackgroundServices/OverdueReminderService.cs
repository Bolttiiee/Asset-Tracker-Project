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

        // Overdue reminders run once per day at 08:00 SAST (South Africa Standard Time, UTC+2, no daylight saving).
        private static readonly TimeSpan DailyRunTimeSast = new(8, 0, 0);

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

            // Calculate the next run time (daily at 08:00 SAST), expressed in UTC for the timer.
            var nextRun = GetNextRunUtc(DateTime.UtcNow);

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = nextRun - DateTime.UtcNow;

                if (delay > TimeSpan.Zero)
                {
                    _logger.LogInformation($"Next overdue check scheduled for: {nextRun:yyyy-MM-dd HH:mm:ss} UTC (08:00 SAST)");
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

                // Schedule the next run for the following day at 08:00 SAST.
                nextRun = GetNextRunUtc(DateTime.UtcNow);
            }

            _logger.LogInformation("Overdue Reminder Service stopped.");
        }

        // Returns the next 08:00 SAST occurrence (strictly after <paramref name="utcNow"/>) as a UTC timestamp.
        internal static DateTime GetNextRunUtc(DateTime utcNow)
        {
            var nowSast = TimeZoneInfo.ConvertTimeFromUtc(utcNow, SastTimeHelper.SastTimeZone);
            var nextRunSast = nowSast.Date + DailyRunTimeSast;
            if (nowSast >= nextRunSast)
            {
                nextRunSast = nextRunSast.AddDays(1);
            }

            // SAST has no daylight saving, so this conversion is unambiguous year-round.
            return TimeZoneInfo.ConvertTimeToUtc(nextRunSast, SastTimeHelper.SastTimeZone);
        }
    }
}
