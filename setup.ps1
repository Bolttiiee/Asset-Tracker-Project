# Interactive Setup Script for Internal Asset Tracker

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   Internal Asset Tracker Local Setup" -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
Write-Host ""

# 1. Prompt for SMTP Password
Write-Host "Step 1: Configure SMTP Email Settings" -ForegroundColor Yellow
$smtpPassword = Read-Host -Prompt "Enter SMTP Password (or App Password) for email reminders"
if ([string]::IsNullOrWhiteSpace($smtpPassword)) {
    Write-Host "SMTP Password not provided. Email reminder functions will be inactive." -ForegroundColor Gray
} else {
    dotnet user-secrets set "Email:SmtpPassword" "$smtpPassword" --project AssetTracker.API
    Write-Host "SMTP Password successfully stored in local User Secrets." -ForegroundColor Green
}
Write-Host ""

# 2. Prompt for JWT Secret or generate one
Write-Host "Step 2: Configure JWT Authentication" -ForegroundColor Yellow
$jwtSecret = Read-Host -Prompt "Enter JWT Signing Secret (press Enter to auto-generate a secure 64-character key)"
if ([string]::IsNullOrWhiteSpace($jwtSecret)) {
    # Generate a random 64-character hex string
    $bytes = New-Object Byte[] 32
    [System.Security.Cryptography.RandomNumberGenerator]::Create().GetBytes($bytes)
    $jwtSecret = [System.BitConverter]::ToString($bytes) -replace '-'
    Write-Host "Auto-generated a secure 64-character JWT secret key." -ForegroundColor Green
}
dotnet user-secrets set "Jwt:Secret" "$jwtSecret" --project AssetTracker.API
Write-Host "JWT Secret successfully stored in local User Secrets." -ForegroundColor Green
Write-Host ""

# 3. Apply database updates
Write-Host "Step 3: Creating Database and Applying Migrations" -ForegroundColor Yellow
dotnet ef database update --project AssetTracker.API
if ($LASTEXITCODE -eq 0) {
    Write-Host "Database setup completed successfully." -ForegroundColor Green
} else {
    Write-Warning "Database migrations failed to apply. Please verify SQL Server is running."
}
Write-Host ""

# 4. Frontend dependencies installation
Write-Host "Step 4: Installing Frontend Dependencies" -ForegroundColor Yellow
cd AssetTracker.UI
npm install
if ($LASTEXITCODE -eq 0) {
    Write-Host "Frontend packages installed successfully." -ForegroundColor Green
} else {
    Write-Warning "Failed to install frontend dependencies."
}
cd ..
Write-Host ""

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   Setup Completed!" -ForegroundColor Cyan
Write-Host "   You can now start the application." -ForegroundColor Cyan
Write-Host "==========================================" -ForegroundColor Cyan
