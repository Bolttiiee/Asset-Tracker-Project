using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetTracker.API.Data;
using AssetTracker.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace AssetTracker.API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Admin")] // Strict access - Admin only for user management
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public UsersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetAllUsers([FromQuery] string? search, [FromQuery] string? department, [FromQuery] string? role, [FromQuery] string? status)
        {
            var query = _context.Users.AsQueryable();

            // Apply search filter (name or email)
            if (!string.IsNullOrWhiteSpace(search))
            {
                var searchLower = search.ToLower();
                query = query.Where(u => u.FullName.ToLower().Contains(searchLower) || 
                                         u.Email.ToLower().Contains(searchLower));
            }

            // Apply department filter
            if (!string.IsNullOrWhiteSpace(department))
            {
                query = query.Where(u => u.Department == department);
            }

            // Apply role filter
            if (!string.IsNullOrWhiteSpace(role))
            {
                query = query.Where(u => u.Role == role);
            }

            // Apply status filter
            if (!string.IsNullOrWhiteSpace(status))
            {
                query = query.Where(u => u.AccountStatus == status);
            }

            var users = await query
                .OrderBy(u => u.FullName)
                .Select(u => new
                {
                    userID = u.UserID,
                    fullName = u.FullName,
                    email = u.Email,
                    department = u.Department,
                    employeeID = u.EmployeeID,
                    phoneNumber = u.PhoneNumber,
                    role = u.Role,
                    accountStatus = u.AccountStatus,
                    registrationDate = u.RegistrationDate,
                    lastLoginDate = u.LastLoginDate,
                    rejectionReason = u.RejectionReason
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetUser(int id)
        {
            var user = await _context.Users.FindAsync(id);

            if (user == null)
            {
                return NotFound(new { message = "User not found." });
            }

            // Get user's current checkouts
            var checkouts = await _context.Checkouts
                .Include(c => c.Asset)
                .Where(c => c.UserID == id && c.Status == "Active")
                .Select(c => new
                {
                    checkoutID = c.CheckoutID,
                    assetName = c.Asset!.Name,
                    serialNumber = c.Asset.SerialNumber,
                    checkoutDate = c.CheckoutTimestamp,
                    expectedReturnDate = c.ExpectedReturnDate,
                    isOverdue = c.ExpectedReturnDate < DateTime.UtcNow
                })
                .ToListAsync();

            var userDetails = new
            {
                userID = user.UserID,
                fullName = user.FullName,
                email = user.Email,
                department = user.Department,
                employeeID = user.EmployeeID,
                phoneNumber = user.PhoneNumber,
                role = user.Role,
                accountStatus = user.AccountStatus,
                registrationDate = user.RegistrationDate,
                lastLoginDate = user.LastLoginDate,
                approvedBy = user.ApprovedBy,
                approvalRejectionDate = user.ApprovalRejectionDate,
                rejectionReason = user.RejectionReason,
                currentCheckouts = checkouts
            };

            return Ok(userDetails);
        }

        // POST: api/users/{id}/approve
        [HttpPost("{id}/approve")]
        public async Task<IActionResult> ApproveUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });

            if (user.AccountStatus != "Pending")
            {
                return BadRequest(new { message = "Only pending accounts can be approved." });
            }

            // Get current admin's ID
            var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == adminEmail!.ToLower());

            user.AccountStatus = "Active";
            user.ApprovedBy = admin?.UserID;
            user.ApprovalRejectionDate = DateTime.UtcNow;

            // LOG THE AUDIT TRAIL
            var auditLog = new AuditTrail
            {
                Action = "APPROVE_USER",
                AssetID = null,
                AssetName = $"User: {user.FullName} ({user.Email})",
                PerformedByEmail = adminEmail ?? "System Admin",
                Timestamp = DateTime.UtcNow,
                Details = $"User account approved. Employee ID: {user.EmployeeID}, Department: {user.Department}"
            };
            _context.AuditTrails.Add(auditLog);

            await _context.SaveChangesAsync();

            return Ok(new { message = $"User '{user.FullName}' has been approved and can now log in." });
        }

        // POST: api/users/{id}/reject
        [HttpPost("{id}/reject")]
        public async Task<IActionResult> RejectUser(int id, [FromBody] RejectUserDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });

            if (user.AccountStatus != "Pending")
            {
                return BadRequest(new { message = "Only pending accounts can be rejected." });
            }

            if (string.IsNullOrWhiteSpace(dto.RejectionReason))
            {
                return BadRequest(new { message = "Rejection reason is required." });
            }

            // Get current admin's ID
            var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value;
            var admin = await _context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == adminEmail!.ToLower());

            user.AccountStatus = "Rejected";
            user.RejectionReason = dto.RejectionReason;
            user.ApprovedBy = admin?.UserID;
            user.ApprovalRejectionDate = DateTime.UtcNow;

            // LOG THE AUDIT TRAIL
            var auditLog = new AuditTrail
            {
                Action = "REJECT_USER",
                AssetID = null,
                AssetName = $"User: {user.FullName} ({user.Email})",
                PerformedByEmail = adminEmail ?? "System Admin",
                Timestamp = DateTime.UtcNow,
                Details = $"User account rejected. Reason: {dto.RejectionReason}"
            };
            _context.AuditTrails.Add(auditLog);

            await _context.SaveChangesAsync();

            return Ok(new { message = $"User '{user.FullName}' has been rejected." });
        }

        // PUT: api/users/{id}/status
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateUserStatus(int id, [FromBody] UpdateStatusDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });

            if (user.AccountStatus == "Pending")
            {
                return BadRequest(new { message = "Please approve or reject pending accounts before changing status." });
            }

            var previousStatus = user.AccountStatus;

            // Toggle between Active and Inactive
            if (dto.Activate)
            {
                user.AccountStatus = "Active";
            }
            else
            {
                user.AccountStatus = "Inactive";
            }

            // LOG THE AUDIT TRAIL
            var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "System Admin";
            var auditLog = new AuditTrail
            {
                Action = dto.Activate ? "ACTIVATE_USER" : "DEACTIVATE_USER",
                AssetID = null,
                AssetName = $"User: {user.FullName} ({user.Email})",
                PerformedByEmail = adminEmail,
                Timestamp = DateTime.UtcNow,
                Details = $"User account status changed from '{previousStatus}' to '{user.AccountStatus}'."
            };
            _context.AuditTrails.Add(auditLog);

            await _context.SaveChangesAsync();

            return Ok(new { message = $"User '{user.FullName}' account status updated to '{user.AccountStatus}'." });
        }

        // PUT: api/users/{id}/role
        [HttpPut("{id}/role")]
        public async Task<IActionResult> UpdateUserRole(int id, [FromBody] UpdateRoleDto dto)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });

            var validRoles = new[] { "Standard User", "Asset Manager", "Admin" };
            if (!validRoles.Contains(dto.NewRole))
            {
                return BadRequest(new { message = "Invalid role. Must be: Standard User, Asset Manager, or Admin." });
            }

            var previousRole = user.Role;
            user.Role = dto.NewRole;

            // LOG THE AUDIT TRAIL
            var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "System Admin";
            var auditLog = new AuditTrail
            {
                Action = "CHANGE_USER_ROLE",
                AssetID = null,
                AssetName = $"User: {user.FullName} ({user.Email})",
                PerformedByEmail = adminEmail,
                Timestamp = DateTime.UtcNow,
                Details = $"User role changed from '{previousRole}' to '{dto.NewRole}'."
            };
            _context.AuditTrails.Add(auditLog);

            await _context.SaveChangesAsync();

            return Ok(new { message = $"User '{user.FullName}' role updated to '{dto.NewRole}'." });
        }

        // POST: api/users/{id}/reset-password
        [HttpPost("{id}/reset-password")]
        public async Task<IActionResult> ResetUserPassword(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound(new { message = "User not found." });

            // Generate a temporary password
            var temporaryPassword = GenerateTemporaryPassword();

            // Hash the temporary password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(temporaryPassword);

            // LOG THE AUDIT TRAIL
            var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? "System Admin";
            var auditLog = new AuditTrail
            {
                Action = "RESET_PASSWORD",
                AssetID = null,
                AssetName = $"User: {user.FullName} ({user.Email})",
                PerformedByEmail = adminEmail,
                Timestamp = DateTime.UtcNow,
                Details = $"Password reset by administrator. Temporary password generated."
            };
            _context.AuditTrails.Add(auditLog);

            await _context.SaveChangesAsync();

            return Ok(new 
            { 
                message = $"Password reset for user '{user.FullName}'. User must change password on next login.",
                temporaryPassword = temporaryPassword // In production, send via email instead
            });
        }

        // Helper method to generate temporary password
        private string GenerateTemporaryPassword()
        {
            const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%";
            var random = new Random();
            var password = new char[12];

            // Ensure complexity requirements
            password[0] = "ABCDEFGHJKLMNPQRSTUVWXYZ"[random.Next(26)]; // Uppercase
            password[1] = "23456789"[random.Next(8)]; // Number
            password[2] = "!@#$%"[random.Next(5)]; // Special char

            // Fill remaining with random chars
            for (int i = 3; i < 12; i++)
            {
                password[i] = chars[random.Next(chars.Length)];
            }

            // Shuffle the array
            return new string(password.OrderBy(x => random.Next()).ToArray());
        }
    }

    // DTOs for user management
    public class RejectUserDto
    {
        public string RejectionReason { get; set; } = string.Empty;
    }

    public class UpdateStatusDto
    {
        public bool Activate { get; set; } // true = activate, false = deactivate
    }

    public class UpdateRoleDto
    {
        public string NewRole { get; set; } = string.Empty;
    }
}
