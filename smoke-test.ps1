# Full end-to-end email test against the running API
Add-Type @"
using System.Net;
using System.Security.Cryptography.X509Certificates;
public class TrustAll5 : ICertificatePolicy {
    public bool CheckValidationResult(ServicePoint sp, X509Certificate cert, WebRequest req, int problem) { return true; }
}
"@
[System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAll5

# Auto-detect API port
$baseUrl = $null
foreach ($url in @("https://localhost:7175", "http://localhost:5167")) {
    try {
        Invoke-WebRequest -Uri "$url/weatherforecast" -TimeoutSec 3 -ErrorAction Stop | Out-Null
        $baseUrl = $url
        break
    } catch { }
}
if (-not $baseUrl) { Write-Host "ERROR: API not reachable." -ForegroundColor Red; exit 1 }
Write-Host "API at: $baseUrl" -ForegroundColor Cyan

# Login
$body = '{"email":"adaboltman@gmail.com","password":"Boltm@n123"}'
$loginResp = Invoke-RestMethod -Method POST -Uri "$baseUrl/api/auth/login" -ContentType "application/json" -Body $body
$token = $loginResp.token
$headers = @{ Authorization = "Bearer $token" }
Write-Host "Login: OK" -ForegroundColor Green

# SMTP connectivity check
$testResp = Invoke-RestMethod -Method POST -Uri "$baseUrl/api/checkouts/test-email" -Headers $headers
Write-Host "SMTP connectivity: $($testResp.message)" -ForegroundColor Green

# Trigger real overdue reminder emails
Write-Host "Triggering overdue reminder emails..." -ForegroundColor Yellow
$remResp = Invoke-RestMethod -Method POST -Uri "$baseUrl/api/checkouts/trigger-overdue-reminders" -Headers $headers
Write-Host "Overdue assets:    $($remResp.overdueCount)"
Write-Host "Emails sent:       $($remResp.reminderEmailsSent)"
Write-Host "Emails failed:     $($remResp.reminderEmailsFailed)"
Write-Host "Admin digest sent: $($remResp.adminDigestSent)"

if ($remResp.reminderEmailsSent -gt 0) {
    Write-Host "SUCCESS - real emails sent via Gmail SMTP. Check adaboltman@gmail.com inbox." -ForegroundColor Green
} elseif ($remResp.overdueCount -eq 0) {
    Write-Host "No overdue assets right now - no emails needed." -ForegroundColor Yellow
}
