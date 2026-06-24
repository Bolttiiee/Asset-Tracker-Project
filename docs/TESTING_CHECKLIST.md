# Compliance Testing Checklist

Use this checklist to verify all requirements are working correctly.

---

## ✅ Section 2.1 - Checkout Transaction Handling

### Test 1.1: Single Asset Checkout ✓
- [ ] Attempt to checkout one asset
- [ ] Verify only one asset processed per transaction
- [ ] **Expected:** Success

### Test 1.2: Success Message ✓
- [ ] Complete a successful checkout
- [ ] Verify response contains `"success": true`
- [ ] **Expected:** "Checkout successful! Asset 'X' has been assigned to Y."

### Test 1.3: Cancellation Message ✓
- [ ] Cancel an active checkout via `POST /api/checkouts/cancel/{id}`
- [ ] Verify response message
- [ ] **Expected:** "Checkout has been successfully cancelled..."

### Test 1.4: Status Updates on Checkout ✓
- [ ] Note asset status before checkout
- [ ] Complete checkout
- [ ] Verify asset status changed to "Checked Out" immediately
- [ ] **Expected:** Status = "Checked Out"

### Test 1.5: Status Updates on Return ✓
- [ ] Complete a return via `POST /api/checkouts/return/{assetId}`
- [ ] Verify asset status changed to "Available" immediately
- [ ] **Expected:** Status = "Available"

### Test 1.6: Capture User Information ✓
- [ ] Complete a checkout
- [ ] Query checkout record
- [ ] Verify captured: UserFullName, UserEmail, CheckoutTimestamp, ExpectedReturnDate
- [ ] **Expected:** All fields populated

### Test 1.7: Expected Return Date Validation ✓
- [ ] Attempt checkout with past date: `"expectedReturnDate": "2025-01-01"`
- [ ] **Expected:** 400 Error - "Expected return date cannot be in the past."

### Test 1.8: Expected Return Date Required ✓
- [ ] Attempt checkout without ExpectedReturnDate field
- [ ] **Expected:** 400 Error - "Expected return date is required."

---

## ✅ Section 2.2 - Transaction Validation

### Test 2.1: No Asset Selected ✓
```json
POST /api/checkouts/assign
{
  "assetID": 0,
  "employeeEmail": "user@test.com",
  "expectedReturnDate": "2026-07-15"
}
```
- [ ] **Expected:** 400 Error - "No asset selected..."

### Test 2.2: Asset Not Available ✓
```json
// Try to checkout an asset that's already "Checked Out"
POST /api/checkouts/assign
{
  "assetID": [id of checked out asset],
  "employeeEmail": "user@test.com",
  "expectedReturnDate": "2026-07-15"
}
```
- [ ] **Expected:** 400 Error - "Asset is already checked out..."

### Test 2.3: Under Maintenance Rejection ✓
- [ ] Set asset to "Under Maintenance"
- [ ] Attempt to checkout
- [ ] **Expected:** 400 Error - "Asset is currently under maintenance..."

### Test 2.4: Decommissioned Asset Rejection ✓
- [ ] Decommission an asset (`DELETE /api/assets/{id}`)
- [ ] Verify status changed to "Decommissioned"
- [ ] Attempt to checkout
- [ ] **Expected:** 400 Error - "Asset has been decommissioned..."

### Test 2.5: Duplicate Checkout Prevention ✓
- [ ] User A checks out Asset 1
- [ ] User A attempts to checkout Asset 1 again
- [ ] **Expected:** 400 Error - "This user already has this asset checked out."

### Test 2.6: Missing Email Validation ✓
```json
POST /api/checkouts/assign
{
  "assetID": 1,
  "employeeEmail": "",
  "expectedReturnDate": "2026-07-15"
}
```
- [ ] **Expected:** 400 Error - "Employee email address is required."

### Test 2.7: User Not Found ✓
```json
POST /api/checkouts/assign
{
  "assetID": 1,
  "employeeEmail": "nonexistent@test.com",
  "expectedReturnDate": "2026-07-15"
}
```
- [ ] **Expected:** 404 Error - "Employee record not found."

---

## ✅ Section 2.3 - Asset & Status Management

### Test 3.1: Display Only Available Assets ✓
```http
GET /api/checkouts/available-assets
```
- [ ] Verify response contains ONLY assets with status "Available"
- [ ] Verify Checked Out, Under Maintenance, Decommissioned assets NOT included
- [ ] **Expected:** Filtered list of available assets

### Test 3.2: Duplicate Serial Number Detection ✓
- [ ] Create Asset A with serial "TEST123"
- [ ] Attempt to create Asset B with serial "TEST123"
- [ ] **Expected:** 400 Error - "Duplicate serial number detected..."
- [ ] Verify error message includes existing asset details

### Test 3.3: Duplicate Check Includes Decommissioned ✓
- [ ] Create and decommission Asset A with serial "TEST456"
- [ ] Attempt to create Asset B with serial "TEST456"
- [ ] **Expected:** 400 Error - Duplicate detected (even though first is decommissioned)

### Test 3.4: Soft Delete (Decommission) ✓
```http
DELETE /api/assets/{id}
```
- [ ] Note the AssetID before deletion
- [ ] Execute DELETE request
- [ ] Query the asset by ID: `GET /api/assets/{id}`
- [ ] **Expected:** Asset still exists with status "Decommissioned"

### Test 3.5: Cannot Delete Checked-Out Asset ✓
- [ ] Checkout an asset
- [ ] Attempt to decommission it
- [ ] **Expected:** 400 Error - "Cannot decommission a checked-out asset..."

### Test 3.6: Audit Trail for All Actions ✓
```http
GET /api/checkouts/audit-trail
```
- [ ] Perform: Create asset, Checkout, Return, Decommission
- [ ] Query audit trail
- [ ] **Expected:** All actions logged with user email, timestamp, details

### Test 3.7: Asset Restoration ✓
```http
POST /api/assets/{id}/restore
```
- [ ] Decommission an asset
- [ ] Restore it using the endpoint
- [ ] **Expected:** Status changed back to "Available"

---

## ✅ Section 2.4 - Overdue Return Reminders

### Test 4.1: Overdue Checkouts Query ✓
```http
GET /api/checkouts/overdue
```
- [ ] Create checkout with past ExpectedReturnDate
- [ ] Query overdue endpoint
- [ ] **Expected:** Checkout appears in list with daysOverdue calculated

### Test 4.2: Background Service Running ✓
- [ ] Check application logs after startup
- [ ] **Expected:** Log entry "Overdue Reminder Service started."
- [ ] **Expected:** Log entry showing next scheduled run time

### Test 4.3: Email Reminder Sent ✓
**Note:** Background service runs at 8 AM UTC. For immediate testing:
- [ ] Temporarily change schedule time in `OverdueReminderService.cs`
- [ ] Create overdue checkout
- [ ] Wait for service to run
- [ ] Check recipient email inbox
- [ ] **Expected:** Reminder email received with asset details

### Test 4.4: Email Contains Required Information ✓
Verify email includes:
- [ ] Asset Name
- [ ] Serial Number
- [ ] Checkout Date
- [ ] Expected Return Date
- [ ] Days Overdue

### Test 4.5: Admin Digest Sent ✓
- [ ] Ensure there are overdue checkouts
- [ ] Wait for daily service run
- [ ] Check admin email inbox
- [ ] **Expected:** Digest email with table of all overdue assets

### Test 4.6: Admin Digest Contains User Details ✓
Verify admin digest includes:
- [ ] User Full Name
- [ ] User Email
- [ ] Asset Name
- [ ] Serial Number
- [ ] Days Overdue for each checkout

### Test 4.7: Email Logged in Audit Trail ✓
```http
GET /api/checkouts/audit-trail
```
- [ ] After email sent, query audit trail
- [ ] Search for Action = "EMAIL_REMINDER"
- [ ] **Expected:** Entry with recipient email, timestamp, asset details

### Test 4.8: Admin Digest Logged ✓
```http
GET /api/checkouts/audit-trail
```
- [ ] After digest sent, query audit trail
- [ ] Search for Action = "ADMIN_DIGEST"
- [ ] **Expected:** Entry with summary details

### Test 4.9: Follow-Up Emails ✓
- [ ] Create overdue checkout
- [ ] Wait for first email (Day 1)
- [ ] Do not return asset
- [ ] Wait for next day's service run
- [ ] **Expected:** Second reminder email sent

---

## ✅ Section 2.5 - Reporting

### Test 5.1: Date Range Filtering ✓
```http
GET /api/reports/checkout-history?fromDate=2026-06-01&toDate=2026-06-30
```
- [ ] Query with date range
- [ ] **Expected:** Only checkouts within range returned

### Test 5.2: Single Day Report ✓
```http
GET /api/reports/checkout-history?fromDate=2026-06-15&toDate=2026-06-15
```
- [ ] Query with same from/to date
- [ ] **Expected:** Only checkouts from that specific day

### Test 5.3: Category Filtering ✓
```http
GET /api/reports/checkout-history?category=Laptops
```
- [ ] Query with specific category
- [ ] **Expected:** Only checkouts of assets in that category

### Test 5.4: All Categories (No Filter) ✓
```http
GET /api/reports/checkout-history?category=All
```
- [ ] Query with "All" category
- [ ] **Expected:** All categories included

### Test 5.5: Specific Asset Filtering ✓
```http
GET /api/reports/checkout-history?assetId=5
```
- [ ] Query with specific asset ID
- [ ] **Expected:** Only checkouts of that asset

### Test 5.6: User Filtering ✓
```http
GET /api/reports/checkout-history?userId=3
```
- [ ] Query with specific user ID
- [ ] **Expected:** Only checkouts by that user

### Test 5.7: Report Contains All Required Columns ✓
- [ ] Query any report
- [ ] Verify response includes:
  - [ ] Checkout Date
  - [ ] Asset Name
  - [ ] Category
  - [ ] Serial Number
  - [ ] User Full Name
  - [ ] User Email
  - [ ] Return Date
  - [ ] Status

### Test 5.8: Excel Export ✓
```http
GET /api/reports/checkout-history/excel?fromDate=2026-06-01&toDate=2026-06-30
```
- [ ] Request Excel export
- [ ] Open downloaded file
- [ ] **Expected:** .xlsx file with all required columns and data

### Test 5.9: PDF Export ✓
```http
GET /api/reports/checkout-history/pdf?fromDate=2026-06-01&toDate=2026-06-30
```
- [ ] Request PDF export
- [ ] Open downloaded file
- [ ] **Expected:** .pdf file with formatted report

---

## 🔧 Additional Integration Tests

### Test I.1: End-to-End Checkout Flow ✓
1. [ ] Query available assets
2. [ ] Select an available asset
3. [ ] Checkout with valid future return date
4. [ ] Verify asset no longer in available list
5. [ ] Verify checkout in "my-assets"
6. [ ] Return asset
7. [ ] Verify asset back in available list

### Test I.2: Overdue Flow ✓
1. [ ] Create checkout with past return date (manually in DB)
2. [ ] Query overdue endpoint
3. [ ] Wait for email service run
4. [ ] Verify email sent and logged
5. [ ] Return asset
6. [ ] Verify no longer in overdue list

### Test I.3: Admin Override ✓
1. [ ] User checks out asset
2. [ ] Admin forces return: `POST /api/assets/{id}/admin-return`
3. [ ] Verify asset available
4. [ ] Verify checkout status = "Returned"
5. [ ] Verify audit trail logged

### Test I.4: Maintenance Workflow ✓
1. [ ] Set asset to maintenance: `PUT /api/assets/{id}/maintenance`
2. [ ] Attempt checkout
3. [ ] Verify rejection
4. [ ] Clear maintenance status
5. [ ] Successfully checkout asset

### Test I.5: Decommission and Restore ✓
1. [ ] Decommission asset: `DELETE /api/assets/{id}`
2. [ ] Attempt checkout
3. [ ] Verify rejection
4. [ ] Restore asset: `POST /api/assets/{id}/restore`
5. [ ] Successfully checkout asset

---

## 📊 Database Integrity Checks

### Check D.1: No Orphaned Checkouts ✓
```sql
SELECT * FROM Checkouts c
LEFT JOIN Assets a ON c.AssetID = a.AssetID
WHERE a.AssetID IS NULL;
```
- [ ] **Expected:** 0 results

### Check D.2: Status Consistency ✓
```sql
SELECT * FROM Assets 
WHERE Status = 'Checked Out' 
  AND AssetID NOT IN (
    SELECT AssetID FROM Checkouts WHERE Status = 'Active'
  );
```
- [ ] **Expected:** 0 results

### Check D.3: No Duplicate Active Checkouts ✓
```sql
SELECT UserID, AssetID, COUNT(*) as Count
FROM Checkouts
WHERE Status = 'Active'
GROUP BY UserID, AssetID
HAVING COUNT(*) > 1;
```
- [ ] **Expected:** 0 results

### Check D.4: All Active Checkouts Have Future/Present Return Dates ✓
```sql
SELECT * FROM Checkouts
WHERE Status = 'Active'
  AND ExpectedReturnDate IS NULL;
```
- [ ] **Expected:** 0 results (or manually set for testing)

---

## 📝 Test Data Setup

### Create Test Assets
```sql
INSERT INTO Assets (Name, Category, SerialNumber, Status, PurchaseDate, Value)
VALUES 
('Test Laptop 1', 'Laptops', 'TL001', 'Available', GETUTCDATE(), 1200.00),
('Test Laptop 2', 'Laptops', 'TL002', 'Available', GETUTCDATE(), 1200.00),
('Test Monitor', 'Monitors', 'TM001', 'Available', GETUTCDATE(), 300.00),
('Test Phone', 'Mobile Devices', 'TP001', 'Under Maintenance', GETUTCDATE(), 800.00),
('Old Laptop', 'Laptops', 'OL001', 'Decommissioned', GETUTCDATE(), 500.00);
```

### Create Test User
```sql
INSERT INTO Users (FullName, Email, PasswordHash, Role, PhoneNumber)
VALUES ('Test User', 'testuser@test.com', 'hashedpassword', 'Employee', '1234567890');
```

### Create Overdue Checkout (for testing emails)
```sql
-- First checkout normally, then update the date
INSERT INTO Checkouts (AssetID, UserID, UserFullName, UserEmail, CheckoutTimestamp, ExpectedReturnDate, Status)
VALUES (1, 1, 'Test User', 'testuser@test.com', GETUTCDATE(), DATEADD(day, -3, GETUTCDATE()), 'Active');

-- Update asset status
UPDATE Assets SET Status = 'Checked Out', AssignedTo = 'Test User' WHERE AssetID = 1;
```

---

## ✅ Final Compliance Verification

Once all tests pass:

- [ ] All Section 2.1 tests passed
- [ ] All Section 2.2 tests passed
- [ ] All Section 2.3 tests passed
- [ ] All Section 2.4 tests passed
- [ ] All Section 2.5 tests passed
- [ ] All Integration tests passed
- [ ] All Database checks passed
- [ ] Email configuration working
- [ ] Background service running
- [ ] Audit trail comprehensive
- [ ] Documentation complete

---

## 🎉 Compliance Certification

**Date Tested:** _________________

**Tested By:** _________________

**Compliance Status:** ☐ PASS ☐ FAIL

**Notes:**
_______________________________________________________________________
_______________________________________________________________________
_______________________________________________________________________

**Issues Found:**
_______________________________________________________________________
_______________________________________________________________________
_______________________________________________________________________

**Signature:** _________________
