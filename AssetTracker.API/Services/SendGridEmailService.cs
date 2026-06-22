using AssetTracker.API.Models;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace AssetTracker.API.Services
{
    /// <summary>
    /// Email service implementation backed by the SendGrid API.
    /// Activated when Email:Provider = "SendGrid" is set in configuration.
    /// Credentials required: Email:SendGridApiKey (set via environment variable Email__SendGridApiKey).
    /// </summary>
    public class SendGridEmailService : IEmailService
    {
        private readonly string _apiKey;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _adminEmail;
        private readonly ILogger<SendGridEmailService> _logger;

        public SendGridEmailService(IConfiguration configuration, ILogger<SendGridEmailService> logger)
        {
            _logger = logger;
            _apiKey     = configuration["Email:SendGridApiKey"] ?? string.Empty;
            _fromEmail  = configuration["Email:FromEmail"]      ?? string.Empty;
            _fromName   = configuration["Email:FromName"]       ?? "Asset Tracker System";
            _adminEmail = configuration["Email:AdminEmail"]     ?? string.Empty;
        }

        // ── Individual overdue reminder ────────────────────────────────────────

        public async Task SendOverdueReminderAsync(Checkout checkout)
        {
            if (checkout.Asset == null)
                throw new ArgumentException("Checkout must include Asset information");

            var subject = $"REMINDER: Asset '{checkout.Asset.Name}' is overdue for return";

            var body = $@"
<html>
<body style='font-family: Arial, sans-serif; color: #0f172a;'>
    <h2 style='color: #d9534f;'>Asset Return Overdue</h2>
    <p>Dear {checkout.UserFullName},</p>
    <p>This is a reminder that the following asset is overdue for return:</p>

    <div style='background:#f5f5f5; padding:15px; border-left:4px solid #d9534f; margin:20px 0;'>
        <p><strong>Asset Name:</strong> {checkout.Asset.Name}</p>
        <p><strong>Serial Number:</strong> {checkout.Asset.SerialNumber}</p>
        <p><strong>Checkout Date:</strong> {checkout.CheckoutTimestamp:yyyy-MM-dd}</p>
        <p><strong>Expected Return Date:</strong> {checkout.ExpectedReturnDate:yyyy-MM-dd}</p>
        <p><strong>Days Overdue:</strong> {(DateTime.UtcNow.Date - checkout.ExpectedReturnDate.Date).Days}</p>
    </div>

    <p>Please return this asset as soon as possible. If you have already returned it, please disregard this message.</p>
    <p>If you need an extension or have any questions, please contact your asset manager.</p>
    <p>Thank you,<br/>Asset Tracker System</p>
</body>
</html>";

            await SendAsync(checkout.UserEmail, subject, body);
        }

        // ── Admin daily digest ─────────────────────────────────────────────────

        public async Task SendAdminOverdueDigestAsync(List<OverdueCheckoutInfo> overdueCheckouts)
        {
            if (!overdueCheckouts.Any()) return;

            var subject = $"Daily Overdue Assets Digest — {overdueCheckouts.Count} asset(s) overdue";

            var tableRows = string.Join("\n", overdueCheckouts.Select(o => $@"
<tr>
    <td style='padding:8px; border:1px solid #ddd;'>{o.UserFullName}</td>
    <td style='padding:8px; border:1px solid #ddd;'>{o.UserEmail}</td>
    <td style='padding:8px; border:1px solid #ddd;'>{o.AssetName}</td>
    <td style='padding:8px; border:1px solid #ddd;'>{o.SerialNumber}</td>
    <td style='padding:8px; border:1px solid #ddd;'>{o.CheckoutDate:yyyy-MM-dd}</td>
    <td style='padding:8px; border:1px solid #ddd;'>{o.ExpectedReturnDate:yyyy-MM-dd}</td>
    <td style='padding:8px; border:1px solid #ddd; font-weight:bold; color:#d9534f;'>{o.DaysOverdue}</td>
</tr>"));

            var body = $@"
<html>
<body style='font-family: Arial, sans-serif; color: #0f172a;'>
    <h2 style='color:#d9534f;'>Daily Overdue Assets Report</h2>
    <p>The following {overdueCheckouts.Count} asset(s) are currently overdue:</p>
    <table style='border-collapse:collapse; width:100%; margin:20px 0;'>
        <thead>
            <tr style='background:#f5f5f5;'>
                <th style='padding:8px; border:1px solid #ddd; text-align:left;'>User Name</th>
                <th style='padding:8px; border:1px solid #ddd; text-align:left;'>Email</th>
                <th style='padding:8px; border:1px solid #ddd; text-align:left;'>Asset Name</th>
                <th style='padding:8px; border:1px solid #ddd; text-align:left;'>Serial Number</th>
                <th style='padding:8px; border:1px solid #ddd; text-align:left;'>Checkout Date</th>
                <th style='padding:8px; border:1px solid #ddd; text-align:left;'>Expected Return</th>
                <th style='padding:8px; border:1px solid #ddd; text-align:left;'>Days Overdue</th>
            </tr>
        </thead>
        <tbody>
            {tableRows}
        </tbody>
    </table>
    <p>Please follow up with users who have overdue assets.</p>
    <p>Asset Tracker System</p>
</body>
</html>";

            await SendAsync(_adminEmail, subject, body);
        }

        // ── Password reset ─────────────────────────────────────────────────────

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var subject = "Asset Tracker Password Reset Request";
            var body = $@"
<html>
<body style='font-family: Arial, sans-serif; color: #0f172a;'>
    <h2 style='color:#2b6cb0;'>Password Reset Request</h2>
    <p>We received a request to reset your Asset Tracker account password.</p>
    <p>Use the secure link below to set a new password. This link expires in 1 hour.</p>
    <p style='margin:24px 0;'>
        <a href='{resetLink}' style='background:#2b6cb0; color:#fff; padding:10px 16px; text-decoration:none; border-radius:4px;'>
            Reset Password
        </a>
    </p>
    <p>If the button above does not work, copy and paste this URL into your browser:</p>
    <p><a href='{resetLink}'>{resetLink}</a></p>
    <p>If you did not request this change, you can safely ignore this email.</p>
    <p>Asset Tracker System</p>
</body>
</html>";

            await SendAsync(toEmail, subject, body);
        }

        // ── SMTP connectivity check — validates the API key via the SendGrid API ─

        public async Task<bool> TestEmailConnectionAsync()
        {
            if (string.IsNullOrWhiteSpace(_apiKey)) return false;

            try
            {
                var client = new SendGridClient(_apiKey);
                // A lightweight call: retrieve stats for today — it succeeds if the key is valid.
                var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
                var response = await client.RequestAsync(
                    SendGridClient.Method.GET,
                    urlPath: $"stats?start_date={today}"
                );
                // 200 = key is valid; 401 = bad key; anything else is a network/server issue
                return (int)response.StatusCode is 200 or 204;
            }
            catch
            {
                return false;
            }
        }

        // ── Private helpers ────────────────────────────────────────────────────

        private async Task SendAsync(string toEmail, string subject, string htmlBody)
        {
            var client  = new SendGridClient(_apiKey);
            var from    = new EmailAddress(_fromEmail, _fromName);
            var to      = new EmailAddress(toEmail);
            var message = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent: null, htmlContent: htmlBody);

            var response = await client.SendEmailAsync(message);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Body.ReadAsStringAsync();
                _logger.LogError("SendGrid rejected email to {To}. Status: {Status}. Body: {Body}",
                    toEmail, response.StatusCode, body);
                throw new InvalidOperationException(
                    $"SendGrid returned {(int)response.StatusCode} when sending to {toEmail}. Details: {body}");
            }

            _logger.LogInformation("SendGrid: email sent to {To} (subject: {Subject})", toEmail, subject);
        }
    }
}
