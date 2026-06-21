# 🧪 BACKEND TESTING SCRIPT - RUN THIS NOW

**Follow this script step-by-step to verify your backend is working**

---

## ✅ STEP 1: START THE API (2 minutes)

### Open PowerShell terminal in Visual Studio
**Menu:** View → Terminal

### Navigate to API project
```powershell
cd C:\Users\AbdudDaiyyaanB\source\repos\InternalAssetTracker\AssetTracker.API
```

### Run the API
```powershell
dotnet run
```

### ✅ Expected Output:
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7xxx
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5xxx
info: AssetTracker.API.BackgroundServices.OverdueReminderService[0]
      Overdue Reminder Service started.
      Next overdue check scheduled for: 2026-06-XX 08:00:00 UTC
```

### ✅ Success Criteria:
- [ ] API starts without exceptions
- [ ] HTTPS and HTTP URLs shown
- [ ] "Overdue Reminder Service started" message appears
- [ ] No red error text in console

### ❌ If it fails:
**Error: "Unable to connect to database"**
- Fix: Check SQL Server is running
- Fix: Verify ConnectionString in appsettings.json
- Fix: Run `dotnet ef database update`

**Error: "Port already in use"**
- Fix: Close other instances of the API
- Fix: Change port in launchSettings.json

---

## ✅ STEP 2: OPEN SWAGGER (1 minute)

### Copy the HTTPS URL from terminal output
Example: `https://localhost:7045`

### Open in your browser
```
https://localhost:7045/swagger
```

### ✅ Success Criteria:
- [ ] Swagger UI loads (green banner "Asset Tracker API")
- [ ] All controllers visible:
  - Auth
  - Assets
  - Checkouts
  - Reports
  - Users
- [ ] Endpoints expand when you click them

### ❌ If it fails:
**Certificate error / "Your connection is not private"**
- Click "Advanced" → "Proceed to localhost (unsafe)" (it's safe, it's your own computer)
- Or run: `dotnet dev-certs https --trust`

---

## ✅ STEP 3: TEST AUTHENTICATION (5 minutes)

### 3.1 Test Unauthenticated Access
1. Find `GET /api/assets` endpoint
2. Click "Try it out"
3. Click "Execute"

**✅ Expected:** 401 Unauthorized response

### 3.2 Login as Admin
1. Find `POST /api/auth/login` endpoint
2. Click "Try it out"
3. Enter credentials (check your database for an admin user):
   ```json
   {
     "email": "admin@example.com",
     "password": "your_password"
   }
   ```
4. Click "Execute"

**✅ Expected:** 200 OK with JWT token in response
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "admin@example.com",
  "role": "Admin"
}
```

### 3.3 Authorize Swagger
1. Copy the token (just the long string)
2. Click the "Authorize" button (🔒 icon at top right)
3. Paste token in the "Value" field
4. Click "Authorize"
5. Click "Close"

### 3.4 Test Authenticated Access
1. Go back to `GET /api/assets`
2. Click "Try it out"
3. Click "Execute"

**✅ Expected:** 200 OK with list of assets

### ✅ Success Criteria:
- [ ] Unauthenticated request returns 401
- [ ] Login returns JWT token
- [ ] Authenticated request returns 200 with data

---

## ✅ STEP 4: TEST ROLE ENFORCEMENT (3 minutes)

### 4.1 Login as Standard User
1. Logout (click Authorize → Logout)
2. Login with Standard User credentials
3. Authorize Swagger with new token

### 4.2 Try to Create Asset (Should Fail)
1. Find `POST /api/assets` endpoint
2. Click "Try it out"
3. Enter test data:
   ```json
   {
     "name": "Test Laptop",
     "category": "Laptop",
     "serialNumber": "TEST123",
     "status": "Available",
     "purchaseDate": "2025-06-01T00:00:00Z",
     "value": 1200
   }
   ```
4. Click "Execute"

**✅ Expected:** 403 Forbidden
```json
{
  "error": "User does not have permission..."
}
```

### 4.3 Login as Admin Again
1. Logout
2. Login with Admin credentials
3. Authorize with admin token

### 4.4 Try to Create Asset (Should Succeed)
1. Use same `POST /api/assets` request as above
2. Click "Execute"

**✅ Expected:** 201 Created with asset details

### ✅ Success Criteria:
- [ ] Standard User gets 403 on POST /api/assets
- [ ] Admin gets 201 on POST /api/assets

---

## ✅ STEP 5: TEST CHECKOUT STATE MACHINE (5 minutes)

### 5.1 Note an Available Asset ID
1. `GET /api/assets` → Note an asset with Status = "Available"
2. Remember its AssetID (e.g., 5)

### 5.2 Checkout the Asset
1. Find `POST /api/checkouts/assign` endpoint
2. Click "Try it out"
3. Enter:
   ```json
   {
     "assetID": 5,
     "employeeEmail": "user@example.com",
     "expectedReturnDate": "2025-12-31"
   }
   ```
   (Use a real user email from your Users table)
4. Click "Execute"

**✅ Expected:** 200 OK
```json
{
  "success": true,
  "message": "Checkout successful!...",
  "checkoutId": 1,
  "assetName": "...",
  "expectedReturnDate": "2025-12-31T00:00:00Z"
}
```

### 5.3 Verify Database Changes
Open SQL Server Management Studio or VS SQL Server Object Explorer:

```sql
-- Check asset status
SELECT AssetID, Status, AssignedTo FROM Assets WHERE AssetID = 5;
-- Should show: Status = 'Checked Out', AssignedTo = user name

-- Check checkout record
SELECT * FROM Checkouts WHERE AssetID = 5 AND Status = 'Active';
-- Should show: 1 row with checkout details

-- Check audit trail
SELECT Action, AssetName, PerformedByEmail, Timestamp 
FROM AuditTrails 
WHERE AssetID = 5 
ORDER BY Timestamp DESC;
-- Should show: 'ASSIGN' entry
```

### 5.4 Try to Checkout Again (Should Fail)
1. Try `POST /api/checkouts/assign` with same asset again
2. **✅ Expected:** 400/409 error
   ```json
   {
     "message": "Asset is already checked out and not available."
   }
   ```

### ✅ Success Criteria:
- [ ] Checkout succeeds and returns success message
- [ ] Asset status changed to "Checked Out" in database
- [ ] Checkout record created in Checkouts table
- [ ] Audit trail entry created
- [ ] Second checkout attempt rejected with error

---

## ✅ STEP 6: TEST ILLEGAL STATUS CHANGE (3 minutes)

### 6.1 Try to Change Status via PUT
1. Get the asset you just checked out (AssetID = 5)
2. Find `PUT /api/assets/{id}` endpoint
3. Click "Try it out"
4. Enter ID: 5
5. Get the current asset data first: `GET /api/assets/5`
6. In PUT request body, change status to "Available":
   ```json
   {
     "assetID": 5,
     "name": "...",
     "category": "...",
     "serialNumber": "...",
     "status": "Available",  // ← Changed this
     "purchaseDate": "...",
     "value": ...
   }
   ```
7. Click "Execute"

**✅ Expected:** 400 Bad Request
```json
{
  "message": "Status cannot be changed directly via PUT. Use the appropriate endpoint..."
}
```

### ✅ Success Criteria:
- [ ] PUT with changed status rejected
- [ ] Error message directs to proper endpoints
- [ ] Database status unchanged

---

## ✅ STEP 7: TEST RETURN (2 minutes)

### 7.1 Return the Asset
1. Find `POST /api/checkouts/return/{assetId}` endpoint
2. Click "Try it out"
3. Enter assetId: 5
4. Click "Execute"

**✅ Expected:** 200 OK
```json
{
  "message": "Asset '...' has been successfully returned to inventory."
}
```

### 7.2 Verify Database Changes
```sql
-- Check asset status
SELECT AssetID, Status, AssignedTo FROM Assets WHERE AssetID = 5;
-- Should show: Status = 'Available', AssignedTo = '' (empty)

-- Check checkout record
SELECT Status, ActualReturnDate FROM Checkouts WHERE AssetID = 5;
-- Should show: Status = 'Returned', ActualReturnDate = now

-- Check audit trail
SELECT Action FROM AuditTrails WHERE AssetID = 5 ORDER BY Timestamp DESC;
-- Should show: 'RETURN' entry (newest)
```

### ✅ Success Criteria:
- [ ] Return succeeds
- [ ] Asset status = "Available"
- [ ] AssignedTo cleared
- [ ] Checkout marked as "Returned"
- [ ] Audit entry created

---

## ✅ STEP 8: TEST DECOMMISSION & RESTORE (3 minutes)

### 8.1 Decommission Asset
1. Find `DELETE /api/assets/{id}` endpoint
2. Click "Try it out"
3. Enter id: 5
4. Click "Execute"

**✅ Expected:** 200 OK
```json
{
  "message": "Asset '...' has been successfully decommissioned."
}
```

### 8.2 Verify Soft Delete
```sql
-- Check asset still exists
SELECT AssetID, Name, Status FROM Assets WHERE AssetID = 5;
-- Should show: 1 row, Status = 'Decommissioned'

-- Verify row not deleted
SELECT COUNT(*) FROM Assets WHERE AssetID = 5;
-- Should show: 1 (not 0)
```

### 8.3 Restore Asset
1. Find `POST /api/assets/{id}/restore` endpoint
2. Click "Try it out"
3. Enter id: 5
4. Click "Execute"

**✅ Expected:** 200 OK
```json
{
  "message": "Asset '...' has been successfully restored to active inventory."
}
```

### 8.4 Verify Restore
```sql
SELECT Status FROM Assets WHERE AssetID = 5;
-- Should show: 'Available'

-- Check audit trail
SELECT Action FROM AuditTrails WHERE AssetID = 5 ORDER BY Timestamp DESC;
-- Should show: 'RESTORE', 'DECOMMISSION', 'RETURN', 'ASSIGN', 'PROVISION'
```

### ✅ Success Criteria:
- [ ] Decommission changes status (doesn't delete row)
- [ ] Database row persists after decommission
- [ ] Restore changes status back to Available
- [ ] Both actions audited

---

## ✅ STEP 9: TEST DUPLICATE SERIAL (2 minutes)

### 9.1 Try to Create Duplicate Serial
1. Note an existing asset's serial number (e.g., "TEST123")
2. Try `POST /api/assets` with same serial:
   ```json
   {
     "name": "Another Laptop",
     "category": "Laptop",
     "serialNumber": "TEST123",  // ← Duplicate
     "status": "Available",
     "purchaseDate": "2025-06-01T00:00:00Z",
     "value": 1500
   }
   ```
3. Click "Execute"

**✅ Expected:** 400 Bad Request
```json
{
  "message": "Duplicate serial number detected. An asset with serial number 'TEST123' already exists..."
}
```

### 9.2 Try to Edit to Duplicate Serial
1. Get a different asset (e.g., AssetID = 3)
2. Try `PUT /api/assets/3` with serial "TEST123"
3. Click "Execute"

**✅ Expected:** 400 Bad Request (same duplicate error)

### ✅ Success Criteria:
- [ ] Create with duplicate serial rejected
- [ ] Edit to duplicate serial rejected
- [ ] Error message shows which asset has the serial

---

## ✅ STEP 10: TEST PAST DATE VALIDATION (2 minutes)

### 10.1 Try to Checkout with Past Date
1. Find an available asset
2. Try `POST /api/checkouts/assign`:
   ```json
   {
     "assetID": 3,
     "employeeEmail": "user@example.com",
     "expectedReturnDate": "2020-01-01"  // ← Past date
   }
   ```
3. Click "Execute"

**✅ Expected:** 400 Bad Request
```json
{
  "message": "Expected return date cannot be in the past."
}
```

### ✅ Success Criteria:
- [ ] Past date rejected
- [ ] No checkout record created

---

## ✅ STEP 11: TEST USER REJECTION (3 minutes)

### 11.1 Find a Pending User
```sql
SELECT UserID, FullName, Email FROM Users WHERE AccountStatus = 'Pending';
```
(If none exist, register a new user via `POST /api/auth/register`)

### 11.2 Reject the User
1. Find `POST /api/users/{id}/reject` endpoint
2. Click "Try it out"
3. Enter id: (pending user ID)
4. Enter body:
   ```json
   {
     "rejectionReason": "Invalid company email domain"
   }
   ```
5. Click "Execute"

**✅ Expected:** 200 OK
```json
{
  "message": "User '...' has been rejected."
}
```

### 11.3 Verify Rejection
```sql
SELECT FullName, AccountStatus, RejectionReason 
FROM Users 
WHERE UserID = ?;
-- Should show: AccountStatus = 'Rejected', RejectionReason = 'Invalid company email domain'

SELECT Action, Details 
FROM AuditTrails 
WHERE Action = 'REJECT_USER' 
ORDER BY Timestamp DESC;
-- Should show: Audit entry with rejection reason
```

### ✅ Success Criteria:
- [ ] User status changed to "Rejected"
- [ ] Rejection reason stored
- [ ] Audit entry created

---

## ✅ STEP 12: TEST REPORT EXPORT (2 minutes)

### 12.1 Export Excel Report
1. Find `GET /api/reports/checkout-history/excel` endpoint
2. Click "Try it out"
3. Enter parameters:
   - fromDate: 2025-01-01
   - toDate: 2025-12-31
4. Click "Execute"

**✅ Expected:** 
- Download starts automatically
- File name: `checkout_history_*.xlsx`
- File size > 0 bytes

### 12.2 Verify Excel File
1. Open the downloaded file in Excel
2. Check:
   - [ ] File opens without errors
   - [ ] Column headers present
   - [ ] Data rows match checkout history
   - [ ] Dates within specified range

### ✅ Success Criteria:
- [ ] File downloads successfully
- [ ] File opens in Excel
- [ ] Data matches filter criteria

---

## ✅ STEP 13: TEST BACKGROUND SERVICE (1 minute)

### 13.1 Check Console Output
Look at the PowerShell terminal where API is running.

**✅ Expected to see:**
```
info: AssetTracker.API.BackgroundServices.OverdueReminderService[0]
      Overdue Reminder Service started.
      Next overdue check scheduled for: 2026-06-XX 08:00:00 UTC
```

### 13.2 Verify Service Registration
Open `Program.cs` and find:
```csharp
builder.Services.AddHostedService<OverdueReminderService>();
```

### ✅ Success Criteria:
- [ ] Service startup message in console
- [ ] Next run time scheduled
- [ ] No service exceptions

---

## 📊 FINAL SCORECARD

Mark each test as you complete it:

| # | Test | Status | Notes |
|---|------|--------|-------|
| 1 | API Startup | ☐ | |
| 2 | Swagger Loads | ☐ | |
| 3 | Auth - 401 Without Token | ☐ | |
| 4 | Auth - Login Gets Token | ☐ | |
| 5 | Auth - Token Works | ☐ | |
| 6 | Role - Standard User 403 | ☐ | |
| 7 | Role - Admin 201 | ☐ | |
| 8 | Checkout - Success | ☐ | |
| 9 | Checkout - Database Updated | ☐ | |
| 10 | Checkout - Audit Written | ☐ | |
| 11 | Checkout - Duplicate Blocked | ☐ | |
| 12 | Status - Direct Change Blocked | ☐ | |
| 13 | Return - Success | ☐ | |
| 14 | Return - Database Updated | ☐ | |
| 15 | Decommission - Soft Delete | ☐ | |
| 16 | Restore - Works | ☐ | |
| 17 | Serial - Duplicate Blocked | ☐ | |
| 18 | Date - Past Date Blocked | ☐ | |
| 19 | User - Rejection Stored | ☐ | |
| 20 | Report - Excel Export | ☐ | |
| 21 | Background - Service Running | ☐ | |

**TOTAL: ___ / 21**

**Passing Grade: 18/21 (85%)**

---

## 🎉 WHEN YOU'RE DONE

### If 18+ tests passed:
✅ **YOU'RE ASSESSMENT READY!**
- Print the ASSESSOR_DEMO_QUICKREF.md
- Practice the 6-test demo
- Schedule your assessment
- You've got this! 💪

### If < 18 tests passed:
⚠️ **NEEDS ATTENTION**
- Note which tests failed
- Check error messages
- Review code for those features
- Re-test until passing

---

## 📝 TESTING NOTES

**Date Tested:** _______________  
**Total Tests Passed:** ___ / 21  
**Issues Found:**
```
_____________________________________________________________
_____________________________________________________________
_____________________________________________________________
```

**Next Actions:**
```
_____________________________________________________________
_____________________________________________________________
```

---

**Good luck with your testing!** 🚀
