import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './test/e2e',
  timeout: 90_000,
  expect: {
    timeout: 10_000,
  },
  fullyParallel: false,
  retries: 0,
  workers: 1,
  reporter: [
    ['list'],
    ['json', { outputFile: 'test-results/runbook-report.json' }],
  ],
  use: {
    baseURL: process.env.API_BASE_URL ?? 'https://localhost:7175',
    ignoreHTTPSErrors: true,
  },
  outputDir: 'test-results/playwright-output',
});
