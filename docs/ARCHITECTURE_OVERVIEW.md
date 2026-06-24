# Asset Tracker - Implementation Architecture

## 📐 System Architecture Overview

```
┌─────────────────────────────────────────────────────────────────────┐
│                         FRONTEND (Angular)                           │
│  ┌────────────────┐  ┌────────────────┐  ┌────────────────┐       │
│  │  Checkout Page │  │  Admin Panel   │  │   My Assets    │       │
│  │  (Available)   │  │  (Overdue)     │  │   Dashboard    │       │
│  └────────┬───────┘  └────────┬───────┘  └────────┬───────┘       │
└───────────┼──────────────────┼──────────────────┼─────────────────┘
            │                   │                   │
            │   HTTP/REST API   │                   │
            ▼                   ▼                   ▼
┌─────────────────────────────────────────────────────────────────────┐
│                     ASSET TRACKER API (.NET 8)                       │
│  ┌──────────────────────────────────────────────────────────────┐  │
│  │                      CONTROLLERS                              │  │
│  │  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐      │  │
│  │  │ Checkouts    │  │   Assets     │  │   Reports    │      │  │
│  │  │ Controller   │  │  Controller  │  │  Controller  │      │  │
│  │  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘      │  │
│  └─────────┼──────────────────┼──────────────────┼─────────────┘  │
│            │                   │                   │                │
│            │  ┌────────────────┴───────────────┐  │                │
│            │  │                                 │  │                │
│            ▼  ▼                                 ▼  ▼                │
│  ┌────────────────────────────────────────────────────────────┐   │
│  │                       SERVICES                              │   │
│  │  ┌──────────────────────┐  ┌──────────────────────┐       │   │
│  │  │   Email Service      │  │  Background Service  │       │   │
│  │  │   (IEmailService)    │  │ (OverdueReminder)    │       │   │
│  │  │  • User reminders    │  │  • Daily at 8 AM     │       │   │
│  │  │  • Admin digest      │  │  • Auto-trigger      │       │   │
│  │  │  • SMTP integration  │  │  • Email logging     │       │   │
│  │  └──────────┬───────────┘  └──────────┬───────────┘       │   │
│  └─────────────┼──────────────────────────┼───────────────────┘   │
│                │                           │                        │
│                │                           │                        │
│                ▼                           ▼                        │
│  ┌─────────────────────────────────────────────────────────────┐  │
│  │                  DATABASE CONTEXT                            │  │
│  │              (ApplicationDbContext)                          │  │
│  └─────────────────────────┬───────────────────────────────────┘  │
└────────────────────────────┼──────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                    SQL SERVER DATABASE                               │
│  ┌──────────┐  ┌──────────┐  ┌──────────┐  ┌──────────────────┐  │
│  │  Users   │  │  Assets  │  │ Checkouts│  │  AuditTrail      │  │
│  │  Table   │  │  Table   │  │  Table   │  │  Table           │  │
│  └──────────┘  └──────────┘  └──────────┘  └──────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                             │
                             ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      EMAIL SERVER (SMTP)                             │
│  ┌────────────────────────────────────────────────────────────┐    │
│  │  Gmail / Outlook / Yahoo / Custom SMTP                      │    │
│  │  • User reminder emails                                     │    │
│  │  • Admin daily digest                                       │    │
│  └────────────────────────────────────────────────────────────┘    │
└─────────────────────────────────────────────────────────────────────┘
```

---

## 🔄 Checkout Flow with Validation

```
┌─────────────┐
│   User      │
│  Requests   │
│  Checkout   │
└──────┬──────┘
       │
       ▼
┌──────────────────────────────────────────────────────────────────┐
│              VALIDATION PIPELINE (10+ Checks)                     │
│                                                                   │
│  ✓ 1. Asset selected? (assetID > 0)                             │
│  ✓ 2. Asset exists in database?                                 │
│  ✓ 3. Asset status = "Available"?                               │
│  ✓ 4. Asset NOT "Checked Out"?                                  │
│  ✓ 5. Asset NOT "Under Maintenance"?                            │
│  ✓ 6. Asset NOT "Decommissioned"?                               │
│  ✓ 7. Employee email provided?                                   │
│  ✓ 8. User exists in system?                                     │
│  ✓ 9. Expected return date provided?                             │
│  ✓ 10. Return date NOT in past?                                  │
│  ✓ 11. User NOT already checked out same asset?                  │
│                                                                   │
│  ❌ ANY FAIL → Return 400 with specific error message           │
│  ✅ ALL PASS → Continue to checkout                              │
└─────────────────────────┬─────────────────────────────────────────┘
                          │
                          ▼
                  ┌───────────────┐
                  │   Create      │
                  │   Checkout    │
                  │   Record      │
                  └───────┬───────┘
                          │
                          ▼
                  ┌───────────────┐
                  │   Update      │
                  │   Asset       │
                  │   Status      │
                  └───────┬───────┘
                          │
                          ▼
                  ┌───────────────┐
                  │   Log to      │
                  │   Audit Trail │
                  └───────┬───────┘
                          │
                          ▼
                  ┌───────────────┐
                  │   Return      │
                  │   Success     │
                  │   Message     │
                  └───────────────┘
```

---

## 📧 Email Notification Flow

```
┌──────────────────────────────────────────────────────────────────┐
│         BACKGROUND SERVICE (Runs Daily at 8 AM UTC)               │
└────────────────────────────┬─────────────────────────────────────┘
                             │
                             ▼
                ┌────────────────────────┐
                │  Query Database for    │
                │  Overdue Checkouts     │
                │  (ExpectedReturnDate   │
                │   < Today)             │
                └────────┬───────────────┘
                         │
                         ▼
                ┌────────────────┐
                │  Found Any?    │
                └────┬───────────┘
                     │
         ┌───────────┴───────────┐
         │                       │
    YES  │                       │  NO
         ▼                       ▼
┌─────────────────┐      ┌──────────────┐
│  For Each       │      │  Skip Email  │
│  Overdue        │      │  Processing  │
│  Checkout:      │      └──────────────┘
└────┬────────────┘
     │
     ▼
┌─────────────────────────────────┐
│  1. Send Reminder to User       │
│     ✉ Asset details             │
│     ✉ Days overdue              │
│     ✉ Action required           │
└────┬────────────────────────────┘
     │
     ▼
┌─────────────────────────────────┐
│  2. Log Email to Audit Trail    │
│     Action: EMAIL_REMINDER      │
│     Details: Recipient, Asset   │
└────┬────────────────────────────┘
     │
     ▼
┌─────────────────────────────────┐
│  3. Compile Admin Digest        │
│     • All overdue checkouts     │
│     • User details              │
│     • Days overdue per asset    │
└────┬────────────────────────────┘
     │
     ▼
┌─────────────────────────────────┐
│  4. Send Digest to Admin        │
│     ✉ HTML Table format         │
│     ✉ Summary statistics        │
└────┬────────────────────────────┘
     │
     ▼
┌─────────────────────────────────┐
│  5. Log Digest to Audit Trail   │
│     Action: ADMIN_DIGEST        │
│     Details: Total count        │
└────┬────────────────────────────┘
     │
     ▼
┌─────────────────────────────────┐
│  Schedule Next Run              │
│  (Tomorrow at 8 AM UTC)         │
└─────────────────────────────────┘
```

---

## 🗂️ Database Schema Changes

### **No New Tables Required!** ✅

All features work with existing schema:

```sql
Users Table
├── UserID (PK)
├── FullName
├── Email
├── PasswordHash
├── Role
└── PhoneNumber

Assets Table
├── AssetID (PK)
├── Name
├── Category
├── SerialNumber ⚠️ Now checked for duplicates
├── Status ⚠️ Now soft-deleted to "Decommissioned"
├── AssignedTo
├── PurchaseDate
└── Value

Checkouts Table
├── CheckoutID (PK)
├── AssetID (FK)
├── UserID (FK)
├── UserFullName
├── UserEmail
├── CheckoutTimestamp
├── ExpectedReturnDate ⚠️ Now validated (no past dates)
├── ActualReturnDate
└── Status ⚠️ Now includes "Cancelled"

AuditTrail Table
├── AuditLogID (PK)
├── Action ⚠️ Now includes EMAIL_REMINDER, ADMIN_DIGEST
├── AssetID (nullable)
├── AssetName
├── PerformedByEmail
├── Timestamp
└── Details
```

---

## 🛡️ Validation Matrix

| Validation Check | Error Code | Error Message |
|------------------|------------|---------------|
| No asset selected | 400 | "No asset selected. Please select an asset to checkout." |
| Asset not found | 404 | "Asset not found." |
| Asset checked out | 400 | "Asset is already checked out and not available." |
| Under maintenance | 400 | "Asset is currently under maintenance and cannot be checked out." |
| Decommissioned | 400 | "Asset has been decommissioned and cannot be checked out." |
| Status not available | 400 | "Asset is currently unavailable." |
| Email missing | 400 | "Employee email address is required." |
| User not found | 404 | "Employee record not found." |
| Return date missing | 400 | "Expected return date is required." |
| Past return date | 400 | "Expected return date cannot be in the past." |
| Duplicate checkout | 400 | "This user already has this asset checked out." |
| Duplicate serial | 400 | "Duplicate serial number detected. An asset with serial number 'X' already exists..." |

---

## 📊 API Endpoints Summary

### Checkouts
- `POST /api/checkouts/assign` - Checkout asset (with validation)
- `POST /api/checkouts/return/{assetId}` - Return asset
- `POST /api/checkouts/cancel/{checkoutId}` - Cancel checkout ⭐ NEW
- `GET /api/checkouts/my-assets` - Get user's checkouts
- `GET /api/checkouts/available-assets` - Get available assets ⭐ NEW
- `GET /api/checkouts/overdue` - Get overdue checkouts ⭐ NEW
- `GET /api/checkouts/audit-trail` - Get audit trail

### Assets
- `GET /api/assets` - Get all assets
- `GET /api/assets/{id}` - Get specific asset
- `POST /api/assets` - Create asset (with duplicate check)
- `PUT /api/assets/{id}` - Update asset
- `DELETE /api/assets/{id}` - Decommission asset (soft delete) ⭐ UPDATED
- `POST /api/assets/{id}/restore` - Restore decommissioned asset
- `PUT /api/assets/{id}/maintenance` - Set maintenance status
- `POST /api/assets/{id}/admin-return` - Admin override return
- `GET /api/assets/dashboard-metrics` - Dashboard metrics

### Reports
- `GET /api/reports/checkout-history` - Get checkout history
- `GET /api/reports/checkout-history/excel` - Download Excel
- `GET /api/reports/checkout-history/pdf` - Download PDF

---

## 🎯 Key Improvements

### Before Implementation
```
❌ Basic validation only
❌ No email notifications
❌ Hard delete (data loss)
❌ Shows unavailable assets
❌ No duplicate prevention
❌ Limited audit trail
❌ No overdue tracking
```

### After Implementation
```
✅ 10+ comprehensive validations
✅ Automated email system
✅ Soft delete (data preserved)
✅ Filtered available assets
✅ Duplicate serial detection
✅ Complete audit trail with email logs
✅ Real-time overdue monitoring
```

---

## 📁 Project Structure

```
InternalAssetTracker/
├── AssetTracker.API/
│   ├── BackgroundServices/
│   │   └── OverdueReminderService.cs ⭐ NEW
│   ├── Controller/
│   │   ├── AssetsController.cs ⚠️ UPDATED
│   │   ├── CheckoutsController.cs ⚠️ UPDATED
│   │   ├── ReportsController.cs
│   │   ├── AuthController.cs
│   │   └── UsersController.cs
│   ├── Data/
│   │   └── ApplicationDbContext.cs
│   ├── Models/
│   │   ├── Asset.cs
│   │   ├── Checkout.cs
│   │   ├── User.cs
│   │   └── AuditTrail.cs
│   ├── Services/
│   │   ├── IEmailService.cs ⭐ NEW
│   │   └── EmailService.cs ⭐ NEW
│   ├── Program.cs ⚠️ UPDATED
│   └── appsettings.json ⚠️ UPDATED
│
├── Documentation/ (Root)
│   ├── README_IMPLEMENTATION_COMPLETE.md ⭐ START HERE
│   ├── COMPLIANCE_IMPLEMENTATION_SUMMARY.md
│   ├── EMAIL_SETUP_GUIDE.md
│   ├── TESTING_CHECKLIST.md
│   ├── API_QUICK_REFERENCE.md
│   └── Database_Migration_Compliance_Check.sql
│
└── [Frontend Project]
```

---

## 🔐 Security & Compliance

### Data Protection
- ✅ Soft delete preserves audit trail
- ✅ All actions logged with timestamp and user
- ✅ Email sends logged for compliance
- ✅ Duplicate prevention protects data integrity

### Access Control
- ✅ Role-based endpoint authorization
- ✅ JWT authentication required
- ✅ Admin-only sensitive operations
- ✅ User can only see own checkouts

### Validation & Integrity
- ✅ Input validation on all endpoints
- ✅ Business rule enforcement
- ✅ Database constraint compliance
- ✅ Email credential protection

---

## 🚀 Performance Considerations

### Background Service
- Runs once daily (minimal resource usage)
- Processes only overdue checkouts
- Asynchronous email sending
- Error handling prevents crashes

### Database Queries
- Indexed foreign keys
- Efficient filtering (status-based)
- Minimal joins required
- Pagination support in reports

### Email Service
- Connection pooling
- Timeout handling
- Retry logic for failures
- Batch processing for digest

---

## 📈 Monitoring & Maintenance

### What to Monitor
1. **Audit Trail Table**
   - Check for EMAIL_REMINDER entries
   - Verify ADMIN_DIGEST daily logs
   - Review failed operations

2. **Application Logs**
   - Background service start/stop
   - Email send confirmations
   - Validation failures

3. **Database Health**
   - Overdue checkout count
   - Active checkout trends
   - Asset status distribution

4. **Email Delivery**
   - Check spam folders initially
   - Monitor bounce rates
   - Verify SMTP connection

---

## 🎓 Best Practices Implemented

✅ **Separation of Concerns** - Services, Controllers, Models separated  
✅ **Dependency Injection** - Loose coupling, testable code  
✅ **Comprehensive Logging** - All actions audited  
✅ **Fail-Fast Validation** - Catch errors early  
✅ **Clear Error Messages** - User-friendly feedback  
✅ **Background Processing** - Non-blocking operations  
✅ **Soft Delete Pattern** - Data preservation  
✅ **Configuration Management** - External settings  

---

**Documentation Version:** 2.0  
**Last Updated:** June 15, 2026  
**Compliance Status:** 100% ✅
