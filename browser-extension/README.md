# Sentinel Browser Extension

A Chrome extension that captures high-signal content from X (Twitter) and sends it to your Sentinel knowledge base.

## Features

- **One-click capture**: Save any tweet with a single click
- **Smart extraction**: Automatically extracts tweet ID, author, text, and timestamp
- **Secure storage**: API key stored locally in your browser
- **Real-time feedback**: Visual confirmation when tweets are saved

## Project Structure

```
browser-extension/
├── manifest.json          # Extension manifest (V3)
├── package.json           # NPM dependencies
├── tsconfig.json          # TypeScript configuration
├── options.html           # Settings page
├── popup.html             # Extension popup
├── src/
│   ├── content.ts         # Content script (DOM injection & scraping)
│   ├── content.css        # Styles for injected elements
│   ├── background.ts      # Service worker (API communication)
│   ├── options.ts         # Options page logic
│   ├── popup.ts           # Popup logic
│   └── types/
│       └── chrome.d.ts    # Chrome API type definitions
├── dist/                  # Compiled JavaScript (generated)
└── icons/                 # Extension icons (to be added)
```

## Setup Instructions

### 1. Install Dependencies

```bash
cd browser-extension
npm install
```

### 2. Build the Extension

```bash
npm run build
```

This compiles TypeScript files to `dist/` directory.

### 3. Load in Chrome

1. Open Chrome and navigate to `chrome://extensions/`
2. Enable "Developer mode" (toggle in top right)
3. Click "Load unpacked"
4. Select the `browser-extension` folder
5. The extension should now appear in your extensions list

### 4. Configure

1. Click the Sentinel icon in your browser toolbar
2. Click "Open Settings"
3. Enter your Sentinel API key and backend URL
4. Click "Test Connection" to verify

## Development

### Watch Mode

To automatically rebuild on file changes:

```bash
npm run watch
```

### File Structure Details

- **Content Script** (`src/content.ts`): Runs on X.com pages, injects save buttons, extracts tweet data
- **Background Script** (`src/background.ts`): Handles API communication with your backend
- **Options Page** (`options.html` + `src/options.ts`): User settings interface
- **Popup** (`popup.html` + `src/popup.ts`): Quick access from toolbar icon

## API Integration

The extension sends captured tweets to:

```
POST /api/v1/capture
Authorization: Bearer {API_KEY}
Content-Type: application/json
```

Payload structure:
```json
{
  "source": "twitter",
  "tweet_id": "1234567890",
  "author": {
    "username": "@handle",
    "display_name": "Display Name"
  },
  "content": {
    "text": "Tweet text content...",
    "timestamp": "2026-01-31T12:00:00Z",
    "url": "https://x.com/handle/status/1234567890"
  },
  "captured_at": "2026-01-31T12:30:00Z"
}
```

## Icons

Before packaging for distribution, add icon files to `icons/`:
- `icon16.png` (16x16)
- `icon48.png` (48x48)
- `icon128.png` (128x128)

## Troubleshooting

### Extension not showing on X.com
- Ensure you're on `x.com` (not `twitter.com` subdomain issues)
- Check console for errors
- Try refreshing the page

### "Failed to save" error
- Verify API key is set in settings
- Check that backend URL is correct
- Test connection in settings page

### Build errors
- Ensure Node.js is installed
- Run `npm install` to install dependencies
- Check TypeScript version compatibility

## Next Steps

This extension is Phase 1 of the Sentinel platform. Future phases include:
- Backend API for processing captured data
- Vector database for semantic search
- Dashboard UI for browsing insights
