# 📋 Internal Asset Tracker

An enterprise-grade, full-stack internal asset-tracking system featuring a robust ASP.NET Core Web API backend and a responsive Angular frontend. Designed to manage equipment lifecycles, checkout transactions, audit trails, and automated email reminders for overdue returns.

---

## 🚀 Introduction

The **Internal Asset Tracker** is built to solve the challenges of tracking internal company hardware and equipment (laptops, keyboards, monitors, etc.) across employees.

### Key Objectives
* **Lifecycle Monitoring**: Track assets from acquisition through maintenance to decommissioning.
* **Accountability**: Real-time assignment/checkout of available assets to active employees, preventing duplicate checkouts or unauthorized assignments.
* **Automation**: Proactive email notifications and alerts notifying users of overdue assets and sending daily status digests to administrators.
* **Compliance & Auditing**: Maintain a permanent, tamper-proof record of all checkouts, returns, maintenance logs, and administrative actions.

---

## 🛠️ Getting Started

Follow these steps to configure, build, and run the project locally.

### 📋 Prerequisites
Make sure you have the following installed on your machine:
* **SDKs**: [.NET SDK 8.0 & 10.0](https://dotnet.microsoft.com/download)
* **Database**: SQL Server (LocalDB / Express / Standard)
* **Package Manager**: [Node.js + npm](https://nodejs.org/) (for Angular frontend)
* **EF Core CLI** (run once globally):
  ```powershell
  dotnet tool install --global dotnet-ef
  ```

---

### 📦 Installation & Setup

#### 1. Clone & Configuration
Clone the repository and set up configuration secrets. The application uses .NET User Secrets in development to keep credentials secure.

```powershell
# Set JWT signing key (Required: 64-char key recommended)
dotnet user-secrets set "Jwt:Secret" "YOUR_SUPER_SECRET_64_CHARACTER_KEY_GOES_HERE" --project AssetTracker.API

# Set SMTP password (Optional: required for sending emails)
dotnet user-secrets set "Email:SmtpPassword" "your-smtp-app-password" --project AssetTracker.API
```
> See `AssetTracker.API/appsettings.Example.json` for the full configuration schema. For detailed email configuration, refer to `EMAIL_SETUP_GUIDE.md`.

#### 2. Database Migration
Update the database schema by applying EF Core migrations:
```powershell
dotnet ef database update --project AssetTracker.API
```

#### 3. Run the Backend API
Start the Web API server:
```powershell
dotnet run --project AssetTracker.API --launch-profile https
```
* **API Endpoint**: `https://localhost:7175` / `http://localhost:5167`
* **API Documentation**: Access the interactive Swagger UI at `https://localhost:7175/swagger`

#### 4. Run the Frontend UI
Navigate to the UI project, install dependencies, and start the development server:
```powershell
cd AssetTracker.UI
npm install
npm start
```
* **UI Endpoint**: `http://localhost:4200` (auto-opens in your browser)

---

## ⚙️ Software Dependencies & Tech Stack

| Tier | Technology | Purpose |
|------|------------|---------|
| **Backend API** | ASP.NET Core (.NET 8) | RESTful API, JWT authentication, role authorization |
| **Database ORM**| Entity Framework Core | Database queries, migrations, and relationship management |
| **Frontend** | Angular | Responsive single-page application (SPA), Tailwind CSS styling |
| **Email Service**| SMTP / SendGrid | Automated transactional email notifications |
| **Task Scheduler**| .NET Background Service | Daily overdue sweep at 08:00 SAST |

---

## 🧪 Build and Test

The project has comprehensive unit and integration test coverage for both backend and frontend.

### 🖥️ Backend Tests (xUnit)
Run the C# test suite:
```powershell
dotnet test
```

### 🎨 Frontend Tests (Vitest / Angular CLI)
Run the Angular UI test suite:
```powershell
cd AssetTracker.UI
npm test -- --watch=false
```

---

## 🤝 Contribute

We welcome contributions! Please adhere to the following workflow:

1. **Create a branch**: Use descriptive names (`feature/`, `bugfix/`).
2. **Code Quality**: Keep component responsibilities separate, write unit tests for new logic, and maintain compliance standards.
3. **Database Changes**: If modifying database models, generate a new migration:
   ```powershell
   dotnet ef migrations add NameOfYourMigration --project AssetTracker.API
   ```
4. **Pull Requests**: Ensure all unit tests pass before submitting a pull request.

---

## 📚 Additional Documentation
For in-depth guides, check the following markdown files:
* 📖 [START_HERE.md](file:///d:/Repositories/Abdud-DaiyyaanBoltman/START_HERE.md) — Quick onboarding guide.
* ✉️ [EMAIL_SETUP_GUIDE.md](file:///d:/Repositories/Abdud-DaiyyaanBoltman/EMAIL_SETUP_GUIDE.md) — SMTP & SendGrid setup.
* 🏗️ [ARCHITECTURE_OVERVIEW.md](file:///d:/Repositories/Abdud-DaiyyaanBoltman/ARCHITECTURE_OVERVIEW.md) — Architecture and design.
* 📡 [API_QUICK_REFERENCE.md](file:///d:/Repositories/Abdud-DaiyyaanBoltman/API_QUICK_REFERENCE.md) — Endpoint reference.
* 📝 [TESTING_CHECKLIST.md](file:///d:/Repositories/Abdud-DaiyyaanBoltman/TESTING_CHECKLIST.md) — QA verification steps.
