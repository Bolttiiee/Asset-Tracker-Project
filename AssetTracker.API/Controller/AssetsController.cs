using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AssetTracker.API.Data;
using AssetTracker.API.Models;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace AssetTracker.API.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize] // Requires a valid JWT token badge for ANY interaction
    public class AssetsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public AssetsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // 1. GET: api/assets (Accessible by all logged-in employees)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<Asset>>> GetAssets()
        {
            return await _context.Assets.ToListAsync();
        }

        // 2. GET: api/assets/5 (Fetch specific asset details)
        [HttpGet("{id}")]
        public async Task<ActionResult<Asset>> GetAsset(int id)
        {
            var asset = await _context.Assets.FindAsync(id);

            if (asset == null)
            {
                return NotFound(new { message = "Asset not found." });
            }

            return asset;
        }

        // 3. POST: api/assets (STRICT ROLE ACCESS: Admin or Manager only)
        [HttpPost]
        [Authorize(Roles = "Admin,Asset Manager")]
        public async Task<ActionResult<Asset>> PostAsset([FromBody] Asset asset)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            // DUPLICATE SERIAL NUMBER CHECK - Check across ALL assets including decommissioned
            var existingAsset = await _context.Assets
                .FirstOrDefaultAsync(a => a.SerialNumber.ToLower() == asset.SerialNumber.ToLower());

            if (existingAsset != null)
            {
                return BadRequest(new { 
                    message = $"Duplicate serial number detected. An asset with serial number '{asset.SerialNumber}' already exists " +
                              $"(Asset ID: {existingAsset.AssetID}, Name: {existingAsset.Name}, Status: {existingAsset.Status})." 
                });
            }

            // System-set the creation timestamp; a newly provisioned asset is never decommissioned.
            asset.DateAdded = DateTime.UtcNow;
            asset.DecommissionDate = null;

            _context.Assets.Add(asset);
            await _context.SaveChangesAsync(); // Saves asset first to generate AssetID

            // LOG THE AUDIT TRAIL
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "System Admin";
            var auditLog = new AuditTrail
            {
                Action = "PROVISION",
                AssetID = asset.AssetID,
                AssetName = asset.Name,
                PerformedByEmail = userEmail,
                Timestamp = DateTime.UtcNow,
                Details = $"Asset initialized with serial {asset.SerialNumber} and valuation of ${asset.Value}."
            };
            _context.AuditTrails.Add(auditLog);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetAsset), new { id = asset.AssetID }, asset);
        }

        // 4. PUT: api/assets/5 (EDIT ACCESS: Admin or Manager only)
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin,Asset Manager")]
        public async Task<IActionResult> PutAsset(int id, [FromBody] Asset asset)
        {
            if (id != asset.AssetID)
            {
                return BadRequest(new { message = "ID mismatch error." });
            }

            // CRITICAL SECURITY: Prevent direct status changes via PUT
            // Status should ONLY change through specific endpoints: checkout, return, decommission, restore, maintenance
            var existingAsset = await _context.Assets.AsNoTracking().FirstOrDefaultAsync(a => a.AssetID == id);
            if (existingAsset == null)
            {
                return NotFound(new { message = "Asset no longer exists." });
            }

            if (existingAsset.Status != asset.Status)
            {
                return BadRequest(new { 
                    message = "Status cannot be changed directly via PUT. Use the appropriate endpoint: " +
                              "checkout (POST /api/checkouts/assign), return (POST /api/checkouts/return/{assetId}), " +
                              "decommission (DELETE /api/assets/{id}), restore (POST /api/assets/{id}/restore), " +
                              "or maintenance (PUT /api/assets/{id}/maintenance)." 
                });
            }

            // DUPLICATE SERIAL NUMBER CHECK (if serial number is being changed)
            if (existingAsset.SerialNumber != asset.SerialNumber)
            {
                var duplicateSerial = await _context.Assets
                    .FirstOrDefaultAsync(a => a.SerialNumber.ToLower() == asset.SerialNumber.ToLower() && a.AssetID != id);

                if (duplicateSerial != null)
                {
                    return BadRequest(new { 
                        message = $"Duplicate serial number detected. An asset with serial number '{asset.SerialNumber}' already exists " +
                                  $"(Asset ID: {duplicateSerial.AssetID}, Name: {duplicateSerial.Name})." 
                    });
                }
            }

            // Preserve system-managed fields so an edit payload cannot overwrite them.
            asset.DateAdded = existingAsset.DateAdded;
            asset.DecommissionDate = existingAsset.DecommissionDate;

            _context.Entry(asset).State = EntityState.Modified;

            try
            {
                await _context.SaveChangesAsync();

                // LOG THE AUDIT TRAIL for edits
                var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "System Admin";
                var auditLog = new AuditTrail
                {
                    Action = "EDIT_ASSET",
                    AssetID = asset.AssetID,
                    AssetName = asset.Name,
                    PerformedByEmail = userEmail,
                    Timestamp = DateTime.UtcNow,
                    Details = $"Asset details updated. Serial: {asset.SerialNumber}, Category: {asset.Category}, Value: ${asset.Value}"
                };
                _context.AuditTrails.Add(auditLog);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AssetExists(id))
                {
                    return NotFound(new { message = "Asset no longer exists." });
                }
                throw;
            }

            return Ok(new { message = "Asset information updated successfully." });
        }

        // 5. DELETE: api/assets/5 (SYSTEM DECOMMISSION: Admin or Manager only)
        // NOTE: This performs a SOFT DELETE by changing status to "Decommissioned" rather than removing from database
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin,Asset Manager")]
        public async Task<IActionResult> DeleteAsset(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null) return NotFound(new { message = "Asset record not found." });

            if (asset.Status == "Decommissioned")
            {
                return BadRequest(new { message = "Asset is already decommissioned." });
            }

            if (asset.Status == "Checked Out")
            {
                return BadRequest(new { message = "Cannot decommission a checked-out asset. Please return it first or use admin override." });
            }

            // Soft delete - change status to Decommissioned instead of removing from database
            var previousStatus = asset.Status;
            asset.Status = "Decommissioned";
            asset.AssignedTo = string.Empty;
            asset.DecommissionDate = DateTime.UtcNow;

            // LOG THE AUDIT TRAIL
            var userEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "System Admin";
            var auditLog = new AuditTrail
            {
                Action = "DECOMMISSION",
                AssetID = asset.AssetID,
                AssetName = asset.Name,
                PerformedByEmail = userEmail,
                Timestamp = DateTime.UtcNow,
                Details = $"Asset decommissioned. Previous status: {previousStatus}. Asset retained in system for historical records."
            };
            _context.AuditTrails.Add(auditLog);

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Asset '{asset.Name}' has been successfully decommissioned." });
        }

        private bool AssetExists(int id)
        {
            return _context.Assets.Any(e => e.AssetID == id);
        }

        // GET: api/assets/dashboard-metrics
        [HttpGet("dashboard-metrics")]
        [Authorize(Roles = "Admin,Asset Manager")] // Strict access lock for stakeholders
        public async Task<IActionResult> GetDashboardMetrics()
        {
            // 1. Pull down the total inventory list
            var allAssets = await _context.Assets.ToListAsync();

            // 2. Execute lightning-fast server-side calculations
            var totalCount = allAssets.Count;
            var totalValuation = allAssets.Sum(a => a.Value);
            var averageAssetCost = totalCount > 0 ? allAssets.Average(a => a.Value) : 0;

            // 3. Segment status counts for distribution analysis
            var availableCount = allAssets.Count(a => a.Status == "Available");
            var checkedOutCount = allAssets.Count(a => a.Status == "Checked Out");
            var maintenanceCount = allAssets.Count(a => a.Status == "Under Maintenance");
            var decommissionedCount = allAssets.Count(a => a.Status == "Decommissioned");

            // 4. Return a perfectly structured metric package
            return Ok(new
            {
                TotalAssets = totalCount,
                TotalValuation = totalValuation,
                AverageAssetCost = Math.Round(averageAssetCost, 2),
                Distribution = new {
                    Available = availableCount,
                    CheckedOut = checkedOutCount,
                    UnderMaintenance = maintenanceCount,
                    Decommissioned = decommissionedCount
                }
            });
        }

        // POST: api/assets/{id}/admin-return
        [HttpPost("{id}/admin-return")]
        [Authorize(Roles = "Admin,Asset Manager")] // Admin override authority
        public async Task<IActionResult> AdminOverrideReturn(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null) return NotFound(new { message = "Asset not found." });

            if (asset.Status != "Checked Out")
            {
                return BadRequest(new { message = "Asset is not currently checked out." });
            }

            // Find the active checkout record
            var checkout = await _context.Checkouts
                .FirstOrDefaultAsync(c => c.AssetID == id && c.Status == "Active");

            // Capture user info before clearing
            var previousAssignee = asset.AssignedTo;

            // Force return the asset
            asset.Status = "Available";
            asset.AssignedTo = string.Empty;

            // Close the checkout record
            if (checkout != null)
            {
                checkout.ActualReturnDate = DateTime.UtcNow;
                checkout.Status = "Returned";
            }

            // LOG THE AUDIT TRAIL
            var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "System Admin";
            var auditLog = new AuditTrail
            {
                Action = "ADMIN_OVERRIDE_RETURN",
                AssetID = asset.AssetID,
                AssetName = asset.Name,
                PerformedByEmail = adminEmail,
                Timestamp = DateTime.UtcNow,
                Details = $"Asset forcibly returned by admin. Previously assigned to: {previousAssignee}"
            };
            _context.AuditTrails.Add(auditLog);

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Asset '{asset.Name}' has been forcibly returned to inventory by admin override." });
        }

        // POST: api/assets/{id}/restore
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Admin,Asset Manager")] // Restoration authority
        public async Task<IActionResult> RestoreDecommissionedAsset(int id)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null) return NotFound(new { message = "Asset not found." });

            if (asset.Status != "Decommissioned")
            {
                return BadRequest(new { message = "Only decommissioned assets can be restored." });
            }

            // Restore asset to available status
            asset.Status = "Available";
            asset.AssignedTo = string.Empty;
            asset.DecommissionDate = null;

            // LOG THE AUDIT TRAIL
            var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "System Admin";
            var auditLog = new AuditTrail
            {
                Action = "RESTORE",
                AssetID = asset.AssetID,
                AssetName = asset.Name,
                PerformedByEmail = adminEmail,
                Timestamp = DateTime.UtcNow,
                Details = $"Decommissioned asset restored to active inventory and marked as Available."
            };
            _context.AuditTrails.Add(auditLog);

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Asset '{asset.Name}' has been successfully restored to active inventory." });
        }

        // PUT: api/assets/{id}/maintenance
        [HttpPut("{id}/maintenance")]
        [Authorize(Roles = "Admin,Asset Manager")] // Status change authority
        public async Task<IActionResult> SetMaintenanceStatus(int id, [FromBody] MaintenanceDto dto)
        {
            var asset = await _context.Assets.FindAsync(id);
            if (asset == null) return NotFound(new { message = "Asset not found." });

            var previousStatus = asset.Status;

            // Toggle between Under Maintenance and Available
            if (dto.SetMaintenance)
            {
                if (asset.Status == "Checked Out")
                {
                    return BadRequest(new { message = "Cannot set a checked-out asset to maintenance. Please return it first or use admin override." });
                }

                asset.Status = "Under Maintenance";
            }
            else
            {
                if (asset.Status != "Under Maintenance")
                {
                    return BadRequest(new { message = "Asset is not currently under maintenance." });
                }

                asset.Status = "Available";
            }

            // LOG THE AUDIT TRAIL
            var adminEmail = User.FindFirst(ClaimTypes.Email)?.Value ?? User.Identity?.Name ?? "System Admin";
            var auditLog = new AuditTrail
            {
                Action = dto.SetMaintenance ? "SET_MAINTENANCE" : "CLEAR_MAINTENANCE",
                AssetID = asset.AssetID,
                AssetName = asset.Name,
                PerformedByEmail = adminEmail,
                Timestamp = DateTime.UtcNow,
                Details = $"Asset status changed from '{previousStatus}' to '{asset.Status}'."
            };
            _context.AuditTrails.Add(auditLog);

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Asset '{asset.Name}' status updated to '{asset.Status}'." });
        }
    }

    // DTO for maintenance status toggle
    public class MaintenanceDto
    {
        public bool SetMaintenance { get; set; } // true = set to maintenance, false = return to available
    }
}
