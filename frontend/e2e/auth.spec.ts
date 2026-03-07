import { test, expect } from '@playwright/test';

async function mockAuth(page: import('@playwright/test').Page, displayName = 'test') {
  let authenticated = false;
  const user = {
    id: 'user-1',
    email: `${displayName}@example.com`,
    displayName,
    role: 'member',
  };

  await page.route('**/api/auth/me', async route => {
    if (authenticated) {
      await route.fulfill({ json: user });
      return;
    }

    await route.fulfill({ status: 401, body: '' });
  });

  await page.route('**/api/auth/login', async route => {
    authenticated = true;
    await route.fulfill({ json: user });
  });
}

test.describe('Authentication Flow', () => {
  test('should redirect to login when not authenticated', async ({ page }) => {
    await mockAuth(page);
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/.*login/);
  });

  test('should successfully log in and redirect to dashboard', async ({ page }) => {
    await mockAuth(page);
    await page.goto('/login');
    
    await page.fill('input[type="email"]', 'test@example.com');
    await page.fill('input[type="password"]', 'password123');
    
    await page.click('button[type="submit"]');
    
    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.locator('h1')).toContainText('Sentinel Vault');
  });

  test('should show correct user info in sidebar after login', async ({ page }) => {
    await mockAuth(page, 'david');
    await page.goto('/login');
    await page.fill('input[type="email"]', 'david@sentinel.ai');
    await page.fill('input[type="password"]', 'securepass');
    await page.click('button[type="submit"]');
    
    await expect(page.locator('.user-name')).toContainText('david');
  });
});
