using AssetTracker.API.Models;

namespace AssetTracker.API.Services
{
    public interface IEmailService
    {
        Task SendOverdueReminderAsync(Checkout checkout);
        Task SendAdminOverdueDigestAsync(List<OverdueCheckoutInfo> overdueCheckouts);
        Task SendPasswordResetEmailAsync(string toEmail, string resetLink);
        Task<bool> TestEmailConnectionAsync();
    }

    public class OverdueCheckoutInfo
    {
        public string UserFullName { get; set; } = string.Empty;
        public string UserEmail { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string SerialNumber { get; set; } = string.Empty;
        public DateTime CheckoutDate { get; set; }
        public DateTime ExpectedReturnDate { get; set; }
        public int DaysOverdue { get; set; }
    }
}
