using System;
using System.ComponentModel.DataAnnotations;

namespace AssetTracker.API.Models
{
    public class User
    {
        [Key]
        public int UserID { get; set; } // System-generated 

        [Required]
        [StringLength(100)]
        public string FullName { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        [StringLength(256)]
        public string Email { get; set; } = string.Empty; // Unique username 

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [Required]
        [StringLength(100)]
        public string Department { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string EmployeeID { get; set; } = string.Empty; // Unique identifier 

        [StringLength(20)]
        public string? PhoneNumber { get; set; } // Optional phone number

        [Required]
        [StringLength(50)]
        public string Role { get; set; } = "Standard User"; // Standard User, Asset Manager, Admin

        [Required]
        [StringLength(50)]
        public string AccountStatus { get; set; } = "Pending"; // Pending, Active, Inactive, Rejected 

        public DateTime RegistrationDate { get; set; } = DateTime.UtcNow;
        public DateTime? LastLoginDate { get; set; }
        public int? ApprovedBy { get; set; } // Admin user ID 
        public DateTime? ApprovalRejectionDate { get; set; }
        public string? RejectionReason { get; set; } // Captured if account rejected [cite: 51]

        public string? PasswordResetToken { get; set; }
        public DateTime? ResetTokenExpiry { get; set; }

        [Required]
        public bool MustChangePassword { get; set; } = false;
    }
}