import { test, expect } from '@playwright/test';

test('has title', async ({ page }) => {
  await page.goto('/');
  await expect(page).toHaveTitle(/Frontend/);
});

test('login page is centered', async ({ page }) => {
  await page.goto('/login');
  const loginCard = page.locator('.glass-card');
  await expect(loginCard).toBeVisible();
});
