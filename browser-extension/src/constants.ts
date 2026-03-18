/**
 * Shared constants for the Sentinel browser extension
 */

/** Default API URL when not configured by user */
export const DEFAULT_API_URL = 'http://localhost:5000';

/** Default web app URL used by popup dashboard shortcut */
export const DEFAULT_APP_URL = 'http://localhost:4200';

/** Project documentation URL */
export const DOCUMENTATION_URL = 'https://github.com/microsoftdocs/sentinel-knowledge-engine#readme';

/** Maximum number of processed tweet IDs to track (prevents memory leak) */
export const MAX_PROCESSED_TWEETS = 1000;

/** Extension name for display purposes */
export const EXTENSION_NAME = 'Sentinel Knowledge Collector';

/** Extension version */
export const EXTENSION_VERSION = '1.0.0';
