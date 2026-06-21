# Asset Tracker API - Quick Reference (Updated for Compliance)

## 🔐 Authentication
All endpoints require JWT Bearer token except where noted.

```http
Authorization: Bearer <your-jwt-token>
```

---

## 📦 Assets Endpoints

### Get All Assets
```http
GET /api/assets
Authorization: Required (All Roles)
```

### Get Available Assets Only (NEW)
```http
GET /api/checkouts/available-assets
Authorization: Required (All Roles)
```
**Use this for checkout screens** - filters out unavailable assets.

### Get Specific Asset
```http
GET /api/assets/{id}
Authorization: Required (All Roles)
```

### Create Asset
```http
POST /api/assets
Authorization: Admin, Asset Manager
Content-Type: application/json

{
  "name": "Dell Laptop XPS 15",
  "category": "Laptops",
  "serialNumber": "DL5520XPS123",
  "status": "Available",
  "purchaseDate": "2026-06-01T00:00:00Z",
  "value": 1499.99
}
```
**Validation:** Rejects duplicate serial numbers (including decommissioned assets).

### Update Asset
```http
PUT /api/assets/{id}
Authorization: Admin, Asset Manager
Content-Type: application/json

{
  "assetID": 5,
  "name": "Dell Laptop XPS 15 (Updated)",
  "category": "Laptops",
  "serialNumber": "DL5520XPS123",
  "status": "Available",
  "purchaseDate": "2026-06-01T00:00:00Z",
  "value": 1499.99
}
```

### Decommission Asset (Soft Delete)
```http
DELETE /api/assets/{id}
Authorization: Admin, Asset Manager
```
**Changed:** Now performs soft delete (status = "Decommissioned") instead of database deletion.

### Restore Decommissioned Asset
```http
POST /api/assets/{id}/restore
Authorization: Admin, Asset Manager
```

### Set Maintenance Status
```http
PUT /api/assets/{id}/maintenance
Authorization: Admin, Asset Manager
Content-Type: application/json

{
  "setMaintenance": true
}
```

### Admin Override Return
```http
POST /api/assets/{id}/admin-return
Authorization: Admin, Asset Manager
```

### Dashboard Metrics
```http
GET /api/assets/dashboard-metrics
Authorization: Admin, Asset Manager
```

---

## 🔄 Checkouts Endpoints

### Checkout Asset (UPDATED)
```http
POST /api/checkouts/assign
Authorization: Required (All Roles)
Content-Type: application/json

{
  "assetID": 5,
  "employeeEmail": "user@company.com",
  "expectedReturnDate": "2026-07-15T00:00:00Z"
}
```

**Changed:** Now requires explicit `expectedReturnDate` (not duration in days).

**Validations:**
- ✅ Asset must be selected (assetID > 0)
- ✅ Asset must exist
- ✅ Asset status must be "Available"
- ✅ Asset cannot be decommissioned
- ✅ Asset cannot be under maintenance
- ✅ Employee email required
- ✅ User must exist
- ✅ Expected return date required
- ✅ Expected return date cannot be in the past
- ✅ User cannot checkout same asset twice

**Success Response:**
```json
{
  "success": true,
  "message": "Checkout successful! Asset 'Dell Laptop XPS 15' has been assigned to John Doe.",
  "checkoutId": 42,
  "assetName": "Dell Laptop XPS 15",
  "expectedReturnDate": "2026-07-15T00:00:00Z"
}
```

**Error Response Examples:**
```json
{
  "message": "Expected return date cannot be in the past."
}

{
  "message": "This user already has this asset checked out."
}

{
  "message": "Asset has been decommissioned and cannot be checked out."
}
```

### Return Asset
```http
POST /api/checkouts/return/{assetId}
Authorization: Required (All Roles)
```

### Cancel Checkout (NEW)
```http
POST /api/checkouts/cancel/{checkoutId}
Authorization: Admin, Asset Manager
```

Cancels an active checkout and returns asset to available status.

**Response:**
```json
{
  "success": true,
  "message": "Checkout has been successfully cancelled and the asset has been returned to available status."
}
```

### Get My Assets
```http
GET /api/checkouts/my-assets
Authorization: Required (All Roles - returns only user's own checkouts)
```

Returns all assets currently checked out to the authenticated user.

### Get Overdue Checkouts (NEW)
```http
GET /api/checkouts/overdue
Authorization: Admin, Asset Manager
```

Returns all checkouts past their expected return date.

**Response:**
```json
[
  {
    "checkoutID": 42,
    "assetID": 5,
    "assetName": "Dell Laptop XPS 15",
    "serialNumber": "DL5520XPS123",
    "category": "Laptops",
    "userFullName": "John Doe",
    "userEmail": "john.doe@company.com",
    "checkoutDate": "2026-06-01T10:30:00Z",
    "expectedReturnDate": "2026-06-10T00:00:00Z",
    "daysOverdue": 5
  }
]
```

### Get Audit Trail
```http
GET /api/checkouts/audit-trail
Authorization: Admin
```

Returns complete audit trail including email notifications.

**Actions Logged:**
- PROVISION (asset created)
- ASSIGN (checkout)
- RETURN (asset returned)
- CANCEL_CHECKOUT (checkout cancelled)
- DECOMMISSION (asset decommissioned)
- RESTORE (asset restored)
- ADMIN_OVERRIDE_RETURN (admin forced return)
- SET_MAINTENANCE / CLEAR_MAINTENANCE
- **EMAIL_REMINDER** (overdue email sent to user)
- **ADMIN_DIGEST** (daily digest sent to admin)

---

## 📊 Reports Endpoints

### Get Checkout History
```http
GET /api/reports/checkout-history?fromDate=2026-06-01&toDate=2026-06-30&category=Laptops&assetId=5&userId=3
Authorization: Admin, Asset Manager
```

**Query Parameters:**
- `fromDate` (optional): Start date (YYYY-MM-DD)
- `toDate` (optional): End date (YYYY-MM-DD)
- `category` (optional): Asset category or "All"
- `assetId` (optional): Specific asset ID
- `userId` (optional): Specific user ID

**Single Day Report:**
```http
GET /api/reports/checkout-history?fromDate=2026-06-15&toDate=2026-06-15
```

### Download Excel Report
```http
GET /api/reports/checkout-history/excel?fromDate=2026-06-01&toDate=2026-06-30
Authorization: Admin, Asset Manager
```

Returns `.xlsx` file with all checkout data.

### Download PDF Report
```http
GET /api/reports/checkout-history/pdf?fromDate=2026-06-01&toDate=2026-06-30
Authorization: Admin, Asset Manager
```

Returns `.pdf` file with formatted report.

---

## 👥 Users Endpoints

### Register User
```http
POST /api/auth/register
Authorization: None
Content-Type: application/json

{
  "fullName": "John Doe",
  "email": "john.doe@company.com",
  "password": "SecurePass123!",
  "role": "Employee",
  "phoneNumber": "555-1234"
}
```

### Login
```http
POST /api/auth/login
Authorization: None
Content-Type: application/json

{
  "email": "john.doe@company.com",
  "password": "SecurePass123!"
}
```

**Response:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "email": "john.doe@company.com",
  "role": "Employee"
}
```

---

## 🔑 Roles and Permissions

| Endpoint | Employee | Asset Manager | Admin |
|----------|----------|---------------|-------|
| View Assets | ✅ | ✅ | ✅ |
| View Available Assets | ✅ | ✅ | ✅ |
| Checkout Asset | ✅ | ✅ | ✅ |
| Return Own Asset | ✅ | ✅ | ✅ |
| View My Assets | ✅ | ✅ | ✅ |
| Create Asset | ❌ | ✅ | ✅ |
| Update Asset | ❌ | ✅ | ✅ |
| Decommission Asset | ❌ | ✅ | ✅ |
| Cancel Checkout | ❌ | ✅ | ✅ |
| Admin Override Return | ❌ | ✅ | ✅ |
| View Overdue Checkouts | ❌ | ✅ | ✅ |
| View Reports | ❌ | ✅ | ✅ |
| View Audit Trail | ❌ | ❌ | ✅ |

---

## 🚨 Error Response Format

All errors return consistent format:

```json
{
  "message": "Descriptive error message explaining what went wrong"
}
```

**HTTP Status Codes:**
- `200 OK` - Success
- `400 Bad Request` - Validation error
- `401 Unauthorized` - Missing or invalid token
- `403 Forbidden` - Insufficient permissions
- `404 Not Found` - Resource not found
- `500 Internal Server Error` - Server error

---

## 📧 Email Notifications (Automated)

### Overdue Reminder Emails
**Trigger:** Automated daily at 8 AM UTC  
**Recipients:** Users with overdue checkouts  
**Frequency:** Daily until asset returned  

**Email Contains:**
- Asset name
- Serial number
- Checkout date
- Expected return date
- Days overdue

**Logged in Audit Trail:** Action = "EMAIL_REMINDER"

### Admin Daily Digest
**Trigger:** Automated daily at 8 AM UTC  
**Recipients:** Admin email (configured in appsettings.json)  
**Frequency:** Daily if any overdue checkouts exist  

**Email Contains:**
- Table of all overdue checkouts
- User names and emails
- Asset details
- Days overdue for each

**Logged in Audit Trail:** Action = "ADMIN_DIGEST"

---

## 🔧 Configuration

### Email Settings (appsettings.json)
```json
"Email": {
  "SmtpHost": "smtp.gmail.com",
  "SmtpPort": "587",
  "SmtpUser": "your-email@gmail.com",
  "SmtpPassword": "your-app-password",
  "FromEmail": "your-email@gmail.com",
  "FromName": "Asset Tracker System",
  "AdminEmail": "admin@company.com"
}
```

### Database Connection
```json
"ConnectionStrings": {
  "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=AssetTrackerDB;Trusted_Connection=True;TrustServerCertificate=True;"
}
```

### JWT Settings
```json
"Jwt": {
  "Secret": "your-secret-key-min-32-characters",
  "Issuer": "AssetTrackerAPI",
  "Audience": "AssetTrackerUI"
}
```

---

## 📱 Frontend Integration Notes

### Date Handling
- Always send dates in ISO 8601 format: `"2026-07-15T00:00:00Z"`
- Expected return date picker should disable past dates
- Display dates in user's local timezone

### Toast Notifications
Display toast messages based on response:
- Success: `response.message` or `response.success = true`
- Error: `response.message` from error response

### Available Assets
**Use:** `GET /api/checkouts/available-assets`  
**Not:** `GET /api/assets` (includes unavailable)

### Overdue Indicators
Check `isOverdue` flag in "My Assets" response to highlight overdue items.

### Admin Features
Conditionally show based on role:
- Overdue dashboard: Admin, Asset Manager
- Cancel checkout button: Admin, Asset Manager
- Reports: Admin, Asset Manager
- Audit trail: Admin only

---

## 🧪 Testing Endpoints

### Using Swagger
1. Start API
2. Navigate to `https://localhost:XXXX/swagger`
3. Click "Authorize" button
4. Enter: `Bearer <your-token>`
5. Test endpoints

### Using Postman
1. Create collection
2. Set Authorization: Bearer Token
3. Add token to collection variables
4. Test all endpoints

### Using cURL
```bash
curl -X POST "https://localhost:7XXX/api/checkouts/assign" \
  -H "Authorization: Bearer YOUR_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "assetID": 5,
    "employeeEmail": "user@test.com",
    "expectedReturnDate": "2026-07-15T00:00:00Z"
  }'
```

---

## 🎯 Quick Compliance Reference

| Requirement | Endpoint/Feature | Status |
|-------------|------------------|--------|
| Single asset checkout | POST /api/checkouts/assign | ✅ |
| Success message | Response.message | ✅ |
| Cancellation message | POST /api/checkouts/cancel | ✅ |
| Status updates | Immediate on checkout/return | ✅ |
| Capture user info | Checkout model | ✅ |
| Expected return date | Required in DTO | ✅ |
| Past date validation | Validation in controller | ✅ |
| All validations (2.2) | AssignAsset method | ✅ |
| Available assets filter | GET /api/checkouts/available-assets | ✅ |
| Duplicate serial check | POST /api/assets | ✅ |
| Soft delete | DELETE /api/assets | ✅ |
| Audit trail | Logged on all actions | ✅ |
| Email reminders | OverdueReminderService | ✅ |
| Admin digest | Daily automated | ✅ |
| Email logging | Audit trail | ✅ |
| Reports | /api/reports/* | ✅ |

---

## 📚 Additional Resources

- **Full Documentation:** `COMPLIANCE_IMPLEMENTATION_SUMMARY.md`
- **Email Setup:** `EMAIL_SETUP_GUIDE.md`
- **Testing Checklist:** `TESTING_CHECKLIST.md`
- **Database Migration:** `Database_Migration_Compliance_Check.sql`

---

## 🆘 Common Issues

### Issue: "Expected return date cannot be in the past"
**Solution:** Ensure date picker sends future date in correct format.

### Issue: "This user already has this asset checked out"
**Solution:** Check active checkouts, return asset first.

### Issue: Emails not sending
**Solution:** Verify SMTP credentials in appsettings.json. See `EMAIL_SETUP_GUIDE.md`.

### Issue: Asset not appearing in available list
**Solution:** Check asset status is "Available", not "Checked Out", "Under Maintenance", or "Decommissioned".

---

**Last Updated:** June 2026  
**API Version:** 2.0 (Compliance Update)  
**Compatible With:** .NET 8
