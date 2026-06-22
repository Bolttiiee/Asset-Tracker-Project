# Direct email trigger test - http port only, no auto-detection
$baseUrl = "http://localhost:5167"

$body = '{"email":"adaboltman@gmail.com","password":"Boltm@n123"}'
$loginResp = Invoke-RestMethod -Method POST -Uri "$baseUrl/api/auth/login" -ContentType "application/json" -Body $body
$headers = @{ Authorization = "Bearer $($loginResp.token)" }
Write-Host "Login: OK"

$r = Invoke-RestMethod -Method POST -Uri "$baseUrl/api/checkouts/trigger-overdue-reminders" -Headers $headers
Write-Host "Overdue:  $($r.overdueCount)"
Write-Host "Sent:     $($r.reminderEmailsSent)"
Write-Host "Failed:   $($r.reminderEmailsFailed)"
Write-Host "Digest:   $($r.adminDigestSent)"
Write-Host "Message:  $($r.message)"
