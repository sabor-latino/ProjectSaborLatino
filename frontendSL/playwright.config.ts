import { defineConfig, devices } from '@playwright/test';

export default defineConfig({
  testDir: './e2e',
  timeout: 30000,
  fullyParallel: true,
  retries: 0,
  workers: 1,
  use: {
    baseURL: 'http://localhost:54526',
    ignoreHTTPSErrors: true,
    trace: 'on-first-retry',
  },
  projects: [
    {
      name: 'webkit-desktop',
      use: {
        ...devices['Desktop Safari'],
        browserName: 'webkit',
      },
    },
    {
      name: 'webkit-mobile-safari',
      use: {
        ...devices['iPhone 15'],
        browserName: 'webkit',
      },
    },
  ],
});
