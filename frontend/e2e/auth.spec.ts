import { test, expect } from '@playwright/test';

test.describe('Authentication Flow', () => {
  test('should redirect to login when not authenticated', async ({ page }) => {
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/.*login/);
  });

  test('should successfully log in and redirect to dashboard', async ({ page }) => {
    await page.goto('/login');
    
    await page.fill('input[type="email"]', 'test@example.com');
    await page.fill('input[type="password"]', 'password123');
    
    await page.click('button[type="submit"]');
    
    // Wait for artificial premium delay + navigation
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.locator('h1')).toContainText('Sentinel Vault');
  });

  test('should show correct user info in sidebar after login', async ({ page }) => {
    await page.goto('/login');
    await page.fill('input[type="email"]', 'david@sentinel.ai');
    await page.fill('input[type="password"]', 'securepass');
    await page.click('button[type="submit"]');
    
    await expect(page.locator('.user-name')).toContainText('david');
  });
});
