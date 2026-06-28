using AssetTracker.API.BackgroundServices;
using FluentAssertions;

namespace AssetTracker.API.Tests.Services
{
    /// <summary>
    /// Tests for the §2.4 daily scheduling math. Reminders must fire at 08:00 SAST
    /// (South Africa Standard Time, UTC+2, no daylight saving), which is 06:00 UTC.
    /// </summary>
    public class OverdueReminderScheduleTests
    {
        [Fact]
        public void GetNextRunUtc_ResolvesTo0600Utc()
        {
            // Arrange - midnight UTC, well before the daily run
            var utcNow = new DateTime(2026, 6, 15, 0, 0, 0, DateTimeKind.Utc);

            // Act
            var nextRun = OverdueReminderService.GetNextRunUtc(utcNow);

            // Assert - 08:00 SAST is 06:00 UTC the same calendar day
            nextRun.Hour.Should().Be(6);
            nextRun.Minute.Should().Be(0);
            nextRun.Date.Should().Be(new DateTime(2026, 6, 15));
        }

        [Fact]
        public void GetNextRunUtc_BeforeDailyRun_SchedulesSameDay()
        {
            // Arrange - 05:00 UTC (07:00 SAST), one hour before the run
            var utcNow = new DateTime(2026, 6, 15, 5, 0, 0, DateTimeKind.Utc);

            // Act
            var nextRun = OverdueReminderService.GetNextRunUtc(utcNow);

            // Assert - still fires today at 06:00 UTC
            nextRun.Should().Be(new DateTime(2026, 6, 15, 6, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void GetNextRunUtc_AfterDailyRun_SchedulesNextDay()
        {
            // Arrange - 07:00 UTC (09:00 SAST), one hour after the run
            var utcNow = new DateTime(2026, 6, 15, 7, 0, 0, DateTimeKind.Utc);

            // Act
            var nextRun = OverdueReminderService.GetNextRunUtc(utcNow);

            // Assert - rolls over to tomorrow at 06:00 UTC
            nextRun.Should().Be(new DateTime(2026, 6, 16, 6, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void GetNextRunUtc_ExactlyAtRunTime_SchedulesNextDay()
        {
            // Arrange - 06:00 UTC is exactly 08:00 SAST; "strictly after" means roll to tomorrow
            var utcNow = new DateTime(2026, 6, 15, 6, 0, 0, DateTimeKind.Utc);

            // Act
            var nextRun = OverdueReminderService.GetNextRunUtc(utcNow);

            // Assert
            nextRun.Should().Be(new DateTime(2026, 6, 16, 6, 0, 0, DateTimeKind.Utc));
        }

        [Fact]
        public void GetNextRunUtc_LateEveningUtc_RollsToNextDay()
        {
            // Arrange - 23:00 UTC (01:00 SAST next day) should target that upcoming SAST morning
            var utcNow = new DateTime(2026, 6, 15, 23, 0, 0, DateTimeKind.Utc);

            // Act
            var nextRun = OverdueReminderService.GetNextRunUtc(utcNow);

            // Assert - 01:00 SAST on the 16th is before 08:00 SAST on the 16th => 06:00 UTC on the 16th
            nextRun.Should().Be(new DateTime(2026, 6, 16, 6, 0, 0, DateTimeKind.Utc));
        }
    }
}
