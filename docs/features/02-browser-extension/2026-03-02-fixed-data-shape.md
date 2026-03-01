# 2026-03-02 Fixed Capture Data Shape (Extension -> API)

## Context
The browser extension and backend `CaptureController` were using different request shapes.

Backend expects `CaptureRequestDto`:
- `sourceUrl` (string)
- `contentType` (`Tweet|Article|Code|Note|Other`)
- `rawContent` (string, required, max 10000)
- `metadata` (string, optional)
- `tags` (string array, optional)

Extension previously sent source-specific nested payloads (tweet/webpage/selection), for example:
- `tweet_id`, `author`, `content`, `captured_at`
- `url`, `title`, `publish_date`, nested `metadata`

This caused contract drift and API validation failures.

## Changes Implemented

### 1) Added client-side payload mapping in extension background worker
In `browser-extension/src/background.ts`:
- Added `CaptureRequestPayload` interface.
- Added mapping functions:
  - `mapTweetToCaptureRequest(...)`
  - `mapWebpageToCaptureRequest(...)`
  - `mapSelectionToCaptureRequest(...)`
- Added `postCapture(...)` helper so all capture paths use `POST /api/v1/capture` with unified payload.
- Added `normalizeRawContent(...)` to trim and cap `rawContent` at `10000` chars to match backend validation.

### 2) Updated default API base URL
In `browser-extension/src/constants.ts`:
- Changed default URL from `http://localhost:3000` to `http://localhost:5000` to match current backend dev setup.

In `browser-extension/src/popup.ts`:
- Replaced hardcoded dashboard URL fallback with `DEFAULT_API_URL`.

### 3) Updated tests to match new contract
Updated:
- `browser-extension/tests/unit/background.test.ts`
- `browser-extension/tests/integration/message-passing.test.ts`
- `browser-extension/tests/unit/constants.test.ts`

Adjustments:
- Removed outdated `/api/v1/capture/webpage` expectation.
- Asserted payload fields now match backend contract (`sourceUrl`, `contentType`, `rawContent`, `metadata` string).
- Updated default URL expectations to port `5000`.

## Result
The extension now submits capture data in the backend DTO format for all capture sources (tweet, webpage, selection), reducing API contract mismatch risk and keeping validation behavior predictable.
