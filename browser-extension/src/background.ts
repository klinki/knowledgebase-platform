/**
 * Sentinel Background Service Worker
 * Handles API communication and storage
 */

import { DEFAULT_API_URL } from './constants.js';
import type { TweetData } from './types/index.js';

/**
 * Handle messages from content scripts
 */
chrome.runtime.onMessage.addListener((request, _sender, sendResponse) => {
  if (request.type === 'SAVE_TWEET') {
    handleSaveTweet(request.data)
      .then((result) => sendResponse(result))
      .catch((error) => sendResponse({ success: false, error: error.message }));

    // Return true to indicate async response
    return true;
  }

  return false;
});

/**
 * Save tweet data to the Sentinel API
 */
async function handleSaveTweet(tweetData: TweetData): Promise<{ success: boolean; error?: string }> {
  try {
    // Get both API key and URL from storage
    const { apiKey, apiUrl } = await chrome.storage.local.get(['apiKey', 'apiUrl']);
    const baseUrl = apiUrl || DEFAULT_API_URL;

    if (!apiKey) {
      console.error('[Sentinel] API key not configured');
      return {
        success: false,
        error: 'API key not configured. Please set it in the extension options.'
      };
    }

    // Send to backend API
    const response = await fetch(`${baseUrl}/api/v1/capture`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${apiKey}`
      },
      body: JSON.stringify(tweetData)
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`API error: ${response.status} - ${errorText}`);
    }

    const result = await response.json();
    console.log('[Sentinel] Tweet saved successfully:', result);

    return { success: true };
  } catch (error) {
    console.error('[Sentinel] Failed to save tweet:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error'
    };
  }
}

/**
 * Initialize the background script
 */
chrome.runtime.onInstalled.addListener((details) => {
  console.log('[Sentinel] Extension installed/updated:', details.reason);

  // Set default values if needed
  chrome.storage.local.get(['apiKey', 'apiUrl']).then((result) => {
    if (!result.apiKey) {
      console.log('[Sentinel] No API key configured yet');
    }
    if (!result.apiUrl) {
      console.log('[Sentinel] Using default API URL:', DEFAULT_API_URL);
    }
  });
});
