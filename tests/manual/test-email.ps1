# Bypass SSL certificate checks (dev only)
Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAll : ICertificatePolicy {
    public bool CheckValidationResult(ServicePoint sp, X509Certificate cert, WebRequest req, int problem) { return true; }
}
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAll

$baseUrl = "https://localhost:7175"
$adminEmail = "adaboltman@gmail.com"
$adminPassword = "Boltm@n123"

Write-Host "=== Step 1: Login ===" -ForegroundColor Cyan
$loginBody = @{ email = $adminEmail; password = $adminPassword } | ConvertTo-Json
try {
    $loginResp = Invoke-RestMethod -Method POST -Uri "$baseUrl/api/auth/login" -ContentType "application/json" -Body $loginBody
    $token = $loginResp.token
    Write-Host "Login successful. Got JWT token." -ForegroundColor Green
} catch {
    Write-Host "Login FAILED: $_" -ForegroundColor Red
    exit 1
}

$headers = @{ Authorization = "Bearer $token" }

Write-Host ""
Write-Host "=== Step 2: Test SMTP Connection ===" -ForegroundColor Cyan
try {
    $testResp = Invoke-RestMethod -Method POST -Uri "$baseUrl/api/checkouts/test-email" -Headers $headers
    Write-Host "SMTP test result: $($testResp.message)" -ForegroundColor Green
} catch {
    $msg = if ($errBody.message) { $errBody.message } else { $_.Exception.Message }
    Write-Host "SMTP test FAILED: $msg" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Step 3: Trigger Overdue Reminders ===" -ForegroundColor Cyan
try {
    $reminderResp = Invoke-RestMethod -Method POST -Uri "$baseUrl/api/checkouts/trigger-overdue-reminders" -Headers $headers
    Write-Host "Overdue count:       $($reminderResp.overdueCount)" -ForegroundColor Yellow
    Write-Host "Reminder emails sent: $($reminderResp.reminderEmailsSent)" -ForegroundColor Green
    Write-Host "Reminder emails failed: $($reminderResp.reminderEmailsFailed)" -ForegroundColor $(if ($reminderResp.reminderEmailsFailed -gt 0) { "Red" } else { "Green" })
    Write-Host "Admin digest sent:   $($reminderResp.adminDigestSent)" -ForegroundColor $(if ($reminderResp.adminDigestSent) { "Green" } else { "Yellow" })
    Write-Host "Message: $($reminderResp.message)" -ForegroundColor White
} catch {
    $errBody = $_.ErrorDetails.Message | ConvertFrom-Json -ErrorAction SilentlyContinue
    $msg = if ($errBody.message) { $errBody.message } else { $_.Exception.Message }
    Write-Host "Trigger FAILED: $msg" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Done ===" -ForegroundColor Cyan
