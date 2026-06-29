using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AssetTracker.API.Models
{
    public class Checkout
    {
        [Key]
        public int CheckoutID { get; set; } // System-generated 

        [Required]
        public int AssetID { get; set; } // Foreign Key 

        [Required]
        public int UserID { get; set; } // Foreign Key 

        [Required]
        [StringLength(100)]
        public string UserFullName { get; set; } = string.Empty; // Point-in-time snapshot 

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string UserEmail { get; set; } = string.Empty; // Point-in-time snapshot 

        public DateTime CheckoutTimestamp { get; set; } = DateTime.UtcNow;

        [Required]
        public DateTime ExpectedReturnDate { get; set; }

        public DateTime? ActualReturnDate { get; set; }

        [Required]
        [StringLength(50)]
        public string Status { get; set; } = "Active"; // Active, Returned, Overdue 

        public DateTime? LastReminderSentUtc { get; set; }

        /// <summary>
        /// Three-state status described by the data model (Active, Returned, Overdue).
        /// Not persisted: the stored <see cref="Status"/> holds the lifecycle state
        /// (Active/Returned) that internal queries filter on, while this derives "Overdue"
        /// for any active checkout past its expected return date. Flipping the persisted
        /// Status to "Overdue" would break return, duplicate-checkout and my-assets queries
        /// that filter on Status == "Active", so overdue is computed instead.
        /// </summary>
        [NotMapped]
        public string EffectiveStatus =>
            Status == "Active" && ExpectedReturnDate.Date < Services.SastTimeHelper.GetTodaySast()
                ? "Overdue"
                : Status;

        [ForeignKey("AssetID")]
        public Asset? Asset { get; set; }

        [ForeignKey("UserID")]
        public User? User { get; set; }
    }
}