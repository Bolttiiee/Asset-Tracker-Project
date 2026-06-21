using System;
using System.ComponentModel.DataAnnotations;

namespace AssetTracker.API.Models
{
    public class AuditTrail
    {
        [Key]
        public int AuditLogID { get; set; }

        [Required]
        [StringLength(50)]
        public string Action { get; set; } = string.Empty; // e.g., "PROVISION", "ASSIGN", "RETURN", "DECOMMISSION"

        public int? AssetID { get; set; } // Nullable - not all actions are asset-related (e.g., user management)

        [Required]
        [StringLength(100)]
        public string AssetName { get; set; } = string.Empty;

        [Required]
        [StringLength(256)]
        public string PerformedByEmail { get; set; } = string.Empty; // Extracted directly from the JWT claims

        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        [Required]
        public string Details { get; set; } = string.Empty;
    }
}