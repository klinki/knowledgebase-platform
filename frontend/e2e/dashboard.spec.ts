import { test, expect } from '@playwright/test';

async function mockAuthenticatedSession(page: import('@playwright/test').Page) {
  let authenticated = false;
  const user = {
    id: 'user-1',
    email: 'test@example.com',
    displayName: 'test',
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

  await page.route('**/api/v1/dashboard/overview', async route => {
    await route.fulfill({
      json: {
        recentCaptures: [
          {
            id: '1',
            title: 'DeepSeek-V3 Technical Report',
            sourceUrl: 'https://example.com/deepseek',
            capturedAt: '2026-03-01T12:00:00Z',
            status: 'Completed',
            tags: ['ai', 'research'],
            summary: null,
            similarity: null
          },
          {
            id: '2',
            title: 'Angular Signals Guide',
            sourceUrl: 'https://example.com/signals',
            capturedAt: '2026-02-28T12:00:00Z',
            status: 'Pending',
            tags: ['angular'],
            summary: null,
            similarity: null
          },
          {
            id: '3',
            title: 'Playwright Mocking',
            sourceUrl: 'https://example.com/mocking',
            capturedAt: '2026-02-27T12:00:00Z',
            status: 'Completed',
            tags: ['testing'],
            summary: null,
            similarity: null
          }
        ],
        topTags: [
          { id: 'tag-1', name: 'ai', count: 3, lastUsedAt: '2026-03-01T12:00:00Z' },
          { id: 'tag-2', name: 'angular', count: 2, lastUsedAt: '2026-02-28T12:00:00Z' }
        ],
        stats: {
          totalCaptures: 3,
          activeTags: 2
        }
      }
    });
  });

  await page.route('**/api/v1/tags', async route => {
    await route.fulfill({
      json: [
        { id: 'tag-1', name: 'ai', count: 3, lastUsedAt: '2026-03-01T12:00:00Z' },
        { id: 'tag-2', name: 'angular', count: 2, lastUsedAt: '2026-02-28T12:00:00Z' }
      ]
    });
  });
}

test.describe('Dashboard and Search', () => {
  test.beforeEach(async ({ page }) => {
    await mockAuthenticatedSession(page);

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
          sourceUrl: 'https://example.com/deepseek',
          summary: 'A focused summary of the DeepSeek technical report.',
          similarity: 0.98,
          tags: ['ai', 'research']
        }] });
      } else {
        await route.fulfill({ json: [
          {
            id: '1',
            title: 'DeepSeek-V3 Technical Report',
            sourceUrl: 'https://example.com/deepseek',
            summary: 'A focused summary of the DeepSeek technical report.',
            similarity: 0.98,
            tags: ['ai', 'research']
          },
          {
            id: '2',
            title: 'Angular Signals Guide',
            sourceUrl: 'https://example.com/signals',
            summary: 'Patterns for Angular signals.',
            similarity: 0.84,
            tags: ['angular']
          },
          {
            id: '3',
            title: 'Playwright Mocking',
            sourceUrl: 'https://example.com/mocking',
            summary: 'Testing with Playwright route mocks.',
            similarity: 0.81,
            tags: ['testing']
          }
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
    await expect(items).toHaveCount(3);
  });

  test('should filter items via search bar', async ({ page }) => {
    const searchInput = page.locator('.search-input');
    await searchInput.fill('DeepSeek');
    
    const items = page.locator('.knowledge-item');
    await expect(items).toHaveCount(1);
    await expect(items.first()).toContainText('DeepSeek-V3');
  });


  test('should keep search results on the dashboard route', async ({ page }) => {
    const searchInput = page.locator('.search-input');
    await searchInput.fill('DeepSeek');

    const searchResultCards = page.locator('.search-result-card');
    await expect(searchResultCards).toHaveCount(1);
    await expect(page.locator('a.knowledge-item')).toHaveCount(0);

    await searchResultCards.first().click();
    await expect(page).toHaveURL(/.*dashboard/);
  });

  test('should show empty state when no results found', async ({ page }) => {
    const searchInput = page.locator('.search-input');
    await searchInput.fill('NonExistentItemXYZ');
    
    await expect(page.locator('.empty-state')).toBeVisible();
    await expect(page.locator('.empty-state')).toContainText('No knowledge items found');
  });

  test('should show dashboard error state when overview loading fails', async ({ page }) => {
    await page.route('**/api/v1/dashboard/overview', async route => {
      await route.fulfill({ status: 500, body: '' });
    });

    await page.reload();

    await expect(page.locator('.search-error')).toContainText('Dashboard data could not be loaded.');
    await expect(page.locator('.list-section .empty-state')).toContainText('No captures have been saved yet.');
  });

  test('should navigate to tags page via sidebar', async ({ page }) => {
    await page.click('a[routerLink="/tags"]');
    await expect(page).toHaveURL(/.*tags/);
    await expect(page.locator('h1')).toContainText('Tags Vault');
  });

  test('should show empty and error states on the tags page', async ({ page }) => {
    await page.route('**/api/v1/tags', async route => {
      await route.fulfill({ json: [] });
    });

    await page.click('a[routerLink="/tags"]');
    await expect(page.locator('.empty-state')).toContainText('No tags have been created yet.');

    await page.route('**/api/v1/tags', async route => {
      await route.fulfill({ status: 500, body: '' });
    });

    await page.reload();
    await expect(page.locator('.empty-state')).toContainText('Tag summaries could not be loaded.');
  });
});
