import { defineConfig } from '@playwright/test'
import path from 'path'

export default defineConfig({
  testDir: './tests/e2e',
  fullyParallel: false,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: 1,
  reporter: 'html',
  timeout: 30000,
  use: {
    trace: 'on-first-retry',
    screenshot: 'only-on-failure',
    video: 'on-first-retry',
  },
  projects: [
    {
      name: 'chromium',
      use: {
        viewport: { width: 1280, height: 720 },
      },
    },
  ],
  webServer: {
    command: 'npx http-server tests/e2e/fixtures -p 31337 -c-1 --silent',
    url: 'http://localhost:31337',
    reuseExistingServer: !process.env.CI,
    timeout: 10000,
  },
})
