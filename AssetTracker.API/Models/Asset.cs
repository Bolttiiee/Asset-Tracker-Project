using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetTracker.API.Models
{
    public class Asset
    {
        [Key]
        public int AssetID { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Category { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string SerialNumber { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Available";

        [Required]
        [StringLength(50)]
        public string Condition { get; set; } = "Good"; // New, Good, Fair, Poor

        [StringLength(100)]
        public string? AssignedTo { get; set; } = string.Empty;

        public DateTime PurchaseDate { get; set; } = DateTime.UtcNow;

        public decimal Value { get; set; }

        public DateTime DateAdded { get; set; } = DateTime.UtcNow; // System-set when the record is created

        public DateTime? DecommissionDate { get; set; } // Set when Status becomes Decommissioned, cleared on restore

        public string? Notes { get; set; } // Optional notes / description
    }
}