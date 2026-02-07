/**
 * Sentinel Content Script
 * Injects "Save to Sentinel" buttons into X (Twitter) tweets and handles data extraction
 */

import { MAX_PROCESSED_TWEETS } from './constants.js';
import type { TweetData, AuthorData } from './types/index.js';

// Track processed tweets to avoid duplicate buttons (with size limit to prevent memory leak)
const processedTweets = new Set<string>();

/**
 * Add a tweet ID to the processed set with LRU eviction
 */
function addProcessedTweet(tweetId: string): void {
  if (processedTweets.size >= MAX_PROCESSED_TWEETS) {
    // Remove oldest entry (first in Set)
    const first = processedTweets.values().next().value;
    if (first) {
      processedTweets.delete(first);
    }
  }
  processedTweets.add(tweetId);
}

// Sentinel button SVG icon
const SENTINEL_ICON = `
<svg width="18" height="18" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
  <path d="M12 2L2 7L12 12L22 7L12 2Z" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
  <path d="M2 17L12 22L22 17" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
  <path d="M2 12L12 17L22 12" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"/>
</svg>
`;

/**
 * Extract tweet data from the DOM element
 */
function extractTweetData(tweetElement: HTMLElement): TweetData | null {
  try {
    // Get tweet ID from the article's data attributes or URL
    const tweetId = extractTweetId(tweetElement);
    if (!tweetId) {
      console.debug('[Sentinel] Could not extract tweet ID');
      return null;
    }

    // Extract author information
    const author = extractAuthor(tweetElement);
    if (!author) {
      console.debug('[Sentinel] Could not extract author');
      return null;
    }

    // Extract tweet text
    const text = extractText(tweetElement);

    // Extract timestamp
    const timestamp = extractTimestamp(tweetElement);

    // Extract URL
    const url = extractUrl(tweetElement, tweetId, author.username);

    return {
      source: 'twitter',
      tweet_id: tweetId,
      author,
      content: {
        text,
        timestamp,
        url
      },
      captured_at: new Date().toISOString()
    };
  } catch (error) {
    console.error('[Sentinel] Error extracting tweet data:', error);
    return null;
  }
}

/**
 * Extract tweet ID from the article element
 */
function extractTweetId(tweetElement: HTMLElement): string | null {
  // Try to get from data attributes first
  const tweetIdAttr = tweetElement.getAttribute('data-tweet-id');
  if (tweetIdAttr) return tweetIdAttr;

  // Try to extract from links in the tweet
  const timeLink = tweetElement.querySelector('time');
  if (timeLink) {
    const link = timeLink.closest('a');
    if (link) {
      const href = link.getAttribute('href');
      if (href) {
        const match = href.match(/\/status\/(\d+)/);
        if (match) return match[1];
      }
    }
  }

  // Try to find any status link
  const links = tweetElement.querySelectorAll('a[href*="/status/"]');
  for (const link of Array.from(links)) {
    const href = link.getAttribute('href');
    if (href) {
      const match = href.match(/\/status\/(\d+)/);
      if (match) return match[1];
    }
  }

  return null;
}

/**
 * Extract author information
 */
function extractAuthor(tweetElement: HTMLElement): AuthorData | null {
  // Look for the user link that contains the handle
  const userLinks = tweetElement.querySelectorAll('a[href^="/"]');

  for (const link of Array.from(userLinks)) {
    const href = link.getAttribute('href');
    if (href && href.match(/^\/[\w_]+$/)) {
      const username = href.slice(1); // Remove leading /

      // Find display name - usually in the same link or nearby
      const displayNameElement = link.querySelector('span span');
      const displayName = displayNameElement?.textContent || username;

      return {
        username: `@${username}`,
        display_name: displayName
      };
    }
  }

  return null;
}

/**
 * Extract tweet text content
 */
function extractText(tweetElement: HTMLElement): string {
  // Look for the tweet text container
  const textElements = tweetElement.querySelectorAll('[data-testid="tweetText"]');

  if (textElements.length > 0) {
    // Get the first one (main tweet, not reply)
    const textElement = textElements[0];
    return textElement.textContent || '';
  }

  // Fallback: look for any text content in the article
  const articleText = tweetElement.querySelector('[lang]');
  if (articleText) {
    return articleText.textContent || '';
  }

  return '';
}

/**
 * Extract timestamp from the tweet
 */
function extractTimestamp(tweetElement: HTMLElement): string | null {
  const timeElement = tweetElement.querySelector('time');
  if (timeElement) {
    const datetime = timeElement.getAttribute('datetime');
    if (datetime) return datetime;
  }
  return null;
}

/**
 * Extract or construct the tweet URL
 */
function extractUrl(tweetElement: HTMLElement, tweetId: string, username: string): string {
  // Try to find an existing status link
  const statusLink = tweetElement.querySelector(`a[href*="/status/${tweetId}"]`);
  if (statusLink) {
    const href = statusLink.getAttribute('href');
    if (href) {
      return href.startsWith('http') ? href : `https://x.com${href}`;
    }
  }

  // Construct URL from username and tweet ID
  const cleanUsername = username.replace('@', '');
  return `https://x.com/${cleanUsername}/status/${tweetId}`;
}

/**
 * Create the Sentinel save button
 */
function createSaveButton(tweetElement: HTMLElement): HTMLButtonElement {
  const button = document.createElement('button');
  button.className = 'sentinel-save-btn';
  button.setAttribute('aria-label', 'Save to Sentinel');
  button.setAttribute('title', 'Save to Sentinel');
  button.innerHTML = SENTINEL_ICON;

  button.addEventListener('click', async (e) => {
    e.preventDefault();
    e.stopPropagation();

    // Prevent double-clicks
    if (button.disabled) return;
    button.disabled = true;

    // Show loading state
    button.classList.add('saving');

    const tweetData = extractTweetData(tweetElement);

    if (tweetData) {
      try {
        // Send to background script
        const response = await chrome.runtime.sendMessage({
          type: 'SAVE_TWEET',
          data: tweetData
        });

        if (response && response.success) {
          button.classList.add('saved');
          button.classList.remove('saving');
          showTooltip(button, 'Saved to Sentinel!');
        } else {
          throw new Error((response && response.error) || 'Failed to save');
        }
      } catch (error) {
        console.error('[Sentinel] Failed to save tweet:', error);
        button.classList.remove('saving');
        showTooltip(button, 'Failed to save. Check API key in settings.');
      }
    } else {
      showTooltip(button, 'Could not extract tweet data');
    }

    // Re-enable button after a delay
    setTimeout(() => {
      button.disabled = false;
      button.classList.remove('saved');
    }, 2000);
  });

  return button;
}

/**
 * Show a temporary tooltip
 */
function showTooltip(button: HTMLButtonElement, message: string): void {
  const tooltip = document.createElement('div');
  tooltip.className = 'sentinel-tooltip';
  tooltip.textContent = message;
  button.appendChild(tooltip);

  setTimeout(() => {
    tooltip.remove();
  }, 2000);
}

/**
 * Inject the save button into a tweet's action bar
 */
function injectSaveButton(tweetElement: HTMLElement): void {
  // Get a unique identifier for this tweet
  const tweetId = extractTweetId(tweetElement);
  if (!tweetId) return;

  // Check if already processed
  if (processedTweets.has(tweetId)) return;
  addProcessedTweet(tweetId);

  // Find the action bar (reply, retweet, like buttons)
  const actionBar = tweetElement.querySelector('[role="group"]');
  if (!actionBar) return;

  // Check if button already exists
  if (actionBar.querySelector('.sentinel-save-btn')) return;

  // Create and append the button
  const saveButton = createSaveButton(tweetElement);

  // Wrap in a div to match X's button styling structure
  const wrapper = document.createElement('div');
  wrapper.className = 'sentinel-btn-wrapper';
  wrapper.appendChild(saveButton);

  actionBar.appendChild(wrapper);
}

/**
 * Process all visible tweets on the page
 */
function processVisibleTweets(): void {
  // Find all tweet articles
  const tweets = document.querySelectorAll('article[data-testid="tweet"]');

  tweets.forEach((tweet) => {
    injectSaveButton(tweet as HTMLElement);
  });
}

/**
 * Initialize the content script
 */
function initContentScript(): void {
  console.log('[Sentinel] Content script initialized');

  // Process existing tweets
  processVisibleTweets();

  // Set up MutationObserver to handle dynamically loaded tweets
  let processing = false;
  const observer = new MutationObserver((mutations) => {
    // Throttle processing to avoid performance issues
    if (processing) return;
    processing = true;

    requestAnimationFrame(() => {
      let shouldProcess = false;

      mutations.forEach((mutation) => {
        mutation.addedNodes.forEach((node) => {
          if (node instanceof HTMLElement) {
            // Check if the added node is a tweet or contains tweets
            if (node.matches('article[data-testid="tweet"]') ||
                node.querySelector('article[data-testid="tweet"]')) {
              shouldProcess = true;
            }
          }
        });
      });

      if (shouldProcess) {
        processVisibleTweets();
      }
      processing = false;
    });
  });

  // Observe the main timeline container (try more specific selectors first)
  const timeline = document.querySelector('[data-testid="primaryColumn"]')
    || document.querySelector('main')
    || document.body;
  observer.observe(timeline, {
    childList: true,
    subtree: true
  });
}

// Run initialization when DOM is ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', initContentScript);
} else {
  initContentScript();
}
