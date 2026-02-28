import { test, expect } from '@playwright/test';

test.describe('Dashboard and Search', () => {
  test.beforeEach(async ({ page }) => {
    // Mock the Semantic Search API
    await page.route('**/api/v1/search/semantic', async route => {
      const postData = route.request().postDataJSON();
      const query = postData?.query || '';

      if (query.includes('NonExistent')) {
        await route.fulfill({ json: [] });
      } else if (query.includes('DeepSeek')) {
        await route.fulfill({ json: [{ 
          id: '1', 
          title: 'DeepSeek-V3 Technical Report', 
          sourceUrl: 'https://example.com/deepseek' 
        }] });
      } else {
        await route.fulfill({ json: [
          { id: '1', title: 'DeepSeek-V3 Technical Report', sourceUrl: 'https://example.com/deepseek' },
          { id: '2', title: 'Angular Signals Guide', sourceUrl: 'https://example.com/signals' },
          { id: '3', title: 'Playwright Mocking', sourceUrl: 'https://example.com/mocking' }
        ] });
      }
    });

    // Perform login before each dashboard test
    await page.goto('/login');
    await page.fill('input[type="email"]', 'test@example.com');
    await page.fill('input[type="password"]', 'password');
    await page.click('button[type="submit"]');
    await expect(page).toHaveURL(/.*dashboard/);
  });

  test('should display recent knowledge items', async ({ page }) => {
    const items = page.locator('.knowledge-item');
    // Note: Initial state is hardcoded mock data (5 items), 
    // but a search triggers the API mock (3 items).
    // Let's trigger a blank search to verify mocking works.
    await page.fill('.search-input', ' ');
    await expect(items).toHaveCount(3); 
  });

  test('should filter items via search bar', async ({ page }) => {
    const searchInput = page.locator('.search-input');
    await searchInput.fill('DeepSeek');
    
    const items = page.locator('.knowledge-item');
    await expect(items).toHaveCount(1);
    await expect(items.first()).toContainText('DeepSeek-V3');
  });

  test('should show empty state when no results found', async ({ page }) => {
    const searchInput = page.locator('.search-input');
    await searchInput.fill('NonExistentItemXYZ');
    
    await expect(page.locator('.empty-state')).toBeVisible();
    await expect(page.locator('.empty-state')).toContainText('No knowledge items found');
  });

  test('should navigate to tags page via sidebar', async ({ page }) => {
    await page.click('a[routerLink="/tags"]');
    await expect(page).toHaveURL(/.*tags/);
    await expect(page.locator('h1')).toContainText('Tags Vault');
  });
});
