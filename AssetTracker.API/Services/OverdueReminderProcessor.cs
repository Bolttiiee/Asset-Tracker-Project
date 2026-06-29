using AssetTracker.API.Data;
using AssetTracker.API.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace AssetTracker.API.Services
{
    /// <summary>
    /// Summary of an overdue-reminder processing run.
    /// </summary>
    public class OverdueReminderResult
    {
        public int OverdueCount { get; set; }
        public int EmailsSent { get; set; }
        public int EmailsFailed { get; set; }
        public bool AdminDigestSent { get; set; }
    }

    public interface IOverdueReminderProcessor
    {
        /// <summary>
        /// Sends overdue reminder emails and the admin digest, writing EMAIL_REMINDER and
        /// ADMIN_DIGEST rows to the audit trail. Shared by the scheduled background job and
        /// the manual admin trigger so both run identical logic.
        /// </summary>
        Task<OverdueReminderResult> ProcessOverdueRemindersAsync(CancellationToken cancellationToken = default);
    }

    public class OverdueReminderProcessor : IOverdueReminderProcessor
    {
        // Default number of reminder emails sent concurrently. Each send opens its own SMTP
        // connection, so bounded parallelism is safe while the DbContext stays single-threaded.
        // Override via the "Email:MaxParallelSends" configuration value (set to 1 for the
        // original sequential behaviour).
        private const int DefaultMaxParallelSends = 4;

        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<OverdueReminderProcessor> _logger;
        private readonly IConfiguration? _configuration;

        public OverdueReminderProcessor(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<OverdueReminderProcessor> logger,
            IConfiguration? configuration = null)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }

        private int GetMaxParallelSends()
        {
            if (int.TryParse(_configuration?["Email:MaxParallelSends"], out var configured) && configured > 0)
            {
                return configured;
            }

            return DefaultMaxParallelSends;
        }

        public async Task<OverdueReminderResult> ProcessOverdueRemindersAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Starting overdue reminders processing...");

            var result = new OverdueReminderResult();

            // Get all overdue checkouts (an item due today is treated as overdue)
            var today = SastTimeHelper.GetTodaySast();
            var nowSast = SastTimeHelper.GetNowSast();
            var isPastEightSast = nowSast.TimeOfDay >= TimeSpan.FromHours(8);
            var todayStartSast = nowSast.Date;
            var todayStartUtc = TimeZoneInfo.ConvertTimeToUtc(todayStartSast, SastTimeHelper.SastTimeZone);

            var overdueCheckouts = await _context.Checkouts
                .Include(c => c.Asset)
                .Include(c => c.User)
                .Where(c => c.Status == "Active" && c.ExpectedReturnDate.Date <= today &&
                            (c.LastReminderSentUtc == null || (isPastEightSast && c.LastReminderSentUtc.Value < todayStartUtc)))
                .ToListAsync(cancellationToken);

            result.OverdueCount = overdueCheckouts.Count;
            _logger.LogInformation($"Found {overdueCheckouts.Count} overdue checkout(s).");

            // Pass 1: send reminders with bounded parallelism. Each SendOverdueReminderAsync opens
            // its own SMTP connection, so concurrent sends are safe here. Crucially, no DbContext
            // work happens inside this loop because DbContext is not thread-safe. ILogger is.
            var sentCheckouts = new ConcurrentBag<Checkout>();
            await Parallel.ForEachAsync(
                overdueCheckouts,
                new ParallelOptions
                {
                    MaxDegreeOfParallelism = GetMaxParallelSends(),
                    CancellationToken = cancellationToken
                },
                async (checkout, ct) =>
                {
                    try
                    {
                        await _emailService.SendOverdueReminderAsync(checkout);
                        sentCheckouts.Add(checkout);
                        _logger.LogInformation($"Sent overdue reminder for asset {checkout.AssetID} to {checkout.UserEmail}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Failed to send overdue reminder for checkout {checkout.CheckoutID}");
                    }
                });

            // Pass 2: write audit rows and tally counters sequentially against the single,
            // non-thread-safe DbContext.
            foreach (var checkout in sentCheckouts)
            {
                checkout.LastReminderSentUtc = DateTime.UtcNow;

                var auditLog = new AuditTrail
                {
                    Action = "EMAIL_REMINDER",
                    AssetID = checkout.AssetID,
                    AssetName = checkout.Asset?.Name ?? "Unknown",
                    PerformedByEmail = "system@automated",
                    Timestamp = DateTime.UtcNow,
                    Details = $"Overdue reminder email sent to {checkout.UserFullName} ({checkout.UserEmail}). " +
                              $"Days overdue: {(today - checkout.ExpectedReturnDate.Date).Days}"
                };
                _context.AuditTrails.Add(auditLog);
            }

            result.EmailsSent = sentCheckouts.Count;
            result.EmailsFailed = overdueCheckouts.Count - sentCheckouts.Count;

            // Send admin digest if there are overdue checkouts
            if (overdueCheckouts.Any())
            {
                try
                {
                    var overdueInfo = overdueCheckouts.Select(c => new OverdueCheckoutInfo
                    {
                        UserFullName = c.UserFullName,
                        UserEmail = c.UserEmail,
                        AssetName = c.Asset?.Name ?? "Unknown",
                        SerialNumber = c.Asset?.SerialNumber ?? "Unknown",
                        CheckoutDate = c.CheckoutTimestamp,
                        ExpectedReturnDate = c.ExpectedReturnDate,
                        DaysOverdue = (today - c.ExpectedReturnDate.Date).Days
                    }).ToList();

                    await _emailService.SendAdminOverdueDigestAsync(overdueInfo);

                    // Log admin digest sent
                    var adminAuditLog = new AuditTrail
                    {
                        Action = "ADMIN_DIGEST",
                        AssetID = null,
                        AssetName = "System Report",
                        PerformedByEmail = "system@automated",
                        Timestamp = DateTime.UtcNow,
                        Details = $"Daily overdue digest sent to admin. Total overdue assets: {overdueCheckouts.Count}"
                    };
                    _context.AuditTrails.Add(adminAuditLog);

                    result.AdminDigestSent = true;
                    _logger.LogInformation("Admin overdue digest sent successfully.");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to send admin overdue digest");
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation($"Overdue reminders processing completed. Sent: {result.EmailsSent}, Failed: {result.EmailsFailed}");

            return result;
        }
    }
}
