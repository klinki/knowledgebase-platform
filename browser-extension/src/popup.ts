/**
 * Sentinel Popup Script
 * Handles interactions in the extension popup
 */
import { DEFAULT_API_URL, DEFAULT_APP_URL, DOCUMENTATION_URL } from './constants.js';

/**
 * Initialize the popup
 */
function initPopup(): void {
  const openOptionsBtn = document.getElementById('openOptions');
  const viewDashboardBtn = document.getElementById('viewDashboard');
  const docsLink = document.getElementById('docsLink');

  // Check current tab and auth state for capability-aware status
  checkCurrentContext();

  // Open options page
  openOptionsBtn?.addEventListener('click', () => {
    chrome.runtime.openOptionsPage?.();
  });

  // View dashboard
  viewDashboardBtn?.addEventListener('click', () => {
    chrome.storage.local.get(['appUrl', 'apiUrl']).then((result) => {
      const dashboardUrl = (result.appUrl as string | undefined)?.trim()
        || mapApiUrlToAppUrl(result.apiUrl as string | undefined)
        || DEFAULT_APP_URL;
      chrome.tabs.create({ url: dashboardUrl });
    });
  });

  // Documentation link
  docsLink?.addEventListener('click', (e) => {
    e.preventDefault();
    chrome.tabs.create({ url: DOCUMENTATION_URL });
  });
}

/**
 * Check current tab and auth state to provide capability-aware status
 */
async function checkCurrentContext(): Promise<void> {
  try {
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });
    const auth = await chrome.storage.local.get(['accessToken', 'accessTokenExpiresAt', 'apiKey']);

    const statusDot = document.getElementById('statusDot');
    const statusText = document.getElementById('statusText');

    if (!statusText || !statusDot) {
      return;
    }

    const isOnX = tab.url?.includes('x.com') || tab.url?.includes('twitter.com');
    const hasAccessToken = typeof auth.accessToken === 'string' && auth.accessToken.length > 0;
    const hasLegacyToken = typeof auth.apiKey === 'string' && auth.apiKey.length > 0;
    const tokenExpiresAt = typeof auth.accessTokenExpiresAt === 'string'
      ? Date.parse(auth.accessTokenExpiresAt)
      : Number.NaN;
    const isSignedIn = hasAccessToken && (Number.isNaN(tokenExpiresAt) || tokenExpiresAt > Date.now());

    if (!isSignedIn && !hasLegacyToken) {
      statusDot.classList.remove('active');
      statusDot.classList.add('inactive');
      statusText.textContent = 'Sign in to capture from X, bookmarks, and text selection';
      return;
    }

    statusDot.classList.add('active');
    statusDot.classList.remove('inactive');
    statusText.textContent = isOnX
      ? 'Ready on X + bookmarks + text selection'
      : 'Ready for bookmarks + text selection (X capture on x.com)';
  } catch (error) {
    console.error('[Sentinel] Failed to check current tab context:', error);
  }
}

function mapApiUrlToAppUrl(apiUrl: string | undefined): string {
  const normalizedApiUrl = apiUrl?.trim() || DEFAULT_API_URL;

  try {
    const parsedUrl = new URL(normalizedApiUrl);
    if (parsedUrl.port === '5000') {
      parsedUrl.port = '4200';
    }

    return parsedUrl.origin;
  } catch {
    return DEFAULT_APP_URL;
  }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', initPopup);
