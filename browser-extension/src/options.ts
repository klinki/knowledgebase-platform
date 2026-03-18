/**
 * Sentinel Options Page
 * Allows users to configure the API URL and browser-extension authentication
 */

import { DEFAULT_API_URL, DEFAULT_APP_URL } from './constants.js';

interface DeviceStartResponse {
  deviceCode: string;
  userCode: string;
  verificationUrl: string;
  expiresAt: string;
  intervalSeconds: number;
}

interface TokenResponse {
  accessToken: string;
  refreshToken: string;
  expiresAt: string;
  user: {
    id: string;
    email: string;
    displayName: string;
    role: string;
  };
}

// DOM Elements
const apiKeyInput = document.getElementById('apiKey') as HTMLInputElement;
const apiUrlInput = document.getElementById('apiUrl') as HTMLInputElement;
const appUrlInput = document.getElementById('appUrl') as HTMLInputElement;
const bookmarkCaptureEnabledInput = document.getElementById('bookmarkCaptureEnabled') as HTMLInputElement;
const autoConfirmWebpageCaptureInput = document.getElementById('autoConfirmWebpageCapture') as HTMLInputElement;
const saveButton = document.getElementById('saveBtn') as HTMLButtonElement;
const statusMessage = document.getElementById('status') as HTMLDivElement;
const testConnectionBtn = document.getElementById('testConnection') as HTMLButtonElement;
const signInButton = document.getElementById('signInBtn') as HTMLButtonElement;
const signOutButton = document.getElementById('signOutBtn') as HTMLButtonElement;
const authStatusMessage = document.getElementById('authStatus') as HTMLDivElement;

/**
 * Load saved settings from storage
 */
async function loadSettings(): Promise<void> {
  try {
    const result = await chrome.storage.local.get([
      'apiKey',
      'apiUrl',
      'appUrl',
      'bookmarkCaptureEnabled',
      'autoConfirmWebpageCapture',
      'accessToken',
      'accessTokenExpiresAt',
      'authUser'
    ]);

    if (result.apiKey) {
      apiKeyInput.value = result.apiKey;
    }

    if (result.apiUrl) {
      apiUrlInput.value = result.apiUrl;
    } else {
      apiUrlInput.value = DEFAULT_API_URL;
    }

    if (result.appUrl) {
      appUrlInput.value = result.appUrl;
    } else {
      appUrlInput.value = DEFAULT_APP_URL;
    }

    if (result.bookmarkCaptureEnabled !== undefined) {
      bookmarkCaptureEnabledInput.checked = result.bookmarkCaptureEnabled;
    } else {
      bookmarkCaptureEnabledInput.checked = true;
    }

    if (result.autoConfirmWebpageCapture !== undefined) {
      autoConfirmWebpageCaptureInput.checked = result.autoConfirmWebpageCapture;
    }

    renderAuthStatus(result);
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
  const appUrl = appUrlInput.value.trim() || DEFAULT_APP_URL;
  const bookmarkCaptureEnabled = bookmarkCaptureEnabledInput.checked;
  const autoConfirmWebpageCapture = autoConfirmWebpageCaptureInput.checked;

  try {
    await chrome.storage.local.set({
      apiKey,
      apiUrl,
      appUrl,
      bookmarkCaptureEnabled,
      autoConfirmWebpageCapture
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
  const apiUrl = apiUrlInput.value.trim() || DEFAULT_API_URL;

  showStatus('Testing connection...', 'info');
  testConnectionBtn.disabled = true;

  try {
    const response = await fetch(`${apiUrl}/api/v1/health`, { method: 'GET' });

    if (response.ok) {
      showStatus('Connection successful! API is reachable.', 'success');
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

async function signIn(): Promise<void> {
  const apiUrl = apiUrlInput.value.trim() || DEFAULT_API_URL;

  showStatus('Starting Sentinel sign-in...', 'info');
  setAuthButtonsDisabled(true);

  try {
    const startResponse = await fetch(`${apiUrl}/api/auth/device/start`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({
        deviceName: 'Sentinel Browser Extension'
      })
    });

    if (!startResponse.ok) {
      throw new Error(`Failed to start sign-in: ${startResponse.status}`);
    }

    const payload = await startResponse.json() as DeviceStartResponse;
    window.open(payload.verificationUrl, '_blank', 'noopener,noreferrer');
    showStatus(`Waiting for approval. Use code ${payload.userCode} if prompted.`, 'info');

    const tokens = await pollForTokens(apiUrl, payload);
    await chrome.storage.local.set({
      accessToken: tokens.accessToken,
      refreshToken: tokens.refreshToken,
      accessTokenExpiresAt: tokens.expiresAt,
      authUser: tokens.user
    });

    renderAuthStatus({
      accessToken: tokens.accessToken,
      accessTokenExpiresAt: tokens.expiresAt,
      authUser: tokens.user
    });
    showStatus(`Signed in as ${tokens.user.displayName}.`, 'success');
  } catch (error) {
    console.error('[Sentinel] Sign-in failed:', error);
    showStatus(error instanceof Error ? error.message : 'Sign-in failed.', 'error');
  } finally {
    setAuthButtonsDisabled(false);
  }
}

async function pollForTokens(apiUrl: string, payload: DeviceStartResponse): Promise<TokenResponse> {
  const expiresAt = Date.parse(payload.expiresAt);

  while (Date.now() < expiresAt) {
    await delay(payload.intervalSeconds * 1000);

    const response = await fetch(`${apiUrl}/api/auth/device/poll`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json'
      },
      body: JSON.stringify({ deviceCode: payload.deviceCode })
    });

    if (response.status === 202) {
      continue;
    }

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`Sign-in was not approved: ${errorText}`);
    }

    return await response.json() as TokenResponse;
  }

  throw new Error('Sign-in timed out before approval completed.');
}

async function signOut(): Promise<void> {
  const apiUrl = apiUrlInput.value.trim() || DEFAULT_API_URL;
  const result = await chrome.storage.local.get(['refreshToken', 'accessToken']);

  setAuthButtonsDisabled(true);

  try {
    if (result.refreshToken) {
      await fetch(`${apiUrl}/api/auth/token/revoke`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
          ...(result.accessToken ? { 'Authorization': `Bearer ${result.accessToken}` } : {})
        },
        body: JSON.stringify({
          refreshToken: result.refreshToken
        })
      });
    }

    await chrome.storage.local.remove([
      'accessToken',
      'refreshToken',
      'accessTokenExpiresAt',
      'authUser'
    ]);

    renderAuthStatus({});
    showStatus('Signed out.', 'success');
  } catch (error) {
    console.error('[Sentinel] Sign-out failed:', error);
    showStatus('Failed to sign out cleanly, but local session was cleared.', 'error');
  } finally {
    setAuthButtonsDisabled(false);
  }
}

function renderAuthStatus(data: Record<string, unknown>): void {
  const authUser = data.authUser as TokenResponse['user'] | undefined;
  const expiresAt = typeof data.accessTokenExpiresAt === 'string'
    ? new Date(data.accessTokenExpiresAt)
    : null;

  if (authUser && expiresAt) {
    authStatusMessage.textContent = `Signed in as ${authUser.displayName} (${authUser.role}). Access token expires at ${expiresAt.toLocaleString()}.`;
    authStatusMessage.className = 'status success';
    authStatusMessage.style.display = 'block';
    return;
  }

  authStatusMessage.textContent = 'Not signed in. Use Sign In to connect this extension to Sentinel.';
  authStatusMessage.className = 'status info';
  authStatusMessage.style.display = 'block';
}

function setAuthButtonsDisabled(disabled: boolean): void {
  signInButton.disabled = disabled;
  signOutButton.disabled = disabled;
}

function delay(ms: number): Promise<void> {
  return new Promise((resolve) => {
    window.setTimeout(resolve, ms);
  });
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
  signInButton.addEventListener('click', signIn);
  signOutButton.addEventListener('click', signOut);

  // Allow Enter key to save
  apiKeyInput.addEventListener('keypress', (e) => {
    if (e.key === 'Enter') {
      saveSettings();
    }
  });
}

// Initialize when DOM is ready
document.addEventListener('DOMContentLoaded', initOptions);
