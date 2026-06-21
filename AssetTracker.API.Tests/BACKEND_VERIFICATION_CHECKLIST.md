# Asset Tracker API - Backend Verification Checklist

**Project:** Internal Asset Tracker  
**Date:** _______________  
**Tester:** _______________  
**Environment:** Visual Studio 2026 / .NET 8  

---

## 🔐 AUTHENTICATION & AUTHORIZATION

### Test 1: API Startup and Profile Check
**Pass Criteria:** API runs on HTTPS without startup exceptions, Swagger loads, DB connection succeeds.

- [ ] API starts without exceptions
- [ ] HTTPS endpoint accessible
- [ ] Swagger UI loads at `/swagger`
- [ ] Database connection successful
- [ ] All controllers registered

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

### Test 2: Authentication Baseline
**Pass Criteria:** Unauthenticated calls to protected endpoints return 401.

**Test Endpoints:**
- [ ] `GET /api/assets` → 401 without token
- [ ] `GET /api/users` → 401 without token
- [ ] `POST /api/checkouts` → 401 without token
- [ ] `GET /api/reports/usage` → 401 without token

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

### Test 3: Role Enforcement Matrix
**Pass Criteria:** Standard User token gets 403 on admin/manager endpoints; Admin token succeeds where expected.

**Standard User Token Tests:**
- [ ] `GET /api/users` → 403 (Admin/Manager only)
- [ ] `PUT /api/users/{id}/approve` → 403 (Admin only)
- [ ] `PUT /api/users/{id}/role` → 403 (Admin only)
- [ ] `POST /api/assets` → 403 (Manager/Admin only)
- [ ] `DELETE /api/assets/{id}` → 403 (Admin only)

**Asset Manager Token Tests:**
- [ ] `POST /api/assets` → 200/201 (Can create assets)
- [ ] `PUT /api/assets/{id}` → 200 (Can edit assets)
- [ ] `GET /api/users` → 200 (Can view users)
- [ ] `PUT /api/users/{id}/approve` → 403 (Cannot approve users)
- [ ] `PUT /api/users/{id}/role` → 403 (Cannot change roles)

**Admin Token Tests:**
- [ ] `GET /api/users` → 200
- [ ] `PUT /api/users/{id}/approve` → 200
- [ ] `PUT /api/users/{id}/role` → 200
- [ ] `POST /api/assets` → 200/201
- [ ] `DELETE /api/assets/{id}/decommission` → 200

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

### Test 4: Route-Guard Bypass Test
**Pass Criteria:** Direct API calls with low-privilege token still blocked even if frontend route is hidden/guarded.

**Test Scenario:** Use Postman/Swagger to call protected endpoints directly
- [ ] Standard User token + `POST /api/assets` → 403
- [ ] Standard User token + `DELETE /api/users/{id}` → 403
- [ ] Standard User token + `PUT /api/users/{id}/role` → 403
- [ ] Unverified User token + any protected endpoint → 401/403

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

## 📦 ASSET STATE MACHINE & BUSINESS LOGIC

### Test 5: Checkout State Machine Enforcement
**Pass Criteria:** Checkout only allowed from Available; checkout from Checked Out, Under Maintenance, or Decommissioned returns 400/409 with clear error.

**Test Cases:**
- [ ] Checkout asset with status `Available` → 200/201 (Success)
- [ ] Checkout asset with status `Checked Out` → 400/409 + error message
- [ ] Checkout asset with status `Under Maintenance` → 400/409 + error message
- [ ] Checkout asset with status `Decommissioned` → 400/409 + error message

**Expected Error Messages:**
```
Asset is already checked out
Asset is under maintenance and cannot be checked out
Asset is decommissioned and cannot be checked out
```

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

### Test 6: Return Transition Enforcement
**Pass Criteria:** Successful return always restores status to Available and clears assignment.

**Before State:**
- Asset Status: `Checked Out`
- Assigned To: `user@example.com`
- Expected Return Date: `2025-06-20`

**Test:**
- [ ] `POST /api/checkouts/{id}/return` → 200
- [ ] Asset Status → `Available`
- [ ] Assigned To → `null`
- [ ] Expected Return Date → `null`
- [ ] Checkout record marked as returned with return date

**Database Verification:**
```sql
SELECT AssetID, Status, AssignedTo, ExpectedReturnDate 
FROM Assets WHERE AssetID = ?

SELECT CheckoutID, ReturnDate, ActualReturnDate 
FROM Checkouts WHERE CheckoutID = ?
```

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

### Test 7: Decommission as Soft Delete
**Pass Criteria:** Decommission marks status and keeps DB row/history; restore returns it to Available.

**Test:**
- [ ] Create asset with status `Available`
- [ ] `PUT /api/assets/{id}/decommission` → 200
- [ ] Asset Status → `Decommissioned`
- [ ] Asset row still exists in database
- [ ] Asset history preserved
- [ ] `PUT /api/assets/{id}/restore` → 200
- [ ] Asset Status → `Available`

**Database Verification:**
```sql
-- After decommission
SELECT * FROM Assets WHERE AssetID = ?
-- Row should exist with Status = 'Decommissioned'

-- After restore
SELECT * FROM Assets WHERE AssetID = ?
-- Row should exist with Status = 'Available'
```

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

### Test 8: Illegal Transition Rejection
**Pass Criteria:** Crafted requests cannot push impossible states (e.g., Decommissioned directly to Checked Out).

**Test Cases:**
- [ ] Attempt checkout of Decommissioned asset → 400/409
- [ ] Attempt to set asset status to invalid value → 400
- [ ] Attempt to checkout asset already checked out → 409
- [ ] Attempt to return asset that isn't checked out → 400
- [ ] Attempt to restore asset that isn't decommissioned → 400

**Raw JSON Payload Test:**
```json
PUT /api/assets/{id}
{
  "status": "Checked Out",
  "assignedTo": "attacker@example.com"
}
```
Should be rejected if asset is Decommissioned.

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

## ✅ VALIDATION & DATA INTEGRITY

### Test 9: Server-Side Date Validation
**Pass Criteria:** Expected return date in past is rejected by API even if client tries to send it.

**Test:**
```json
POST /api/checkouts
{
  "assetId": 1,
  "assignedTo": "user@example.com",
  "assignedToFullName": "John Doe",
  "expectedReturnDate": "2020-01-01"  // Past date
}
```

- [ ] Request rejected with 400
- [ ] Error message: "Expected return date cannot be in the past"
- [ ] No checkout record created in database

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

### Test 10: Required-Field Validation
**Pass Criteria:** Missing full name/email/date on checkout is rejected server-side with specific reason.

**Test Cases:**
```json
POST /api/checkouts (Missing assignedTo)
{ "assetId": 1, "assignedToFullName": "John", "expectedReturnDate": "2025-12-31" }
→ 400 + "Assigned To email is required"

POST /api/checkouts (Missing assignedToFullName)
{ "assetId": 1, "assignedTo": "user@example.com", "expectedReturnDate": "2025-12-31" }
→ 400 + "Assigned To Full Name is required"

POST /api/checkouts (Missing expectedReturnDate)
{ "assetId": 1, "assignedTo": "user@example.com", "assignedToFullName": "John" }
→ 400 + "Expected Return Date is required"
```

- [ ] Missing `assignedTo` → 400 with specific error
- [ ] Missing `assignedToFullName` → 400 with specific error
- [ ] Missing `expectedReturnDate` → 400 with specific error
- [ ] Empty string values also rejected

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

### Test 11: Snapshot Correctness at Checkout
**Pass Criteria:** Checkout stores user name and email snapshot at transaction time (not only a mutable foreign key lookup).

**Test:**
1. Create checkout for user `john@example.com` with name `John Doe`
2. Verify checkout record stores:
   - [ ] `AssignedTo` = `john@example.com`
   - [ ] `AssignedToFullName` = `John Doe`
   - [ ] Values stored directly in Checkouts table
3. Change user's name in Users table to `John Smith`
4. Query checkout record
   - [ ] `AssignedToFullName` still shows `John Doe` (snapshot preserved)

**Database Verification:**
```sql
SELECT AssignedTo, AssignedToFullName, CheckoutDate 
FROM Checkouts WHERE CheckoutID = ?
-- Should show original values, not current user values
```

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

### Test 12: Serial Uniqueness Integrity
**Pass Criteria:** Create/edit rejects duplicate serial numbers at API/data layer, including collisions with decommissioned assets.

**Test:**
1. Create Asset A with SerialNumber = `ABC123`
2. Attempt to create Asset B with SerialNumber = `ABC123`
   - [ ] → 400/409 + "Serial number already exists"
3. Attempt to update existing Asset C to SerialNumber = `ABC123`
   - [ ] → 400/409 + "Serial number already exists"
4. Decommission Asset A
5. Attempt to create Asset D with SerialNumber = `ABC123`
   - [ ] → 400/409 (if business rule prevents reuse)
   - [ ] → 200 (if business rule allows reuse)

**Database Constraint:**
```sql
-- Should have unique constraint or index on SerialNumber
```

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

## 📝 AUDIT TRAIL & LOGGING

### Test 13: Audit Trail on Asset Actions
**Pass Criteria:** Audit row written for create, edit, assign, return, decommission, restore, maintenance toggles, force return.

**Test Each Action:**
- [ ] Create asset → Audit entry with Action = "Asset Created"
- [ ] Edit asset → Audit entry with Action = "Asset Updated"
- [ ] Checkout asset → Audit entry with Action = "Asset Checked Out"
- [ ] Return asset → Audit entry with Action = "Asset Returned"
- [ ] Decommission asset → Audit entry with Action = "Asset Decommissioned"
- [ ] Restore asset → Audit entry with Action = "Asset Restored"
- [ ] Toggle maintenance → Audit entry with Action = "Asset Maintenance Status Changed"
- [ ] Force return → Audit entry with Action = "Asset Force Returned"

**Database Verification:**
```sql
SELECT Action, AssetID, PerformedBy, Timestamp, Details 
FROM AuditTrails 
WHERE AssetID = ? 
ORDER BY Timestamp DESC
```

**Verify each audit entry contains:**
- [ ] Correct Action description
- [ ] AssetID reference
- [ ] PerformedBy (user email/ID)
- [ ] Timestamp
- [ ] Relevant Details (JSON or text)

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

### Test 14: Audit Trail on User Admin Actions
**Pass Criteria:** Audit row written for approve, reject, role change, activate/deactivate, reset password.

**Test Each Action:**
- [ ] Approve user → Audit entry with Action = "User Approved"
- [ ] Reject user → Audit entry with Action = "User Rejected" + reason
- [ ] Change user role → Audit entry with Action = "User Role Changed"
- [ ] Activate user → Audit entry with Action = "User Activated"
- [ ] Deactivate user → Audit entry with Action = "User Deactivated"
- [ ] Reset password → Audit entry with Action = "Password Reset"

**Database Verification:**
```sql
SELECT Action, UserID, PerformedBy, Timestamp, Details 
FROM AuditTrails 
WHERE UserID = ? 
ORDER BY Timestamp DESC
```

**Verify each audit entry contains:**
- [ ] Correct Action description
- [ ] UserID reference (target user)
- [ ] PerformedBy (admin who performed action)
- [ ] Timestamp
- [ ] Relevant Details (old/new role, rejection reason, etc.)

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

### Test 15: Reject-with-Reason Persistence
**Pass Criteria:** Rejection reason is stored and retrievable for admins.

**Test:**
1. Admin rejects user with reason: "Invalid company email domain"
   ```json
   PUT /api/users/{id}/reject
   {
     "reason": "Invalid company email domain"
   }
   ```
2. Verify:
   - [ ] User status changed to `Rejected`
   - [ ] Rejection reason stored in Users table or related table
   - [ ] Audit entry created with rejection reason
   - [ ] Admin can retrieve rejection reason via API

**Database Verification:**
```sql
SELECT Email, IsApproved, RejectionReason 
FROM Users 
WHERE UserID = ?
```

**API Verification:**
```
GET /api/users/{id}
Response should include: "rejectionReason": "Invalid company email domain"
```

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

## 📊 REPORTS & EXPORT

### Test 16: Reports Filter Semantics
**Pass Criteria:** From/to date handling supports single-day range; category/asset/user default to All; filtered data matches expected rows.

**Test Cases:**
- [ ] Single-day range: `fromDate=2025-06-15, toDate=2025-06-15` → Returns only that day
- [ ] Date range: `fromDate=2025-06-01, toDate=2025-06-30` → Returns all in range
- [ ] Category filter: `category=Laptop` → Returns only laptops
- [ ] Asset filter: `assetId=5` → Returns only asset 5's history
- [ ] User filter: `userId=10` → Returns only user 10's checkouts
- [ ] No filters (All) → Returns all records
- [ ] Combined filters work correctly

**Manual Verification:**
Compare API results with direct SQL query:
```sql
SELECT * FROM Checkouts 
WHERE CheckoutDate BETWEEN '2025-06-01' AND '2025-06-30'
AND AssetID IN (SELECT AssetID FROM Assets WHERE Category = 'Laptop')
```

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

### Test 17: Export Endpoints
**Pass Criteria:** Excel and PDF endpoints return real files, correct content type, non-empty payload, and match preview/filter criteria.

**Excel Export Test:**
- [ ] `GET /api/reports/usage/export?format=excel&fromDate=...&toDate=...`
- [ ] Response Content-Type: `application/vnd.openxmlformats-officedocument.spreadsheetml.sheet`
- [ ] Response Content-Disposition: `attachment; filename="..."`
- [ ] File size > 0 bytes
- [ ] File opens in Excel without errors
- [ ] Data matches filter criteria
- [ ] Column headers correct
- [ ] All expected rows present

**PDF Export Test:**
- [ ] `GET /api/reports/usage/export?format=pdf&fromDate=...&toDate=...`
- [ ] Response Content-Type: `application/pdf`
- [ ] Response Content-Disposition: `attachment; filename="..."`
- [ ] File size > 0 bytes
- [ ] File opens in PDF reader without errors
- [ ] Data matches filter criteria
- [ ] Formatting and layout correct

**Filter Consistency Test:**
- [ ] Preview and export return same row count for identical filters

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

## 📧 BACKGROUND JOBS & REMINDERS

### Test 18: Overdue Reminder Scheduler
**Pass Criteria:** Background job runs; due-date reminders and daily overdue reminders are sent/logged; admin daily digest generated.

**Test Setup:**
1. Create checkout with `expectedReturnDate` = tomorrow
2. Create checkout with `expectedReturnDate` = yesterday (overdue)
3. Wait for scheduled background job to run OR trigger manually

**Verification:**
- [ ] Background service `OverdueReminderService` is registered in Program.cs
- [ ] Service runs on schedule (check logs)
- [ ] Due-date reminders sent 1 day before return date
- [ ] Daily overdue reminders sent for overdue checkouts
- [ ] Admin daily digest generated with overdue summary
- [ ] Email logs show sent reminders

**Background Service Check:**
```csharp
// In Program.cs
builder.Services.AddHostedService<OverdueReminderService>();
```

**Log Verification:**
```
Check console or log files for:
- "Overdue reminder service started"
- "Sent due-date reminder to user@example.com"
- "Sent overdue reminder for asset ABC123"
- "Sent admin digest with X overdue assets"
```

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

### Test 19: Reminder Audit Logging
**Pass Criteria:** Every reminder/digest write appears in audit trail with timestamp and target context.

**Test:**
1. Trigger reminder for overdue asset
2. Verify audit entry created:
   - [ ] Action = "Overdue Reminder Sent"
   - [ ] AssetID or CheckoutID referenced
   - [ ] PerformedBy = "System" or "OverdueReminderService"
   - [ ] Timestamp = when reminder sent
   - [ ] Details include recipient email

3. Trigger admin digest
4. Verify audit entry created:
   - [ ] Action = "Admin Digest Sent"
   - [ ] Details include count of overdue assets
   - [ ] Timestamp = when digest sent

**Database Verification:**
```sql
SELECT Action, AssetID, PerformedBy, Timestamp, Details 
FROM AuditTrails 
WHERE Action LIKE '%Reminder%' OR Action LIKE '%Digest%'
ORDER BY Timestamp DESC
```

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

## 🗄️ DATABASE PERSISTENCE

### Test 20: SQL Persistence Proof
**Pass Criteria:** For each key mutation, you can show before/after DB state changed as expected and references remain consistent.

**Test Scenarios:**

**Scenario A: Asset Checkout**
```sql
-- BEFORE
SELECT * FROM Assets WHERE AssetID = 5;
-- Status = 'Available', AssignedTo = NULL

SELECT COUNT(*) FROM Checkouts WHERE AssetID = 5 AND ReturnDate IS NULL;
-- 0 active checkouts

-- PERFORM: POST /api/checkouts { assetId: 5, ... }

-- AFTER
SELECT * FROM Assets WHERE AssetID = 5;
-- Status = 'Checked Out', AssignedTo = 'user@example.com'

SELECT COUNT(*) FROM Checkouts WHERE AssetID = 5 AND ReturnDate IS NULL;
-- 1 active checkout
```

**Scenario B: User Approval**
```sql
-- BEFORE
SELECT IsApproved FROM Users WHERE UserID = 10;
-- IsApproved = 0 (false)

-- PERFORM: PUT /api/users/10/approve

-- AFTER
SELECT IsApproved FROM Users WHERE UserID = 10;
-- IsApproved = 1 (true)
```

**Scenario C: Asset Decommission**
```sql
-- BEFORE
SELECT Status FROM Assets WHERE AssetID = 3;
-- Status = 'Available'

-- PERFORM: PUT /api/assets/3/decommission

-- AFTER
SELECT Status FROM Assets WHERE AssetID = 3;
-- Status = 'Decommissioned'

SELECT COUNT(*) FROM Assets WHERE AssetID = 3;
-- 1 (row still exists, not deleted)
```

**Referential Integrity Check:**
- [ ] Foreign key constraints enforced
- [ ] Cascade rules work as expected
- [ ] No orphaned records after operations

**Result:** ⬜ PASS  ⬜ FAIL  
**Notes:**  
```
_____________________________________________________________
```

---

## 📋 MINIMAL TEST SET FOR ASSESSOR DEMONSTRATION

**Quick demonstration covering all major rubric areas:**

### Demo Test 1: Standard User Authorization Denied
- [ ] Login as Standard User
- [ ] Call `POST /api/assets` → 403 Forbidden
- [ ] Show Swagger/Postman response with 403 status

**Evidence:**
```
_____________________________________________________________
```

---

### Demo Test 2: Successful Checkout Updates DB and Audit
- [ ] Check database before: Asset status = Available, no checkouts
- [ ] Call `POST /api/checkouts` with valid data → 201 Created
- [ ] Check database after: Asset status = Checked Out, checkout record exists
- [ ] Check audit trail: "Asset Checked Out" entry exists

**Evidence:**
```
Before:
SELECT * FROM Assets WHERE AssetID = ?
SELECT * FROM Checkouts WHERE AssetID = ?

After:
SELECT * FROM Assets WHERE AssetID = ?
SELECT * FROM Checkouts WHERE AssetID = ?
SELECT * FROM AuditTrails WHERE AssetID = ?
```

**Result:** ⬜ PASS  ⬜ FAIL

---

### Demo Test 3: Illegal Checkout Rejected by API
- [ ] Attempt to checkout asset with status "Checked Out"
- [ ] API returns 400/409 with error message
- [ ] No changes made to database
- [ ] Frontend did not prevent this (API protection works)

**Evidence:**
```
POST /api/checkouts
{
  "assetId": <already checked out asset>
}

Response: 409 Conflict
{
  "error": "Asset is already checked out"
}
```

**Result:** ⬜ PASS  ⬜ FAIL

---

### Demo Test 4: Decommission and Restore Audited
- [ ] Decommission asset → Status changes to Decommissioned
- [ ] Check audit trail: "Asset Decommissioned" entry
- [ ] Restore asset → Status changes to Available
- [ ] Check audit trail: "Asset Restored" entry
- [ ] Asset row never deleted from database

**Evidence:**
```sql
SELECT Status FROM Assets WHERE AssetID = ?
-- Before: Available → After Decommission: Decommissioned → After Restore: Available

SELECT Action, Timestamp FROM AuditTrails WHERE AssetID = ?
-- Should show both "Asset Decommissioned" and "Asset Restored"
```

**Result:** ⬜ PASS  ⬜ FAIL

---

### Demo Test 5: Approve and Reject-with-Reason Audited
- [ ] Approve user → IsApproved = true, audit entry created
- [ ] Reject user with reason → IsApproved = false, reason stored, audit entry created

**Evidence:**
```sql
-- Approve
SELECT IsApproved FROM Users WHERE UserID = ?
SELECT Action, Details FROM AuditTrails WHERE UserID = ? AND Action = 'User Approved'

-- Reject
SELECT IsApproved, RejectionReason FROM Users WHERE UserID = ?
SELECT Action, Details FROM AuditTrails WHERE UserID = ? AND Action = 'User Rejected'
```

**Result:** ⬜ PASS  ⬜ FAIL

---

### Demo Test 6: Report Export Downloads Valid File
- [ ] Call `GET /api/reports/usage/export?format=excel&fromDate=2025-06-01&toDate=2025-06-30`
- [ ] File downloads with correct Content-Type
- [ ] File opens without errors
- [ ] Data matches filter criteria (manual spot check)

**Evidence:**
```
HTTP Response Headers:
Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
Content-Disposition: attachment; filename="usage_report_2025-06-01_2025-06-30.xlsx"
Content-Length: <non-zero>

File opens successfully in Excel: ⬜ YES  ⬜ NO
Row count matches expected: ⬜ YES  ⬜ NO
```

**Result:** ⬜ PASS  ⬜ FAIL

---

## 📊 SUMMARY SCORECARD

| Test Category | Tests Passed | Tests Failed | Pass Rate |
|---------------|--------------|--------------|-----------|
| Authentication & Authorization (Tests 1-4) | ___ / 4 | ___ / 4 | ___% |
| Asset State Machine (Tests 5-8) | ___ / 4 | ___ / 4 | ___% |
| Validation & Integrity (Tests 9-12) | ___ / 4 | ___ / 4 | ___% |
| Audit Trail (Tests 13-15) | ___ / 3 | ___ / 3 | ___% |
| Reports & Export (Tests 16-17) | ___ / 2 | ___ / 2 | ___% |
| Background Jobs (Tests 18-19) | ___ / 2 | ___ / 2 | ___% |
| Database Persistence (Test 20) | ___ / 1 | ___ / 1 | ___% |
| **TOTAL** | **___ / 20** | **___ / 20** | **___%** |

---

## 🎯 CRITICAL FINDINGS

**High Priority Issues:**
```
1. _____________________________________________________________
2. _____________________________________________________________
3. _____________________________________________________________
```

**Medium Priority Issues:**
```
1. _____________________________________________________________
2. _____________________________________________________________
```

**Low Priority Issues:**
```
1. _____________________________________________________________
```

---

## ✅ ASSESSOR READINESS

**Documentation Prepared:**
- [ ] This checklist completed with evidence
- [ ] Database schema diagram
- [ ] API endpoint documentation (Swagger export)
- [ ] Sample audit trail exports
- [ ] Sample report exports (Excel & PDF)

**Live Demonstration Prepared:**
- [ ] Can run all 6 minimal demo tests in < 10 minutes
- [ ] Database queries ready in SQL Management Studio / VS SQL Server Object Explorer
- [ ] Postman collection with test requests
- [ ] Known good test data set in database

**Questions to Anticipate:**
- [ ] "How do you prevent unauthorized users from accessing admin functions?"
- [ ] "What happens if a user tries to checkout an already checked out asset?"
- [ ] "How do you ensure audit trails can't be tampered with?"
- [ ] "Can you show me a decommissioned asset is never hard-deleted?"
- [ ] "How do you handle duplicate serial numbers?"
- [ ] "Where is the background job for overdue reminders configured?"

---

## 🔧 USEFUL COPILOT PROMPTS FOR FIXING ISSUES

If any tests fail, use these prompts in Visual Studio:

```
1. "Generate integration tests for all UsersController endpoints validating Authorize role behavior for Standard User, Asset Manager, Admin"

2. "Find all mutating endpoints and verify each writes an audit entry in the same transaction"

3. "Identify any endpoint where asset status can move to an illegal state and suggest guard clauses"

4. "Create tests for checkout validation: unavailable asset, decommissioned asset, missing fields, past return date, duplicate assignment"

5. "Verify decommission is implemented as soft delete and not hard delete"

6. "Add tests that assert rejection reason is required and persisted on reject user flow"

7. "Add tests for report filters and ensure export payload matches filtered result set"

8. "Locate and validate hosted background service for overdue reminders and admin digest scheduling"
```

---

## 📝 TESTING NOTES & OBSERVATIONS

```
Date: _______________
Tester: _______________

General observations:
_____________________________________________________________
_____________________________________________________________
_____________________________________________________________

Performance notes:
_____________________________________________________________
_____________________________________________________________

Security observations:
_____________________________________________________________
_____________________________________________________________

Additional findings:
_____________________________________________________________
_____________________________________________________________
_____________________________________________________________
```

---

**Sign-off:**

Tester: ________________________  Date: _______________  
Reviewer: ______________________  Date: _______________  

---

**Document Version:** 1.0  
**Last Updated:** June 2026  
**Next Review:** Before assessor demonstration  
