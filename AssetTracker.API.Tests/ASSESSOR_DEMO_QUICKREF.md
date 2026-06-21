# 🎯 ASSESSOR DEMO - QUICK REFERENCE CARD

**Print this page and keep it next to your laptop during assessment**

---

## ⚡ DEMO SEQUENCE (8 minutes total)

### **DEMO 1: Authorization (1 min)** 🔒
**What to show:** Standard User can't create assets  
**How:**
1. Swagger → Authorize with Standard User token
2. Try `POST /api/assets` 
3. **Expected:** 403 Forbidden
4. **Say:** "Server-side role enforcement - can't bypass from frontend"

---

### **DEMO 2: Checkout + Database (2 min)** 💾
**What to show:** Checkout updates asset, creates record, writes audit  
**How:**
1. SQL BEFORE: `SELECT * FROM Assets WHERE AssetID = 5;`
   - Show: Available, NULL assignedTo
2. Swagger: `POST /api/checkouts/assign` with valid data
3. SQL AFTER: 
   ```sql
   SELECT * FROM Assets WHERE AssetID = 5;
   SELECT * FROM Checkouts WHERE AssetID = 5;
   SELECT * FROM AuditTrails WHERE AssetID = 5 ORDER BY Timestamp DESC;
   ```
   - Show: Checked Out, user name, checkout record, audit entry
4. **Say:** "One transaction updates three tables atomically"

---

### **DEMO 3: Illegal Checkout (1 min)** ❌
**What to show:** Can't checkout unavailable asset  
**How:**
1. Try `POST /api/checkouts/assign` with already checked-out asset
2. **Expected:** 400/409 with clear error
3. SQL: Show asset unchanged
4. **Say:** "API validates business rules - frontend can't bypass"

---

### **DEMO 4: Decommission + Restore (2 min)** ♻️
**What to show:** Soft delete preserves history  
**How:**
1. `DELETE /api/assets/{id}` (soft delete)
2. SQL: `SELECT * FROM Assets WHERE AssetID = ?;`
   - Show: Row exists, Status = Decommissioned
3. `POST /api/assets/{id}/restore`
4. SQL: Show Status = Available
5. SQL Audit: `SELECT Action FROM AuditTrails WHERE AssetID = ?;`
   - Show: DECOMMISSION and RESTORE entries
6. **Say:** "Row never deleted - all history preserved"

---

### **DEMO 5: User Rejection (1 min)** 📝
**What to show:** Rejection reason stored and audited  
**How:**
1. `POST /api/users/{id}/reject` 
   ```json
   { "rejectionReason": "Invalid email domain" }
   ```
2. SQL: 
   ```sql
   SELECT AccountStatus, RejectionReason FROM Users WHERE UserID = ?;
   ```
   - Show: Rejected, "Invalid email domain"
3. SQL Audit: `SELECT * FROM AuditTrails WHERE Action = 'REJECT_USER';`
4. **Say:** "Rejection reason required, stored, and audited"

---

### **DEMO 6: Report Export (1 min)** 📊
**What to show:** Reports filter and export to Excel  
**How:**
1. `GET /api/reports/checkout-history/excel?fromDate=2025-06-01&toDate=2025-06-30`
2. Show file downloads
3. Open Excel file
4. Show HTTP headers (Postman/DevTools):
   ```
   Content-Type: application/vnd.openxmlformats-officedocument.spreadsheetml.sheet
   ```
5. **Say:** "Filtered data exports with proper MIME types"

---

## 📋 PRE-DEMO CHECKLIST

- [ ] API running: `dotnet run` in AssetTracker.API folder
- [ ] Swagger open: `https://localhost:7xxx/swagger`
- [ ] SQL Server Management Studio open (or VS SQL Server Object Explorer)
- [ ] Test database has at least:
  - [ ] 3 assets (1 Available, 1 Checked Out, 1 Decommissioned)
  - [ ] 3 users (1 Admin, 1 Asset Manager, 1 Standard User, 1 Pending)
  - [ ] 2 checkouts (1 Active, 1 overdue)
- [ ] JWT tokens ready for all 3 user types
- [ ] This printout + highlighter

---

## 🗄️ QUICK SQL QUERIES (Copy-Paste Ready)

### Before Checkout:
```sql
SELECT AssetID, Status, AssignedTo FROM Assets WHERE AssetID = 5;
```

### After Checkout:
```sql
SELECT AssetID, Status, AssignedTo FROM Assets WHERE AssetID = 5;
SELECT * FROM Checkouts WHERE AssetID = 5 AND Status = 'Active';
SELECT * FROM AuditTrails WHERE AssetID = 5 ORDER BY Timestamp DESC;
```

### Decommission Check:
```sql
SELECT AssetID, Name, Status FROM Assets WHERE AssetID = ?;
```

### Audit Trail:
```sql
SELECT Action, AssetName, PerformedByEmail, Timestamp 
FROM AuditTrails 
ORDER BY Timestamp DESC;
```

### Rejection Reason:
```sql
SELECT FullName, AccountStatus, RejectionReason 
FROM Users 
WHERE AccountStatus = 'Rejected';
```

### Soft Delete Proof:
```sql
SELECT AssetID, Name, SerialNumber, Status 
FROM Assets 
WHERE Status = 'Decommissioned';
-- Should show rows, not empty result
```

---

## 💬 TOUGH QUESTION RESPONSES

### Q: "Can users bypass authorization?"
**A:** "No. All controllers have `[Authorize]` attributes enforced server-side. Even if frontend is hacked, API returns 403. Watch..." *(Show Swagger demo)*

### Q: "What prevents hard deletes?"
**A:** "No hard DELETE exists. Decommission endpoint only changes status. Watch the SQL query..." *(Show database row persists)*

### Q: "Can status be changed directly?"
**A:** "No. PUT endpoint now validates status hasn't changed. It forces use of proper endpoints. Watch..." *(Show 400 error)*

### Q: "How do you prevent duplicate serials?"
**A:** "Checked on both create AND edit. Includes decommissioned assets. Watch..." *(Show duplicate rejection)*

### Q: "What if user's name changes after checkout?"
**A:** "Checkout stores name and email as direct strings, not FK lookups. They're snapshots frozen at checkout time." *(Show Checkout model)*

### Q: "Are all mutations audited?"
**A:** "Yes. Every create, edit, assign, return, decommission, restore, and user action writes to AuditTrails. Watch..." *(Show audit table)*

---

## 🚨 IF SOMETHING GOES WRONG

### API won't start:
1. Check appsettings.json: ConnectionString correct?
2. Run: `dotnet build` (should succeed)
3. Check SQL Server is running

### 401 Unauthorized:
1. Token expired? Login again
2. Token copied correctly? (no extra spaces)
3. Clicked "Authorize" button in Swagger?

### 403 Forbidden (when shouldn't be):
1. Using correct user role?
2. Token claims have correct role?
3. Check User table: Role field correct?

### Database connection fails:
1. SQL Server running?
2. ConnectionString in appsettings.json correct?
3. Database "AssetTrackerDB" exists?
4. Run migrations: `dotnet ef database update`

### Swagger not loading:
1. HTTPS certificate trusted?
2. Try HTTP URL instead (check Program.cs)
3. Clear browser cache

---

## ⏱️ TIMING GUIDE

| Demo | Time | Total |
|------|------|-------|
| Intro | 0:30 | 0:30 |
| Demo 1 - Auth | 1:00 | 1:30 |
| Demo 2 - Checkout | 2:00 | 3:30 |
| Demo 3 - Illegal | 1:00 | 4:30 |
| Demo 4 - Decommission | 2:00 | 6:30 |
| Demo 5 - Reject | 1:00 | 7:30 |
| Demo 6 - Export | 1:00 | 8:30 |
| Questions | 1:30 | 10:00 |

**Target: 10 minutes total**

---

## 📸 BACKUP EVIDENCE (If demo fails)

Have these screenshots ready:
1. Swagger showing all endpoints
2. 403 Forbidden response for Standard User
3. Database before/after checkout
4. Audit trail entries
5. Decommissioned asset still in database
6. Excel file with data

---

## 🎯 CONFIDENCE BOOSTERS

✅ **20/20 rubric tests implemented**  
✅ **Build successful**  
✅ **All critical features tested**  
✅ **Code is clean and well-commented**  
✅ **You know this codebase inside-out**  

**You've got this! Take a deep breath and demonstrate with confidence.** 💪

---

**Print Date:** ____________  
**Assessment Date:** ____________  
**Assessor Name:** ____________  

**Good luck! You're ready.** 🚀
