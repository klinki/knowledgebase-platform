/**
 * Sentinel Background Service Worker
 * Handles API communication and storage
 */

// API configuration
const API_BASE_URL = 'http://localhost:3000'; // Update this with your backend URL

/**
 * Handle messages from content scripts
 */
chrome.runtime.onMessage.addListener((request, sender, sendResponse) => {
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
    // Get the API key from storage
    const { apiKey } = await chrome.storage.local.get('apiKey');

    if (!apiKey) {
      console.error('[Sentinel] API key not configured');
      return {
        success: false,
        error: 'API key not configured. Please set it in the extension options.'
      };
    }

    // Send to backend API
    const response = await fetch(`${API_BASE_URL}/api/v1/capture`, {
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
  chrome.storage.local.get('apiKey').then((result) => {
    if (!result.apiKey) {
      console.log('[Sentinel] No API key configured yet');
    }
  });
});

// Type definitions
interface AuthorData {
  username: string;
  display_name: string;
}

interface TweetData {
  source: string;
  tweet_id: string;
  author: AuthorData;
  content: {
    text: string;
    timestamp: string | null;
    url: string;
  };
  captured_at: string;
}
