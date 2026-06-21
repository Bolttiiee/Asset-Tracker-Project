using AssetTracker.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AssetTracker.API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Strict access - Admin only
    public class AdminController : ControllerBase
    {
        private readonly IOverdueReminderProcessor _overdueReminderProcessor;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AdminController> _logger;

        public AdminController(
            IOverdueReminderProcessor overdueReminderProcessor,
            IWebHostEnvironment environment,
            ILogger<AdminController> logger)
        {
            _overdueReminderProcessor = overdueReminderProcessor;
            _environment = environment;
            _logger = logger;
        }

        // POST: api/admin/run-overdue-reminders
        // DEV-ONLY: Manually triggers the overdue-reminder + admin-digest job immediately so
        // reminder emails can be tested without waiting for the daily schedule. Runs the exact
        // same logic as the scheduled job and writes EMAIL_REMINDER / ADMIN_DIGEST audit rows.
        [HttpPost("run-overdue-reminders")]
        public async Task<IActionResult> RunOverdueReminders(CancellationToken cancellationToken)
        {
            // Guard: only expose this endpoint in the Development environment.
            if (!_environment.IsDevelopment())
            {
                return NotFound();
            }

            _logger.LogWarning(
                "Manual overdue-reminder run triggered by {User}.",
                User.Identity?.Name ?? "Unknown");

            var result = await _overdueReminderProcessor.ProcessOverdueRemindersAsync(cancellationToken);

            return Ok(new
            {
                message = "Overdue reminder job executed successfully.",
                overdueCount = result.OverdueCount,
                emailsSent = result.EmailsSent,
                emailsFailed = result.EmailsFailed,
                adminDigestSent = result.AdminDigestSent
            });
        }
    }
}
