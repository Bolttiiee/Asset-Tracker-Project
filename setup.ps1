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

# 3. Ensure dotnet-ef tool is installed
Write-Host "Step 3: Ensuring EF Core CLI Tool is installed" -ForegroundColor Yellow
$efCommand = Get-Command dotnet-ef -ErrorAction SilentlyContinue
if (-not $efCommand) {
    Write-Host "dotnet-ef not found. Installing dotnet-ef globally..." -ForegroundColor Yellow
    dotnet tool install --global dotnet-ef
    # Update PATH for the current session to ensure the newly installed tool is accessible
    $env:PATH += ";$env:USERPROFILE\.dotnet\tools"
} else {
    Write-Host "dotnet-ef tool is already installed." -ForegroundColor Green
}
Write-Host ""

# 4. Apply database updates
Write-Host "Step 4: Creating Database and Applying Migrations" -ForegroundColor Yellow
Write-Host "Note: By default, the app uses SQL Server Express (localhost\SQLEXPRESS)." -ForegroundColor Gray
Write-Host "If you are using SQL Server LocalDB or another instance, make sure to update " -ForegroundColor Gray
Write-Host "the ConnectionStrings:DefaultConnection in AssetTracker.API/appsettings.json." -ForegroundColor Gray
dotnet ef database update --project AssetTracker.API
if ($LASTEXITCODE -eq 0) {
    Write-Host "Database setup completed successfully." -ForegroundColor Green
} else {
    Write-Warning "Database migrations failed to apply. Verify SQL Server is running and connection string is correct."
}
Write-Host ""

# 5. Frontend dependencies installation
Write-Host "Step 5: Installing Frontend Dependencies" -ForegroundColor Yellow
$nodeCommand = Get-Command node -ErrorAction SilentlyContinue
if (-not $nodeCommand) {
    Write-Warning "Node.js (node) is not installed or not in PATH. Please install Node.js (https://nodejs.org/) to run the Angular UI."
} else {
    cd AssetTracker.UI
    npm install
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Frontend packages installed successfully." -ForegroundColor Green
    } else {
        Write-Warning "Failed to install frontend dependencies."
    }
    cd ..
}
Write-Host ""

Write-Host "==========================================" -ForegroundColor Cyan
Write-Host "   Setup Completed!" -ForegroundColor Cyan
Write-Host "   You can now start the application:" -ForegroundColor Cyan
Write-Host "   Backend: dotnet run --project AssetTracker.API --launch-profile https" -ForegroundColor Gray
Write-Host "   Frontend: cd AssetTracker.UI; npm start" -ForegroundColor Gray
Write-Host "==========================================" -ForegroundColor Cyan
