/**
 * Sentinel Background Service Worker
 * Handles API communication, storage, bookmark events, and context menus
 */

import { DEFAULT_API_URL } from './constants.js';

interface WebpageData {
  source: 'webpage';
  url: string;
  title: string;
  author: string | null;
  publish_date: string | null;
  description: string | null;
  content: {
    text: string;
    html: string;
    excerpt: string;
  };
  metadata: {
    site_name: string | null;
    favicon: string | null;
    language: string | null;
  };
  captured_at: string;
}

interface PendingBookmark {
  url: string;
  title: string;
  timestamp: number;
}

/**
 * Handle messages from content scripts
 */
chrome.runtime.onMessage.addListener((request, _sender, sendResponse) => {
  if (request.type === 'SAVE_TWEET') {
    handleSaveTweet(request.data)
      .then((result) => sendResponse(result))
      .catch((error) => sendResponse({ success: false, error: error.message }));

    return true;
  }

  if (request.type === 'SAVE_WEBPAGE') {
    handleSaveWebpage(request.data)
      .then((result) => sendResponse(result))
      .catch((error) => sendResponse({ success: false, error: error.message }));

    return true;
  }

  if (request.type === 'EXTRACT_WEBPAGE_CONTENT') {
    handleExtractWebpageContent(request.data)
      .then((result) => sendResponse(result))
      .catch((error) => sendResponse({ success: false, error: error.message }));

    return true;
  }

  return false;
});

/**
 * Save tweet data to the Sentinel API
 */
async function handleSaveTweet(tweetData: TweetData): Promise<{ success: boolean; error?: string }> {
  try {
    const { apiKey, apiUrl } = await chrome.storage.local.get(['apiKey', 'apiUrl']);
    const baseUrl = apiUrl || DEFAULT_API_URL;

    if (!apiKey) {
      console.error('[Sentinel] API key not configured');
      return {
        success: false,
        error: 'API key not configured. Please set it in the extension options.'
      };
    }

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
 * Save webpage data to the Sentinel API
 */
async function handleSaveWebpage(webpageData: WebpageData): Promise<{ success: boolean; error?: string }> {
  try {
    const { apiKey, apiUrl } = await chrome.storage.local.get(['apiKey', 'apiUrl']);
    const baseUrl = apiUrl || DEFAULT_API_URL;

    if (!apiKey) {
      console.error('[Sentinel] API key not configured');
      return {
        success: false,
        error: 'API key not configured. Please set it in the extension options.'
      };
    }

    const response = await fetch(`${baseUrl}/api/v1/capture/webpage`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${apiKey}`
      },
      body: JSON.stringify(webpageData)
    });

    if (!response.ok) {
      const errorText = await response.text();
      throw new Error(`API error: ${response.status} - ${errorText}`);
    }

    const result = await response.json();
    console.log('[Sentinel] Webpage saved successfully:', result);

    return { success: true };
  } catch (error) {
    console.error('[Sentinel] Failed to save webpage:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error'
    };
  }
}

/**
 * Extract webpage content by injecting content script
 */
async function handleExtractWebpageContent(tabInfo: { tabId: number; url: string; title: string }): Promise<{ success: boolean; data?: WebpageData; error?: string }> {
  try {
    const results = await chrome.scripting.executeScript({
      target: { tabId: tabInfo.tabId },
      func: extractWebpageData,
      args: [tabInfo.url, tabInfo.title]
    });

    if (results && results[0] && results[0].result) {
      return { success: true, data: results[0].result };
    }

    return { success: false, error: 'Failed to extract content' };
  } catch (error) {
    console.error('[Sentinel] Failed to extract webpage:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error'
    };
  }
}

/**
 * Function to be injected into webpage for content extraction
 */
function extractWebpageData(url: string, title: string): WebpageData | null {
  function getMetaContent(property: string): string | null {
    const meta = document.querySelector(`meta[property="${property}"]`) || 
                 document.querySelector(`meta[name="${property}"]`);
    return meta?.getAttribute('content') || null;
  }

  function extractAuthor(): string | null {
    return getMetaContent('article:author') || 
           getMetaContent('author') ||
           document.querySelector('meta[name="author"]')?.getAttribute('content') || null;
  }

  function extractPublishDate(): string | null {
    return getMetaContent('article:published_time') || 
           getMetaContent('datePublished') ||
           getMetaContent('date') || null;
  }

  function extractMainContent(): { text: string; html: string; excerpt: string } {
    const selectors = [
      'article',
      '[role="main"]',
      '.post-content',
      '.entry-content',
      '.article-content',
      '.content',
      '#content',
      '#main',
      'main'
    ];

    let contentElement: Element | null = null;
    
    for (const selector of selectors) {
      const el = document.querySelector(selector);
      if (el && el.textContent && el.textContent.length > 200) {
        contentElement = el;
        break;
      }
    }

    if (!contentElement) {
      const paragraphs = document.querySelectorAll('p');
      let textContent = '';
      paragraphs.forEach(p => {
        if (p.textContent && p.textContent.length > 50) {
          textContent += p.textContent + '\n\n';
        }
      });
      
      if (textContent.length > 200) {
        return {
          text: textContent.trim(),
          html: '',
          excerpt: textContent.substring(0, 200).trim() + '...'
        };
      }
    }

    if (contentElement) {
      const clone = contentElement.cloneNode(true) as Element;
      
      const removeSelectors = ['script', 'style', 'nav', 'header', 'footer', 'aside', '.ad', '.advertisement', '.social-share', '.comments'];
      removeSelectors.forEach(sel => {
        clone.querySelectorAll(sel).forEach(el => el.remove());
      });

      const text = clone.textContent?.trim() || '';
      const html = clone.innerHTML;
      const excerpt = text.substring(0, 200).trim() + (text.length > 200 ? '...' : '');

      return { text, html, excerpt };
    }

    return { text: document.body.textContent?.trim() || '', html: '', excerpt: '' };
  }

  const content = extractMainContent();
  const siteName = getMetaContent('og:site_name') || 
                   document.querySelector('title')?.textContent?.split('|')[0]?.trim() ||
                   new URL(url).hostname;

  return {
    source: 'webpage',
    url: url,
    title: title || document.title,
    author: extractAuthor(),
    publish_date: extractPublishDate(),
    description: getMetaContent('description') || getMetaContent('og:description'),
    content: content,
    metadata: {
      site_name: siteName || new URL(url).hostname,
      favicon: `${new URL(url).origin}/favicon.ico`,
      language: document.documentElement.lang || null
    },
    captured_at: new Date().toISOString()
  };
}

/**
 * Initialize the background script
 */
chrome.runtime.onInstalled.addListener(async (details) => {
  console.log('[Sentinel] Extension installed/updated:', details.reason);

  // Set default values if needed
  const result = await chrome.storage.local.get(['apiKey', 'apiUrl', 'bookmarkCaptureEnabled']);
  if (!result.apiKey) {
    console.log('[Sentinel] No API key configured yet');
  }
  if (!result.apiUrl) {
    console.log('[Sentinel] Using default API URL:', DEFAULT_API_URL);
  }
  if (result.bookmarkCaptureEnabled === undefined) {
    await chrome.storage.local.set({ bookmarkCaptureEnabled: true });
  }

  // Create context menu for text selection
  chrome.contextMenus?.create({
    id: 'saveSelectionToSentinel',
    title: 'Save Selection to Sentinel',
    contexts: ['selection']
  });

  // Create context menu for page
  chrome.contextMenus?.create({
    id: 'savePageToSentinel',
    title: 'Save Page to Sentinel',
    contexts: ['page']
  });
});

// Bookmark event listener
chrome.bookmarks.onCreated.addListener(async (_id, bookmark) => {
  if (!bookmark.url || bookmark.url.startsWith('javascript:') || 
      bookmark.url.startsWith('chrome://') || bookmark.url.startsWith('file://')) {
    return;
  }

  const result = await chrome.storage.local.get(['bookmarkCaptureEnabled', 'autoConfirmWebpageCapture']);
  if (!result.bookmarkCaptureEnabled) {
    return;
  }

  // Check if URL is blacklisted
  const { captureBlacklist = [] } = await chrome.storage.local.get('captureBlacklist');
  const urlObj = new URL(bookmark.url);
  if (captureBlacklist.some((domain: string) => urlObj.hostname.includes(domain))) {
    return;
  }

  if (result.autoConfirmWebpageCapture) {
    // Auto-capture without confirmation
    if (bookmark.url) {
      captureWebpage(bookmark.url, bookmark.title || '');
    }
  } else {
    // Show confirmation notification
    if (!bookmark.url) return;
    
    chrome.notifications.create({
      type: 'basic',
      iconUrl: 'icons/icon48.svg',
      title: 'Save to Sentinel?',
      message: `Save "${bookmark.title}" to your knowledge base?`,
      buttons: [
        { title: 'Yes, Save it' },
        { title: 'No, Skip' }
      ],
      requireInteraction: true
    }, (notificationId) => {
      if (notificationId && bookmark.url) {
        // Store pending bookmark info
        const pendingBookmarks: Record<string, PendingBookmark> = {};
        pendingBookmarks[notificationId] = {
          url: bookmark.url,
          title: bookmark.title || '',
          timestamp: Date.now()
        };
        chrome.storage.local.set({ pendingBookmarks });
      }
    });
  }
});

// Handle notification button clicks
chrome.notifications.onButtonClicked.addListener(async (notificationId, buttonIndex) => {
  if (buttonIndex === 0) {
    const { pendingBookmarks = {} } = await chrome.storage.local.get('pendingBookmarks');
    const pending = pendingBookmarks[notificationId];
    
    if (pending) {
      await captureWebpage(pending.url, pending.title);
      
      chrome.notifications.create({
        type: 'basic',
        iconUrl: 'icons/icon48.svg',
        title: 'Saved!',
        message: 'Page saved to your Sentinel knowledge base.'
      });

      // Clean up
      delete pendingBookmarks[notificationId];
      await chrome.storage.local.set({ pendingBookmarks });
    }
  }
});

// Handle context menu clicks
chrome.contextMenus?.onClicked.addListener(async (info, tab) => {
  if (info.menuItemId === 'saveSelectionToSentinel' && info.selectionText && tab) {
    captureSelection(tab.url || '', tab.title || '', info.selectionText);
  } else if (info.menuItemId === 'savePageToSentinel' && tab) {
    captureWebpage(tab.url || '', tab.title || '');
  }
});

/**
 * Capture a webpage
 */
async function captureWebpage(url: string, title: string): Promise<{ success: boolean; error?: string }> {
  try {
    const tabs = await chrome.tabs.query({ url: url, active: true, currentWindow: true });
    const tab = tabs[0];

    if (!tab || !tab.id) {
      // Tab not found, create extraction data from URL
      const webpageData: WebpageData = {
        source: 'webpage',
        url: url,
        title: title,
        author: null,
        publish_date: null,
        description: null,
        content: {
          text: '',
          html: '',
          excerpt: ''
        },
        metadata: {
          site_name: new URL(url).hostname,
          favicon: `${new URL(url).origin}/favicon.ico`,
          language: null
        },
        captured_at: new Date().toISOString()
      };

      return handleSaveWebpage(webpageData);
    }

    const result = await handleExtractWebpageContent({
      tabId: tab.id,
      url: tab.url || url,
      title: tab.title || title
    });

    if (result.success && result.data) {
      return handleSaveWebpage(result.data);
    }

    return { success: false, error: result.error || 'Failed to extract content' };
  } catch (error) {
    console.error('[Sentinel] Failed to capture webpage:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error'
    };
  }
}

/**
 * Capture selected text from a page
 */
async function captureSelection(url: string, title: string, selection: string): Promise<{ success: boolean; error?: string }> {
  try {
    const selectionData = {
      source: 'webpage_selection',
      url: url,
      title: title,
      content: {
        text: selection,
        excerpt: selection.substring(0, 200)
      },
      context: {
        surrounding_text: null,
        selection_only: true
      },
      captured_at: new Date().toISOString()
    };

    const { apiKey, apiUrl } = await chrome.storage.local.get(['apiKey', 'apiUrl']);
    const baseUrl = apiUrl || DEFAULT_API_URL;

    if (!apiKey) {
      return {
        success: false,
        error: 'API key not configured'
      };
    }

    const response = await fetch(`${baseUrl}/api/v1/capture/webpage`, {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${apiKey}`
      },
      body: JSON.stringify(selectionData)
    });

    if (!response.ok) {
      throw new Error(`API error: ${response.status}`);
    }

    chrome.notifications.create({
      type: 'basic',
      iconUrl: 'icons/icon48.svg',
      title: 'Saved!',
      message: 'Selection saved to your Sentinel knowledge base.'
    });

    return { success: true };
  } catch (error) {
    console.error('[Sentinel] Failed to save selection:', error);
    return {
      success: false,
      error: error instanceof Error ? error.message : 'Unknown error'
    };
  }
}

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

// Exports for testing
export {
  handleSaveTweet,
  handleSaveWebpage,
  handleExtractWebpageContent,
  extractWebpageData,
  captureWebpage,
  captureSelection,
}

export type { AuthorData, TweetData, WebpageData, PendingBookmark }
