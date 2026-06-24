using System.ComponentModel.DataAnnotations;

namespace AssetTracker.API.Models
{
    // Payload for when a user clicks "Forgot Password"
    public class ForgotPasswordDto
    {
        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
    }

    // Payload for when a user submits their final new password
    public class ResetPasswordDto
    {
        [Required]
        public string Token { get; set; } = string.Empty;

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long.")]
        public string NewPassword { get; set; } = string.Empty;
    }

    // Payload for when an authenticated user changes their password
    public class ChangePasswordDto
    {
        [Required]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "Password must be at least 8 characters long.")]
        public string NewPassword { get; set; } = string.Empty;
    }
}
