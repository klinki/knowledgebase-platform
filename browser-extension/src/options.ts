/**
 * Sentinel Options Page
 * Allows users to configure their API key and settings
 */

import { DEFAULT_API_URL } from './constants.js';

// DOM Elements
const apiKeyInput = document.getElementById('apiKey') as HTMLInputElement;
const apiUrlInput = document.getElementById('apiUrl') as HTMLInputElement;
const saveButton = document.getElementById('saveBtn') as HTMLButtonElement;
const statusMessage = document.getElementById('status') as HTMLDivElement;
const testConnectionBtn = document.getElementById('testConnection') as HTMLButtonElement;

/**
 * Load saved settings from storage
 */
async function loadSettings(): Promise<void> {
  try {
    const result = await chrome.storage.local.get(['apiKey', 'apiUrl']);

    if (result.apiKey) {
      apiKeyInput.value = result.apiKey;
    }

    if (result.apiUrl) {
      apiUrlInput.value = result.apiUrl;
    } else {
      apiUrlInput.value = DEFAULT_API_URL;
    }
  } catch (error) {
    console.error('[Sentinel] Failed to load settings:', error);
    showStatus('Failed to load settings', 'error');
  }
}

/**
 * Save settings to storage
 */
async function saveSettings(): Promise<void> {
  const apiKey = apiKeyInput.value.trim();
  const apiUrl = apiUrlInput.value.trim() || DEFAULT_API_URL;

  if (!apiKey) {
    showStatus('Please enter an API key', 'error');
    return;
  }

  try {
    await chrome.storage.local.set({
      apiKey,
      apiUrl
    });

    showStatus('Settings saved successfully!', 'success');
  } catch (error) {
    console.error('[Sentinel] Failed to save settings:', error);
    showStatus('Failed to save settings', 'error');
  }
}

/**
 * Test the API connection
 */
async function testConnection(): Promise<void> {
  const apiKey = apiKeyInput.value.trim();
  const apiUrl = apiUrlInput.value.trim() || DEFAULT_API_URL;

  if (!apiKey) {
    showStatus('Please enter an API key first', 'error');
    return;
  }

  showStatus('Testing connection...', 'info');
  testConnectionBtn.disabled = true;

  try {
    const response = await fetch(`${apiUrl}/api/v1/health`, {
      method: 'GET',
      headers: {
        'Authorization': `Bearer ${apiKey}`
      }
    });

    if (response.ok) {
      showStatus('Connection successful! API is reachable.', 'success');
    } else if (response.status === 401) {
      showStatus('Connection failed: Invalid API key', 'error');
    } else {
      showStatus(`Connection failed: ${response.status} ${response.statusText}`, 'error');
    }
  } catch (error) {
    console.error('[Sentinel] Connection test failed:', error);
    showStatus('Connection failed: Could not reach API. Check the URL.', 'error');
  } finally {
    testConnectionBtn.disabled = false;
  }
}

/**
 * Show status message
 */
function showStatus(message: string, type: 'success' | 'error' | 'info'): void {
  statusMessage.textContent = message;
  statusMessage.className = `status ${type}`;
  statusMessage.style.display = 'block';

  // Auto-hide after 5 seconds for success messages
  if (type === 'success') {
    setTimeout(() => {
      statusMessage.style.display = 'none';
    }, 5000);
  }
}

/**
 * Initialize the options page
 */
function initOptions(): void {
  // Load existing settings
  loadSettings();

  // Set up event listeners
  saveButton.addEventListener('click', saveSettings);
  testConnectionBtn.addEventListener('click', testConnection);

  // Allow Enter key to save
  apiKeyInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
      saveSettings();
    }
  });
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', initOptions);
