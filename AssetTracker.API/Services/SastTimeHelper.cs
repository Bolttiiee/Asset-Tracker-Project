using System;

namespace AssetTracker.API.Services
{
    public static class SastTimeHelper
    {
        public static readonly TimeZoneInfo SastTimeZone = ResolveSastTimeZone();

        public static DateTime GetNowSast()
        {
            return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, SastTimeZone);
        }

        public static DateTime GetTodaySast()
        {
            return GetNowSast().Date;
        }

        private static TimeZoneInfo ResolveSastTimeZone()
        {
            foreach (var id in new[] { "South Africa Standard Time", "Africa/Johannesburg" })
            {
                try
                {
                    return TimeZoneInfo.FindSystemTimeZoneById(id);
                }
                catch (TimeZoneNotFoundException) { }
                catch (InvalidTimeZoneException) { }
            }

            // Fallback to UTC+2 custom time zone if system zone is missing (standard SAST has no DST)
            return TimeZoneInfo.CreateCustomTimeZone("SAST", TimeSpan.FromHours(2), "South Africa Standard Time", "SAST");
        }
    }
}
