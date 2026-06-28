# Test Authentication Endpoints
# Make sure your API is running on http://localhost:5167

Write-Host "`n=== Testing Authentication Endpoints ===" -ForegroundColor Cyan

# Test 1: Check Email Endpoint
Write-Host "`n1. Testing GET /api/auth/check-email" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5167/api/auth/check-email?email=test@example.com" -Method GET -ErrorAction Stop
    Write-Host "   Status Code: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "   Response: $($response.Content)" -ForegroundColor Green
} catch {
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
}

# Test 2: Check Employee ID Endpoint
Write-Host "`n2. Testing GET /api/auth/check-employee-id" -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "http://localhost:5167/api/auth/check-employee-id?employeeId=EMP001" -Method GET -ErrorAction Stop
    Write-Host "   Status Code: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "   Response: $($response.Content)" -ForegroundColor Green
} catch {
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
}

# Test 3: Register Endpoint
Write-Host "`n3. Testing POST /api/auth/register" -ForegroundColor Yellow
$registerBody = @{
    fullName = "Test User"
    employeeId = "EMP999"
    email = "testuser999@example.com"
    password = "Test@1234"
    phoneNumber = "0123456789"
} | ConvertTo-Json

try {
    $response = Invoke-WebRequest -Uri "http://localhost:5167/api/auth/register" -Method POST -Body $registerBody -ContentType "application/json" -ErrorAction Stop
    Write-Host "   Status Code: $($response.StatusCode)" -ForegroundColor Green
    Write-Host "   Response: $($response.Content)" -ForegroundColor Green
} catch {
    Write-Host "   Error: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        Write-Host "   Status Code: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Red
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $responseBody = $reader.ReadToEnd()
        Write-Host "   Response Body: $responseBody" -ForegroundColor Red
    }
}

Write-Host "`n=== Test Complete ===" -ForegroundColor Cyan
