# Internal Asset Tracker

An internal asset-tracking system: an ASP.NET Core Web API backend (`AssetTracker.API`) with an Angular frontend (`AssetTracker.UI`). It manages assets, checkouts/returns, user accounts, reporting, and automated overdue-return reminders.

## Tech stack

| Component | Technology |
|-----------|------------|
| Backend API | ASP.NET Core (`AssetTracker.API`, .NET 8) |
| Tests | xUnit (`AssetTracker.API.Tests`, .NET 10) |
| Frontend | Angular (`AssetTracker.UI`) |
| Data | Entity Framework Core + SQL Server |
| Email | SMTP (default) or SendGrid, selectable via `Email:Provider` |

## Prerequisites

- [.NET SDK](https://dotnet.microsoft.com/download) (8.0 and 10.0)
- SQL Server (e.g. SQL Server Express / LocalDB)
- EF Core tools (once per machine):
  ```powershell
  dotnet tool install --global dotnet-ef
  ```
- Node.js + npm (only for the Angular UI)

## Getting started (after cloning)

Secret values are **not** stored in the repo. `appsettings.json` ships with blank placeholders; you supply the real values via [.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets) in development (or environment variables in production). See `AssetTracker.API/appsettings.Example.json` for the full config shape.

### 1. Set the required secrets (per machine)

```powershell
# Required: signing key for JWT auth (use a long, random value, 32+ chars)
dotnet user-secrets set "Jwt:Secret" "<a-64-char-key>" --project AssetTracker.API

# Optional: only when you have a real value (needed to actually send email)
dotnet user-secrets set "Email:SmtpPassword" "<gmail-app-password>" --project AssetTracker.API
```

> The app fails fast at startup with a clear message if `Jwt:Secret` is not set.
> For Gmail, `Email:SmtpPassword` is a 16-character **App Password** — see `EMAIL_SETUP_GUIDE.md`.

### 2. Create / update the database

Confirm `ConnectionStrings:DefaultConnection` in `AssetTracker.API/appsettings.json` points at your SQL Server instance, then apply migrations:

```powershell
dotnet ef database update --project AssetTracker.API
```

### 3. Run the API

```powershell
dotnet run --project AssetTracker.API
```

- API: `http://localhost:5167` (and an HTTPS port from `launchSettings.json`)
- Swagger UI (Development): `/swagger`

### 4. Run the frontend (optional)

```powershell
cd AssetTracker.UI
npm install
npm start
```

The Angular dev server runs on `http://localhost:4200`.

## Configuration reference

Secret/sensitive keys (set via user secrets in dev, environment variables in prod):

| Key | Purpose |
|-----|---------|
| `Jwt:Secret` | **Required.** Signing key for JWT auth. |
| `Email:SmtpPassword` | SMTP password / app password (required to send email). |
| `Email:SendGridApiKey` | Only if `Email:Provider` is set to `SendGrid`. |

In production, use the environment-variable form with `__` as the separator, e.g. `Jwt__Secret`, `Email__SmtpPassword`.

The email provider is chosen by `Email:Provider` (`SMTP` by default, or `SendGrid`).

## Overdue return reminders

A background service (`OverdueReminderService`) runs daily at **08:00 SAST** (06:00 UTC) to email users with overdue assets and send an admin digest. Sends use bounded parallelism; tune the concurrency with `Email:MaxParallelSends` (default `4`; set to `1` for sequential sending). Admins can also trigger a run manually via the API.

## Running the tests

```powershell
dotnet test
```

See `AssetTracker.API.Tests/README_START_HERE.md` for details on the test suite.

## Additional documentation

- `START_HERE.md` — quick start
- `EMAIL_SETUP_GUIDE.md` — email/SMTP setup (Gmail, Outlook, SendGrid)
- `ARCHITECTURE_OVERVIEW.md` — architecture
- `API_QUICK_REFERENCE.md` — API endpoints
- `PROJECT_COMPLIANCE_REVIEW.md` / `COMPLIANCE_IMPLEMENTATION_SUMMARY.md` — compliance notes
