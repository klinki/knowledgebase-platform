/**
 * Shared type definitions for the Sentinel browser extension
 */

/**
 * Author data extracted from a tweet
 */
export interface AuthorData {
  username: string;
  display_name: string;
}

/**
 * Tweet data extracted from X/Twitter
 */
export interface TweetData {
  source: 'twitter';
  tweet_id: string;
  author: AuthorData;
  content: {
    text: string;
    timestamp: string | null;
    url: string;
  };
  captured_at: string;
}

/**
 * Generic content data that can be captured
 */
export interface ContentData {
  source: string;
  source_id: string;
  author?: AuthorData;
  content: {
    text: string;
    timestamp?: string | null;
    url: string;
  };
  captured_at: string;
}

/**
 * Response from the background script
 */
export interface SaveResponse {
  success: boolean;
  error?: string;
}
