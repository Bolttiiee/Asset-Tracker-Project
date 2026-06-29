# Internal Asset Tracker

A full-stack company asset-tracking system featuring a robust ASP.NET Core Web API backend and a responsive Angular frontend. Designed to manage equipment lifecycles, checkout transactions, audit trails, and automated email reminders for overdue returns.

---

## Getting Started

Follow these steps to configure, build, and run the project locally.

### Prerequisites
Make sure you have the following installed on your machine:
* **SDKs**: [.NET SDK 8.0 & 10.0](https://dotnet.microsoft.com/download)
* **Database**: SQL Server Express/LocalDB/Standard
* **Package Manager**: [Node.js + npm](https://nodejs.org/) (for Angular frontend)
* **EF Core CLI**:
  ```powershell
  dotnet tool install --global dotnet-ef
  ```

---

## Installation & Setup

The easiest way to get started is by running the interactive setup script, which will prompt you for configuration details (like your SMTP password), apply database migrations, and install packages:

```powershell
./setup.ps1
```

---

### Manual Setup (Alternative)

If you prefer to perform the setup steps manually, run the following commands:

#### 1. Configure Secrets
The application uses .NET User Secrets in development:
```powershell
# Set JWT signing key (Required: 64-char key recommended)
dotnet user-secrets set "Jwt:Secret" "YOUR_SUPER_SECRET_64_CHARACTER_KEY_GOES_HERE" --project AssetTracker.API

# Set SMTP password (Optional: required for sending overdue notification emails)
dotnet user-secrets set "Email:SmtpPassword" "your-smtp-app-password" --project AssetTracker.API
```
> See `AssetTracker.API/appsettings.json` for SMTP host, port, and authentication configuration.

#### 2. Database Migration & Setup
Create the SQL Server database and apply EF Core migrations:
```powershell
dotnet ef database update --project AssetTracker.API
```

#### 3. Run the Backend API
Start the Web API server:
```powershell
dotnet run --project AssetTracker.API --launch-profile https
```
* **API HTTPS Endpoint**: `https://localhost:7175`
* **API HTTP Endpoint**: `http://localhost:5167`
* **API Documentation**: Access Swagger UI at `https://localhost:7175/swagger`

#### 4. Run the Frontend UI
Navigate to the UI project, install dependencies, and start the development server:
```powershell
cd AssetTracker.UI
npm install
npm start
```
* **UI Endpoint**: `http://localhost:4200` (automatically opens in your browser)

---

## Database Seeding

When database migrations are run, the database is automatically seeded with default reference and demo data:

### Seeded Demo Accounts
Use these pre-configured user credentials to log in:

| Full Name | Email Address | Password | Role | Account Status |
|-----------|---------------|----------|------|----------------|
| **Ada Boltman** | `adaboltman@gmail.com` | `Boltm@n123` | `Admin` | Approved |
| **Asset Manager** | `manager@company.com` | `Secret123!` | `Asset Manager` | Approved |
| **Standard User** | `user@company.com` | `Secret123!` | `Standard User` | Approved |
| **Pending Employee**| `pending@company.com` | `Secret123!` | `Standard User` | Pending |

### Other Seeded Reference Data
* **Assets:** preloaded laptops, monitors, keyboards, and accessories in various states (e.g. `Available`, `Checked Out`, `Decommissioned`).
* **Departments:** Admin, Engineering, HR, Sales, Operations, Finance, Legal, Marketing.

---

## Assumptions & Design Decisions

### 1. Authentication & Security
* **JWT Bearer Auth:** The application uses JSON Web Tokens (JWT) for stateless authentication. On successful login, the API issues a signed token which the Angular frontend stores in local storage and includes in the HTTP `Authorization` header.
* **Role-Based Access Control (RBAC):** API endpoints and UI routing are secured using three roles: `Admin` (full system override), `Asset Manager` (manages inventory), and `Standard User` (views checked out assets).

### 2. Email Service & Overdue Sweeps
* **SMTP Implementation:** Switched entirely to direct SMTP configuration for email delivery (specifically tailored for Gmail App Passwords or local mail servers).
* **Automated Overdue Checkouts Sweep:** A C# background worker running inside the API executes at 08:00 SAST daily. It queries for overdue assets, sends email reminders to employees, and compiles a daily status digest for administrators.

### 3. Unified Test Consolidation & Sync Hook
* **Unified Tests Folder:** All backend, Vitest, and Playwright tests are physically consolidated under the root-level `tests/` directory to keep the workspace clean.
* **Synchronization Hook (`sync-tests.js`):** Because Angular CLI and TypeScript builders restrict compiler scopes, we use a pre-test NPM script that copies tests from `tests/frontend` into the Angular folder right before runners (Vitest/Playwright) execute. Synced copies are automatically ignored by Git.

---

## Build and Test

All backend and frontend test suites are consolidated and can be executed via terminal:

### 1. Backend C# Tests (xUnit)
Run the backend unit and integration test suite:
```powershell
dotnet test tests/backend/AssetTracker.API.Tests.csproj
```

### 2. Frontend Unit Tests (Vitest)
Run Angular unit tests:
```powershell
cd AssetTracker.UI
npm test -- --watch=false
```

### 3. Frontend E2E Tests (Playwright)
Run Playwright integration test suite:
```powershell
cd AssetTracker.UI
npm run e2e:runbook
```
