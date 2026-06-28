# Runbook E2E Suite

This folder contains an API-first Playwright suite that executes the assessment runbook scenarios with deterministic setup/teardown.

## Files

- `runbook.spec.ts`: end-to-end API workflow assertions for scenarios 1-10.
- `../../playwright.config.ts`: Playwright config with JSON reporter output.

## Prerequisites

1. Backend API is running and reachable.
2. Set environment variables:
   - `RUNBOOK_ADMIN_EMAIL`
   - `RUNBOOK_ADMIN_PASSWORD`
3. Optionally set:
   - `API_BASE_URL` (defaults to `https://localhost:7175`)
   - `RUNBOOK_TEST_PASSWORD` (defaults to `Pass!2026A`)

## Run

```powershell
npm run e2e:runbook
```

## Machine-readable report

The suite emits a JSON report at:

`test-results/runbook-report.json`
