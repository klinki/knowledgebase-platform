import { test, expect } from '@playwright/test';

async function mockAuth(
  page: import('@playwright/test').Page,
  options?: { displayName?: string; initiallyAuthenticated?: boolean }
) {
  let authenticated = options?.initiallyAuthenticated ?? false;
  const displayName = options?.displayName ?? 'test';
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

  await page.route('**/api/auth/logout', async route => {
    authenticated = false;
    await route.fulfill({ status: 204, body: '' });
  });
}

test.describe('Authentication Flow', () => {
  test('should redirect to login when not authenticated', async ({ page }) => {
    await mockAuth(page);
    await page.goto('/dashboard');
    await expect(page).toHaveURL(/.*login\?returnUrl=%2Fdashboard/);
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
    await mockAuth(page, { displayName: 'david' });
    await page.goto('/login');
    await page.fill('input[type="email"]', 'david@sentinel.ai');
    await page.fill('input[type="password"]', 'securepass');
    await page.click('button[type="submit"]');
    
    await expect(page.locator('.user-name')).toContainText('david');
  });

  test('should restore an existing session on a protected route', async ({ page }) => {
    await mockAuth(page, { displayName: 'restored', initiallyAuthenticated: true });

    await page.goto('/dashboard');

    await expect(page).toHaveURL(/.*dashboard/);
    await expect(page.locator('.user-name')).toContainText('restored');
  });

  test('should return to the originally requested route after login', async ({ page }) => {
    await mockAuth(page);

    await page.goto('/tags');
    await expect(page).toHaveURL(/.*login\?returnUrl=%2Ftags/);

    await page.fill('input[type="email"]', 'test@example.com');
    await page.fill('input[type="password"]', 'password123');
    await page.click('button[type="submit"]');

    await expect(page).toHaveURL(/.*tags/);
    await expect(page.locator('h1')).toContainText('Tags Vault');
  });

  test('should redirect to login when a protected API request returns 401', async ({ page }) => {
    await mockAuth(page, { initiallyAuthenticated: true });

    await page.route('**/api/v1/search/semantic', async route => {
      await route.fulfill({ status: 401, body: '' });
    });

    await page.goto('/dashboard');
    await page.fill('.search-input', 'trigger unauthorized');

    await expect(page).toHaveURL(/.*login\?returnUrl=%2Fdashboard/);
  });
});
