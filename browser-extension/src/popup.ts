/**
 * Sentinel Popup Script
 * Handles interactions in the extension popup
 */

/**
 * Initialize the popup
 */
function initPopup(): void {
  const openOptionsBtn = document.getElementById('openOptions');
  const viewDashboardBtn = document.getElementById('viewDashboard');
  const docsLink = document.getElementById('docsLink');

  // Check if we're on X.com
  checkCurrentTab();

  // Open options page
  openOptionsBtn?.addEventListener('click', () => {
    chrome.runtime.openOptionsPage?.();
  });

  // View dashboard (will be implemented in Phase 4)
  viewDashboardBtn?.addEventListener('click', () => {
    chrome.storage.local.get('apiUrl').then((result) => {
      const dashboardUrl = result.apiUrl || 'http://localhost:3000';
      chrome.tabs.create({ url: dashboardUrl });
    });
  });

  // Documentation link
  docsLink?.addEventListener('click', (e) => {
    e.preventDefault();
    chrome.tabs.create({ url: 'https://github.com/yourusername/sentinel-knowledgebase' });
  });
}

/**
 * Check if the current tab is X.com
 */
async function checkCurrentTab(): Promise<void> {
  try {
    const [tab] = await chrome.tabs.query({ active: true, currentWindow: true });

    const statusDot = document.getElementById('statusDot');
    const statusText = document.getElementById('statusText');

    if (tab.url?.includes('x.com') || tab.url?.includes('twitter.com')) {
      statusDot?.classList.add('active');
      statusDot?.classList.remove('inactive');
      if (statusText) {
        statusText.textContent = 'Active on X.com';
      }
    } else {
      statusDot?.classList.remove('active');
      statusDot?.classList.add('inactive');
      if (statusText) {
        statusText.textContent = 'Navigate to X.com to use';
      }
    }
  } catch (error) {
    console.error('[Sentinel] Failed to check current tab:', error);
  }
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', initPopup);
