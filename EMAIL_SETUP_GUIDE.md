# Email Configuration Quick Start Guide

## Gmail Setup (Most Common)

### Step 1: Enable 2-Factor Authentication
1. Go to [Google Account Security](https://myaccount.google.com/security)
2. Enable "2-Step Verification"

### Step 2: Generate App Password
1. Go to [App Passwords](https://myaccount.google.com/apppasswords)
2. Select "Mail" and "Windows Computer" (or Other)
3. Copy the 16-character password generated

### Step 3: Update appsettings.json
```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SmtpUser": "your-email@gmail.com",
  "SmtpPassword": "xxxx xxxx xxxx xxxx",  // 16-char app password
  "FromEmail": "your-email@gmail.com",
  "FromName": "Asset Tracker System",
  "AdminEmail": "admin@yourcompany.com"
}
```

---

## Outlook/Office 365 Setup

### For Personal Outlook.com Accounts
```json
"Email": {
  "SmtpHost": "smtp-mail.outlook.com",
  "SmtpPort": "587",
  "SmtpUser": "your-email@outlook.com",
  "SmtpPassword": "your-password",
  "FromEmail": "your-email@outlook.com",
  "FromName": "Asset Tracker System",
  "AdminEmail": "admin@yourcompany.com"
}
```

### For Office 365 Business Accounts
```json
"Email": {
  "SmtpHost": "smtp.office365.com",
  "SmtpPort": "587",
  "SmtpUser": "your-email@yourcompany.com",
  "SmtpPassword": "your-password",
  "FromEmail": "your-email@yourcompany.com",
  "FromName": "Asset Tracker System",
  "AdminEmail": "admin@yourcompany.com"
}
```

**Note:** May require app-specific password if MFA is enabled.

---

## Yahoo Mail Setup

```json
"Email": {
  "SmtpHost": "smtp.mail.yahoo.com",
  "SmtpPort": "587",
  "SmtpUser": "your-email@yahoo.com",
  "SmtpPassword": "your-app-password",
  "FromEmail": "your-email@yahoo.com",
  "FromName": "Asset Tracker System",
  "AdminEmail": "admin@yourcompany.com"
}
```

**Requires:** Generate app password at [Yahoo Account Security](https://login.yahoo.com/account/security)

---

## Custom SMTP Server (Company Email)

Contact your IT department for:
- SMTP Server address
- Port number (usually 587 or 465)
- Authentication credentials
- SSL/TLS requirements

```json
"Email": {
  "SmtpHost": "mail.yourcompany.com",
  "SmtpPort": "587",
  "SmtpUser": "assettracker@yourcompany.com",
  "SmtpPassword": "provided-by-IT",
  "FromEmail": "assettracker@yourcompany.com",
  "FromName": "Asset Tracker System",
  "AdminEmail": "admin@yourcompany.com"
}
```

---

## Testing Email Configuration

### Option 1: Use Swagger UI
1. Start the API
2. Navigate to `https://localhost:7XXX/swagger`
3. Create a test checkout with an overdue date
4. Wait for the background service to run (or trigger manually)

### Option 2: Add Test Endpoint (Development Only)

Add to `CheckoutsController.cs`:

```csharp
// GET: api/checkouts/test-email
[HttpGet("test-email")]
[Authorize(Roles = "Admin")]
public async Task<IActionResult> TestEmail([FromServices] IEmailService emailService)
{
    try
    {
        var testCheckout = new Checkout
        {
            UserFullName = "Test User",
            UserEmail = "recipient@example.com", // Change this to your test email
            CheckoutTimestamp = DateTime.UtcNow.AddDays(-10),
            ExpectedReturnDate = DateTime.UtcNow.AddDays(-3),
            Asset = new Asset
            {
                Name = "Test Laptop",
                SerialNumber = "TEST123"
            }
        };

        await emailService.SendOverdueReminderAsync(testCheckout);
        return Ok(new { message = "Test email sent successfully!" });
    }
    catch (Exception ex)
    {
        return BadRequest(new { message = $"Email test failed: {ex.Message}" });
    }
}
```

---

## Common Issues and Solutions

### Issue: "Authentication failed"
**Solution:**
- Verify email and password are correct
- For Gmail: Use app-specific password, not regular password
- For Outlook: Check if MFA requires app password

### Issue: "Connection timeout"
**Solution:**
- Check SMTP host and port are correct
- Verify firewall allows outbound connections on port 587
- Try port 465 as alternative

### Issue: "Sender not authorized"
**Solution:**
- Ensure `FromEmail` matches `SmtpUser`
- Some providers require sender to be verified

### Issue: Emails go to spam
**Solution:**
- Add SPF/DKIM records (requires domain admin access)
- Ask recipients to whitelist the sender address
- Use a professional email domain

---

## Security Best Practices

### ✅ DO:
- Use app-specific passwords (not your main account password)
- Store credentials in environment variables for production
- Use appsettings.Development.json for local testing
- Restrict email endpoint access to authorized roles

### ❌ DON'T:
- Commit real credentials to source control
- Share app passwords
- Use personal email for production systems
- Leave test endpoints in production code

---

## Environment Variables (Production)

Instead of appsettings.json, use environment variables:

**In Azure App Service:**
```
Email__SmtpHost = smtp.gmail.com
Email__SmtpPort = 587
Email__SmtpUser = your-email@gmail.com
Email__SmtpPassword = your-app-password
Email__FromEmail = your-email@gmail.com
Email__FromName = Asset Tracker System
Email__AdminEmail = admin@company.com
```

**In Docker:**
```yaml
environment:
  - Email__SmtpHost=smtp.gmail.com
  - Email__SmtpPort=587
  - Email__SmtpUser=your-email@gmail.com
  - Email__SmtpPassword=your-app-password
```

**In Windows:**
```powershell
setx Email__SmtpHost "smtp.gmail.com"
setx Email__SmtpPort "587"
# ... etc
```

---

## Monitoring Email Delivery

### Check Audit Trail
Email sends are logged:
```sql
SELECT * FROM AuditTrail 
WHERE Action IN ('EMAIL_REMINDER', 'ADMIN_DIGEST')
ORDER BY Timestamp DESC;
```

### Check Application Logs
Look for:
- "Sent overdue reminder for asset X to user@email.com"
- "Admin overdue digest sent successfully"
- Any error messages from EmailService

---

## Support Resources

- **Gmail SMTP:** https://support.google.com/mail/answer/7126229
- **Outlook SMTP:** https://support.microsoft.com/en-us/office/pop-imap-and-smtp-settings-8361e398-8af4-4e97-b147-6c6c4ac95353
- **Yahoo SMTP:** https://help.yahoo.com/kb/SLN4075.html

---

## Contact

If you encounter issues not covered here:
1. Check the application logs
2. Verify audit trail for email send attempts
3. Test with a simple SMTP testing tool
4. Contact your IT department for company email configurations
