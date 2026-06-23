using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetTracker.API.Data;
using AssetTracker.API.Models;
using AssetTracker.API.Services;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AssetTracker.API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires a valid JWT token
    public class CheckoutsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly IConfiguration _configuration;

        public CheckoutsController(ApplicationDbContext context, IEmailService emailService, IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _configuration = configuration;
        }

        // Number of reminder emails sent concurrently. Each send opens its own SMTP connection,
        // so bounded parallelism is safe. Configurable via "Email:MaxParallelSends" (default 4;
        // set to 1 for sequential sending).
        private int GetMaxParallelSends()
        {
            if (int.TryParse(_configuration["Email:MaxParallelSends"], out var configured) && configured > 0)
            {
                return configured;
            }

            return 4;
        }

        // POST: api/checkouts/assign
        [HttpPost("assign")]
        public async Task<IActionResult> AssignAsset([FromBody] CheckoutDto dto)
        {
            // VALIDATION 1: No asset selected
            if (dto.AssetID <= 0)
            {
                return BadRequest(new { message = "No asset selected. Please select an asset to checkout." });
            }

            // VALIDATION 2: Asset must exist
            var asset = await _context.Assets.FindAsync(dto.AssetID);
            if (asset == null)
            {
                return NotFound(new { message = "Asset not found." });
            }

            // VALIDATION 3: Asset must be Available (not Checked Out, Under Maintenance, or Decommissioned)
            if (asset.Status == "Checked Out")
            {
                return BadRequest(new { message = "Asset is already checked out and not available." });
            }

            if (asset.Status == "Under Maintenance")
            {
                return BadRequest(new { message = "Asset is currently under maintenance and cannot be checked out." });
            }

            if (asset.Status == "Decommissioned")
            {
                return BadRequest(new { message = "Asset has been decommissioned and cannot be checked out." });
            }

            if (asset.Status != "Available")
            {
                return BadRequest(new { message = "Asset is currently unavailable." });
            }

            // VALIDATION 4: Employee email is required
            if (string.IsNullOrWhiteSpace(dto.EmployeeEmail))
            {
                return BadRequest(new { message = "Employee email address is required." });
            }

            // VALIDATION 5: User must exist
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == dto.EmployeeEmail);
            if (user == null)
            {
                return NotFound(new { message = "Employee record not found." });
            }

            // VALIDATION 6: Expected return date validation
            if (!dto.ExpectedReturnDate.HasValue)
            {
                return BadRequest(new { message = "Expected return date is required." });
            }

            if (dto.ExpectedReturnDate.Value.Date < DateTime.UtcNow.Date)
            {
                return BadRequest(new { message = "Expected return date cannot be in the past." });
            }

            // VALIDATION 7: User cannot checkout the same asset twice
            var existingCheckout = await _context.Checkouts
                .FirstOrDefaultAsync(c => c.UserID == user.UserID 
                                       && c.AssetID == dto.AssetID 
                                       && c.Status == "Active");

            if (existingCheckout != null)
            {
                return BadRequest(new { message = "This user already has this asset checked out." });
            }

            // ALL VALIDATIONS PASSED - Proceed with checkout
            asset.Status = "Checked Out";
            asset.AssignedTo = user.FullName;

            var checkout = new Checkout
            {
                AssetID = asset.AssetID,
                UserID = user.UserID,
                UserFullName = user.FullName,
                UserEmail = user.Email,
                CheckoutTimestamp = DateTime.UtcNow,
                ExpectedReturnDate = dto.ExpectedReturnDate.Value,
                Status = "Active"
            };
            _context.Checkouts.Add(checkout);

            // LOG THE AUDIT TRAIL
            var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "System Admin";
            var auditLog = new AuditTrail
            {
                Action = "ASSIGN",
                AssetID = asset.AssetID,
                AssetName = asset.Name,
                PerformedByEmail = adminEmail,
                Timestamp = DateTime.UtcNow,
                Details = $"Hardware checked out to {user.FullName} ({user.Email}). Expected return: {dto.ExpectedReturnDate.Value:yyyy-MM-dd}."
            };
            _context.AuditTrails.Add(auditLog);

            await _context.SaveChangesAsync();

            // If the asset is due today (or earlier) it is already overdue, so send the reminder
            // email immediately instead of waiting for the scheduled sweep. Reuses the same email
            // and EMAIL_REMINDER audit logging as the overdue-reminder job.
            var reminderEmailSent = false;
            if (checkout.ExpectedReturnDate.Date <= DateTime.UtcNow.Date)
            {
                checkout.Asset = asset; // Ensure the email service has asset details available
                try
                {
                    await _emailService.SendOverdueReminderAsync(checkout);

                    var reminderAudit = new AuditTrail
                    {
                        Action = "EMAIL_REMINDER",
                        AssetID = checkout.AssetID,
                        AssetName = asset.Name,
                        PerformedByEmail = adminEmail,
                        Timestamp = DateTime.UtcNow,
                        Details = $"Immediate overdue reminder email sent to {user.FullName} ({user.Email}) on assignment. " +
                                  $"Days overdue: {(DateTime.UtcNow.Date - checkout.ExpectedReturnDate.Date).Days}"
                    };
                    _context.AuditTrails.Add(reminderAudit);
                    await _context.SaveChangesAsync();

                    reminderEmailSent = true;
                }
                catch (Exception ex)
                {
                    // Don't fail the assignment if the email can't be sent; surface it in the response.
                    Console.WriteLine($"Failed to send immediate overdue reminder for asset {asset.AssetID}: {ex.Message}");
                }
            }

            return Ok(new { 
                success = true,
                message = $"Checkout successful! Asset '{asset.Name}' has been assigned to {user.FullName}.",
                checkoutId = checkout.CheckoutID,
                assetName = asset.Name,
                expectedReturnDate = checkout.ExpectedReturnDate,
                reminderEmailSent
            });
        }

        // POST: api/checkouts/return/5
        [HttpPost("return/{assetId}")]
        public async Task<IActionResult> ReturnAsset(int assetId)
        {
            var asset = await _context.Assets.FindAsync(assetId);
            if (asset == null) return NotFound(new { message = "Asset not found." });
            if (asset.Status != "Checked Out") return BadRequest(new { message = "Asset is not checked out." });

            var checkout = await _context.Checkouts.FirstOrDefaultAsync(c => c.AssetID == assetId && c.Status == "Active");

            asset.Status = "Available";
            asset.AssignedTo = string.Empty;

            if (checkout != null)
            {
                checkout.ActualReturnDate = DateTime.UtcNow;
                checkout.Status = "Returned";
            }

            // LOG THE AUDIT TRAIL
            var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "System Admin";
            var auditLog = new AuditTrail
            {
                Action = "RETURN",
                AssetID = asset.AssetID,
                AssetName = asset.Name,
                PerformedByEmail = adminEmail,
                Timestamp = DateTime.UtcNow,
                Details = $"Hardware safely returned to locker storage and cleared of worker assignments."
            };
            _context.AuditTrails.Add(auditLog);

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Asset '{asset.Name}' has been successfully returned to inventory." });
        }

        // POST: api/checkouts/cancel/{assetId}
        // Cancels (voids) the active checkout for an asset and returns it to Available.
        // Keyed by assetId to match the return/force-return endpoints used by the asset management page.
        [HttpPost("cancel/{assetId}")]
        [Authorize(Roles = "Admin,Asset Manager")]
        public async Task<IActionResult> CancelCheckout(int assetId)
        {
            var checkout = await _context.Checkouts
                .Include(c => c.Asset)
                .FirstOrDefaultAsync(c => c.AssetID == assetId && c.Status == "Active");

            if (checkout == null)
            {
                return NotFound(new { message = "No active checkout found for this asset." });
            }

            // Restore asset to available status
            if (checkout.Asset != null)
            {
                checkout.Asset.Status = "Available";
                checkout.Asset.AssignedTo = string.Empty;
            }

            checkout.Status = "Cancelled";
            checkout.ActualReturnDate = DateTime.UtcNow;

            // LOG THE AUDIT TRAIL
            var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "System Admin";
            var auditLog = new AuditTrail
            {
                Action = "CANCEL_CHECKOUT",
                AssetID = checkout.AssetID,
                AssetName = checkout.Asset?.Name ?? "Unknown",
                PerformedByEmail = adminEmail,
                Timestamp = DateTime.UtcNow,
                Details = $"Checkout cancelled. Asset was assigned to {checkout.UserFullName} ({checkout.UserEmail})."
            };
            _context.AuditTrails.Add(auditLog);

            await _context.SaveChangesAsync();

            return Ok(new { 
                success = true,
                message = "Checkout has been successfully cancelled and the asset has been returned to available status." 
            });
        }

        // GET: api/checkouts/my-assets
        [HttpGet("my-assets")]
        public async Task<ActionResult<IEnumerable<object>>> GetMyAssets()
        {
            // 1. Extract the user's email from the JWT token claims
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(userEmail))
            {
                return Unauthorized(new { message = "User identity could not be determined from token." });
            }

            // 2. Find the user in the database
            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == userEmail.ToLower());
            if (user == null)
            {
                return NotFound(new { message = "User account not found." });
            }

            // 3. Get all active checkouts for this user with asset details
            var myCheckouts = await _context.Checkouts
                .Include(c => c.Asset) // Include related asset information
                .Where(c => c.UserID == user.UserID && c.Status == "Active")
                .Select(c => new
                {
                    checkoutID = c.CheckoutID,
                    assetID = c.AssetID,
                    assetName = c.Asset!.Name,
                    category = c.Asset.Category,
                    serialNumber = c.Asset.SerialNumber,
                    checkoutDate = c.CheckoutTimestamp,
                    expectedReturnDate = c.ExpectedReturnDate,
                    isOverdue = c.ExpectedReturnDate < DateTime.UtcNow // Flag overdue assets
                })
                .OrderBy(c => c.expectedReturnDate)
                .ToListAsync();

            return Ok(myCheckouts);
        }

        // GET: api/checkouts/available-assets
        [HttpGet("available-assets")]
        public async Task<ActionResult<IEnumerable<object>>> GetAvailableAssets()
        {
            // Only return assets that are Available for checkout
            var availableAssets = await _context.Assets
                .Where(a => a.Status == "Available")
                .Select(a => new
                {
                    assetID = a.AssetID,
                    name = a.Name,
                    category = a.Category,
                    serialNumber = a.SerialNumber,
                    status = a.Status,
                    value = a.Value,
                    purchaseDate = a.PurchaseDate
                })
                .OrderBy(a => a.category)
                .ThenBy(a => a.name)
                .ToListAsync();

            return Ok(availableAssets);
        }

        // GET: api/checkouts/audit-trail
        [HttpGet("audit-trail")]
        [Authorize(Roles = "Admin")] //  Strict gatekeeping: Only Admins can view the raw history log
        public async Task<ActionResult<IEnumerable<AuditTrail>>> GetAuditTrail(
            [FromQuery] string? action,
            [FromQuery] DateTime? fromDate,
            [FromQuery] DateTime? toDate)
        {
            var query = _context.AuditTrails.AsQueryable();

            if (!string.IsNullOrWhiteSpace(action) && !string.Equals(action, "All", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(a => a.Action == action);
            }

            if (fromDate.HasValue)
            {
                var from = fromDate.Value.Date;
                query = query.Where(a => a.Timestamp >= from);
            }

            if (toDate.HasValue)
            {
                var toExclusive = toDate.Value.Date.AddDays(1);
                query = query.Where(a => a.Timestamp < toExclusive);
            }

            return await query
                .OrderByDescending(a => a.Timestamp) // Show latest actions first
                .ToListAsync();
        }

        // GET: api/checkouts/overdue
        [HttpGet("overdue")]
        [Authorize(Roles = "Admin,Asset Manager")]
        public async Task<ActionResult<IEnumerable<object>>> GetOverdueCheckouts()
        {
            var today = DateTime.UtcNow.Date;
            var overdueCheckouts = await _context.Checkouts
                .Include(c => c.Asset)
                .Include(c => c.User)
                .Where(c => c.Status == "Active" && c.ExpectedReturnDate.Date < today)
                .Select(c => new
                {
                    checkoutID = c.CheckoutID,
                    assetID = c.AssetID,
                    assetName = c.Asset!.Name,
                    serialNumber = c.Asset.SerialNumber,
                    category = c.Asset.Category,
                    userFullName = c.UserFullName,
                    userEmail = c.UserEmail,
                    checkoutDate = c.CheckoutTimestamp,
                    expectedReturnDate = c.ExpectedReturnDate,
                    daysOverdue = (today - c.ExpectedReturnDate.Date).Days
                })
                .OrderByDescending(c => c.daysOverdue)
                .ToListAsync();

            return Ok(overdueCheckouts);
        }

        // POST: api/checkouts/trigger-overdue-reminders
        [HttpPost("trigger-overdue-reminders")]
        [Authorize(Roles = "Admin,Asset Manager")]
        public async Task<IActionResult> TriggerOverdueRemindersNow()
        {
            var isConfigured = !string.IsNullOrWhiteSpace(_configuration["Email:SmtpUser"])
                && !string.IsNullOrWhiteSpace(_configuration["Email:SmtpPassword"]);

            if (!isConfigured)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "SMTP is not configured. Set Email:SmtpUser and Email:SmtpPassword in appsettings before triggering reminders."
                });
            }

            var today = DateTime.UtcNow.Date;
            var overdueCheckouts = await _context.Checkouts
                .Include(c => c.Asset)
                .Include(c => c.User)
                .Where(c => c.Status == "Active" && c.ExpectedReturnDate.Date < today)
                .ToListAsync();

            if (!overdueCheckouts.Any())
            {
                return Ok(new
                {
                    success = true,
                    message = "No overdue checkouts found. No reminder emails were sent.",
                    overdueCount = 0,
                    reminderEmailsSent = 0,
                    reminderEmailsFailed = 0,
                    adminDigestSent = false
                });
            }

            var reminderEmailsSent = 0;
            var reminderEmailsFailed = 0;
            var adminDigestSent = false;

            // Pass 1: send reminders with bounded parallelism. Each send opens its own SMTP
            // connection, so concurrent sends are safe. No DbContext work happens here because
            // DbContext is not thread-safe.
            var sentCheckouts = new ConcurrentBag<Checkout>();
            await Parallel.ForEachAsync(
                overdueCheckouts,
                new ParallelOptions { MaxDegreeOfParallelism = GetMaxParallelSends() },
                async (checkout, ct) =>
                {
                    try
                    {
                        await _emailService.SendOverdueReminderAsync(checkout);
                        sentCheckouts.Add(checkout);
                    }
                    catch
                    {
                        // Failures are tallied from the sent count after the parallel pass.
                    }
                });

            // Pass 2: write audit rows sequentially against the single, non-thread-safe DbContext.
            foreach (var checkout in sentCheckouts)
            {
                var auditLog = new AuditTrail
                {
                    Action = "EMAIL_REMINDER",
                    AssetID = checkout.AssetID,
                    AssetName = checkout.Asset?.Name ?? "Unknown",
                    PerformedByEmail = "system@manual-trigger",
                    Timestamp = DateTime.UtcNow,
                    Details = $"Manual overdue reminder email sent to {checkout.UserFullName} ({checkout.UserEmail}). Days overdue: {(today - checkout.ExpectedReturnDate.Date).Days}"
                };
                _context.AuditTrails.Add(auditLog);
            }

            reminderEmailsSent = sentCheckouts.Count;
            reminderEmailsFailed = overdueCheckouts.Count - sentCheckouts.Count;

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

                var adminAuditLog = new AuditTrail
                {
                    Action = "ADMIN_DIGEST",
                    AssetID = null,
                    AssetName = "System Report",
                    PerformedByEmail = "system@manual-trigger",
                    Timestamp = DateTime.UtcNow,
                    Details = $"Manual overdue digest sent to admin. Total overdue assets: {overdueCheckouts.Count}"
                };
                _context.AuditTrails.Add(adminAuditLog);

                adminDigestSent = true;
            }
            catch
            {
                adminDigestSent = false;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "Manual overdue reminder run completed.",
                overdueCount = overdueCheckouts.Count,
                reminderEmailsSent,
                reminderEmailsFailed,
                adminDigestSent
            });
        }

        // POST: api/checkouts/test-email
        [HttpPost("test-email")]
        [Authorize(Roles = "Admin,Asset Manager")]
        public async Task<IActionResult> TestEmailConnection()
        {
            var isConfigured = !string.IsNullOrWhiteSpace(_configuration["Email:SmtpUser"])
                && !string.IsNullOrWhiteSpace(_configuration["Email:SmtpPassword"]);

            if (!isConfigured)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "SMTP is not configured. Set Email:SmtpUser and Email:SmtpPassword in appsettings before testing."
                });
            }

            var connected = await _emailService.TestEmailConnectionAsync();

            if (!connected)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = "SMTP validation failed. Verify host, port, app password, sender settings, and network access."
                });
            }

            return Ok(new
            {
                success = true,
                message = "SMTP validation succeeded (no test email sent)."
            });
        }
    }

    // Simple Data Transfer Object for incoming requests
    public class CheckoutDto
    {
        public int AssetID { get; set; }
        public string EmployeeEmail { get; set; } = string.Empty;
        public DateTime? ExpectedReturnDate { get; set; } // Required field for expected return date
    }
}
