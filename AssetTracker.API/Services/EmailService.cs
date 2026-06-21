using AssetTracker.API.Models;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;

namespace AssetTracker.API.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;
        private readonly string _smtpHost;
        private readonly int _smtpPort;
        private readonly string _smtpUser;
        private readonly string _smtpPassword;
        private readonly string _fromEmail;
        private readonly string _fromName;
        private readonly string _adminEmail;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
            _smtpHost = _configuration["Email:SmtpHost"] ?? "smtp.gmail.com";
            _smtpPort = int.Parse(_configuration["Email:SmtpPort"] ?? "587");
            _smtpUser = _configuration["Email:SmtpUser"] ?? "";
            _smtpPassword = _configuration["Email:SmtpPassword"] ?? "";
            _fromEmail = _configuration["Email:FromEmail"] ?? _smtpUser;
            _fromName = _configuration["Email:FromName"] ?? "Asset Tracker System";
            _adminEmail = _configuration["Email:AdminEmail"] ?? "admin@example.com";
        }

        public async Task SendOverdueReminderAsync(Checkout checkout)
        {
            if (checkout.Asset == null)
            {
                throw new ArgumentException("Checkout must include Asset information");
            }

            var subject = $"REMINDER: Asset '{checkout.Asset.Name}' is overdue for return";

            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2 style='color: #d9534f;'>Asset Return Overdue</h2>
                    <p>Dear {checkout.UserFullName},</p>

                    <p>This is a reminder that the following asset is overdue for return:</p>

                    <div style='background-color: #f5f5f5; padding: 15px; border-left: 4px solid #d9534f; margin: 20px 0;'>
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
                </html>
            ";

            await SendEmailAsync(checkout.UserEmail, subject, body);
        }

        public async Task SendAdminOverdueDigestAsync(List<OverdueCheckoutInfo> overdueCheckouts)
        {
            if (!overdueCheckouts.Any())
            {
                return;
            }

            var subject = $"Daily Overdue Assets Digest - {overdueCheckouts.Count} asset(s) overdue";

            var tableRows = string.Join("\n", overdueCheckouts.Select(o => $@"
                <tr>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{o.UserFullName}</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{o.UserEmail}</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{o.AssetName}</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{o.SerialNumber}</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{o.CheckoutDate:yyyy-MM-dd}</td>
                    <td style='padding: 8px; border: 1px solid #ddd;'>{o.ExpectedReturnDate:yyyy-MM-dd}</td>
                    <td style='padding: 8px; border: 1px solid #ddd; font-weight: bold; color: #d9534f;'>{o.DaysOverdue}</td>
                </tr>
            "));

            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2 style='color: #d9534f;'>Daily Overdue Assets Report</h2>
                    <p>The following {overdueCheckouts.Count} asset(s) are currently overdue:</p>

                    <table style='border-collapse: collapse; width: 100%; margin: 20px 0;'>
                        <thead>
                            <tr style='background-color: #f5f5f5;'>
                                <th style='padding: 8px; border: 1px solid #ddd; text-align: left;'>User Name</th>
                                <th style='padding: 8px; border: 1px solid #ddd; text-align: left;'>Email</th>
                                <th style='padding: 8px; border: 1px solid #ddd; text-align: left;'>Asset Name</th>
                                <th style='padding: 8px; border: 1px solid #ddd; text-align: left;'>Serial Number</th>
                                <th style='padding: 8px; border: 1px solid #ddd; text-align: left;'>Checkout Date</th>
                                <th style='padding: 8px; border: 1px solid #ddd; text-align: left;'>Expected Return</th>
                                <th style='padding: 8px; border: 1px solid #ddd; text-align: left;'>Days Overdue</th>
                            </tr>
                        </thead>
                        <tbody>
                            {tableRows}
                        </tbody>
                    </table>

                    <p>Please follow up with users who have overdue assets.</p>

                    <p>Asset Tracker System</p>
                </body>
                </html>
            ";

            await SendEmailAsync(_adminEmail, subject, body);
        }

        public async Task SendPasswordResetEmailAsync(string toEmail, string resetLink)
        {
            var subject = "Asset Tracker Password Reset Request";
            var body = $@"
                <html>
                <body style='font-family: Arial, sans-serif;'>
                    <h2 style='color: #2b6cb0;'>Password Reset Request</h2>
                    <p>We received a request to reset your Asset Tracker account password.</p>
                    <p>Use the secure link below to set a new password. This link expires in 1 hour.</p>

                    <p style='margin: 24px 0;'>
                        <a href='{resetLink}' style='background-color: #2b6cb0; color: #ffffff; padding: 10px 16px; text-decoration: none; border-radius: 4px;'>Reset Password</a>
                    </p>

                    <p>If the button above does not work, copy and paste this URL into your browser:</p>
                    <p><a href='{resetLink}'>{resetLink}</a></p>

                    <p>If you did not request this change, you can safely ignore this email.</p>

                    <p>Asset Tracker System</p>
                </body>
                </html>
            ";

            await SendEmailAsync(toEmail, subject, body);
        }

        public async Task<bool> TestEmailConnectionAsync()
        {
            try
            {
                // Validate required SMTP settings first.
                if (string.IsNullOrWhiteSpace(_smtpHost)
                    || _smtpPort <= 0
                    || string.IsNullOrWhiteSpace(_smtpUser)
                    || string.IsNullOrWhiteSpace(_smtpPassword)
                    || string.IsNullOrWhiteSpace(_fromEmail))
                {
                    return false;
                }

                // Non-delivery test: only verify SMTP endpoint is reachable.
                using var tcpClient = new TcpClient();
                var connectTask = tcpClient.ConnectAsync(_smtpHost, _smtpPort);
                var timeoutTask = Task.Delay(TimeSpan.FromSeconds(5));
                var completedTask = await Task.WhenAny(connectTask, timeoutTask);

                if (completedTask != connectTask || !tcpClient.Connected)
                {
                    return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        private async Task SendEmailAsync(string toEmail, string subject, string htmlBody)
        {
            try
            {
                using var client = new SmtpClient(_smtpHost, _smtpPort);
                client.EnableSsl = true;
                client.Credentials = new NetworkCredential(_smtpUser, _smtpPassword);

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail, _fromName),
                    Subject = subject,
                    Body = htmlBody,
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                await client.SendMailAsync(mailMessage);
            }
            catch (Exception ex)
            {
                // Log the error (in production, use proper logging)
                Console.WriteLine($"Failed to send email to {toEmail}: {ex.Message}");
                throw;
            }
        }
    }
}
