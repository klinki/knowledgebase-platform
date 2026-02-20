# Browser Extension Test Suite Implementation Plan

## Overview

Full test suite for the Sentinel browser extension using:
- **Unit Tests**: Vitest + vitest-chrome (mocked Chrome APIs)
- **Integration Tests**: Vitest (jsdom environment)
- **E2E Tests**: Playwright with mock HTML fixtures

## Test Stack

| Layer | Tool | Purpose |
|-------|------|---------|
| Unit Tests | Vitest + `vitest-chrome` | Test extraction functions, utilities with mocked Chrome APIs |
| Integration Tests | Vitest (jsdom) | Test message passing between content/background scripts |
| E2E Tests | Playwright | Test extension in real Chromium browser |

---

## Phase 1: Setup ✓

**Goal:** Install dependencies and create configuration files.

### Checklist

- [x] Install test dependencies
  - [x] vitest
  - [x] @vitest/coverage-v8
  - [x] @playwright/test
  - [x] jsdom
  - [x] happy-dom

- [x] Create configuration files
  - [x] vitest.config.ts
  - [x] playwright.config.ts

- [x] Create test utilities
  - [x] src/test-utils/setup.ts (Chrome API mocks)
  - [x] src/test-utils/helpers.ts (Test helper functions)

- [x] Update package.json
  - [x] Add test scripts

- [x] Create test directory structure
  - [x] tests/unit/
  - [x] tests/integration/
  - [x] tests/e2e/fixtures/

- [x] Commit Phase 1

---

## Phase 2: Unit Tests ✓

**Goal:** Create unit tests for core extraction and API functions.

### Checklist

- [x] Create tests/unit/content.test.ts
  - [x] extractTweetId() tests
    - [x] Extract from data-tweet-id attribute
    - [x] Extract from time link /status/123
    - [x] Extract from any status link
    - [x] Return null for invalid elements
    - [x] Handle edge cases
  - [x] extractAuthor() tests
    - [x] Parse username from user links
    - [x] Extract display name from span elements
    - [x] Handle missing display name
    - [x] Return null for invalid elements
  - [x] extractText() tests
    - [x] Extract from [data-testid="tweetText"]
    - [x] Fallback to [lang] elements
    - [x] Handle empty content
  - [x] extractTimestamp() tests
    - [x] Extract from time element datetime attribute
    - [x] Return null when not found
  - [x] extractUrl() tests
    - [x] Extract from status link
    - [x] Construct URL from username and tweet ID
    - [x] Handle relative URLs
  - [x] extractTweetData() tests
    - [x] Complete extraction from valid element
    - [x] Handle missing tweet ID
    - [x] Handle missing author
    - [x] Handle missing text/timestamp

- [x] Create tests/unit/background.test.ts
  - [x] handleSaveTweet() tests
    - [x] Use stored API key and URL
    - [x] Fall back to DEFAULT_API_URL
    - [x] Handle missing API key
    - [x] Handle network errors
    - [x] Handle API error responses
  - [x] handleSaveWebpage() tests
    - [x] Send correct payload structure
    - [x] Handle API errors
  - [x] extractWebpageData() tests
    - [x] Extract metadata from meta tags
    - [x] Extract main content
    - [x] Use URL hostname as fallback

- [x] Create tests/unit/constants.test.ts
  - [x] Export validation

- [x] Export functions from content.ts and background.ts for testing

- [x] Commit Phase 2

---

## Phase 3: Integration Tests ✓

**Goal:** Test message passing and storage integration.

### Checklist

- [x] Create tests/integration/message-passing.test.ts
  - [x] SAVE_TWEET flow
    - [x] Content script sends message
    - [x] Background receives and processes
    - [x] Verify API call is made
  - [x] SAVE_WEBPAGE flow
    - [x] Message handling
    - [x] API call verification
  - [x] Unknown message type handling

- [x] Create tests/integration/storage.test.ts
  - [x] API key storage/retrieval
  - [x] API URL storage/retrieval
  - [x] Settings persistence
  - [x] Multiple keys operations
  - [x] Clear storage

- [x] Commit Phase 3

---

## Phase 4: E2E Tests (Playwright)

**Goal:** Test extension in real browser with mock fixtures.

### Checklist

- [ ] Create E2E fixtures
  - [ ] tests/e2e/fixtures/extension.ts (Playwright fixtures)
  - [ ] tests/e2e/fixtures/tweet-mock.html (Mock X.com tweet)
  - [ ] tests/e2e/fixtures/webpage-mock.html (Mock webpage)

- [ ] Create tests/e2e/extension-load.test.ts
  - [ ] Extension installs without errors
  - [ ] Service worker initializes

- [ ] Create tests/e2e/options-page.test.ts
  - [ ] Navigate to options page
  - [ ] API key save/persistence
  - [ ] Connection test functionality

- [ ] Create tests/e2e/tweet-capture.test.ts
  - [ ] Button injection on mock tweet
  - [ ] Click triggers API call
  - [ ] Error handling (missing API key)
  - [ ] Success state visual feedback

- [ ] Create tests/e2e/webpage-capture.test.ts
  - [ ] Context menu capture
  - [ ] Bookmark capture flow (if testable)

- [ ] Commit Phase 4

---

## Phase 5: CI/CD

**Goal:** Integrate tests into GitHub Actions.

### Checklist

- [ ] Create .github/workflows/test.yml
  - [ ] Unit test job
  - [ ] E2E test job
  - [ ] Coverage reporting

- [ ] Update AGENTS.md with test commands

- [ ] Commit Phase 5

---

## Test Directory Structure

```
browser-extension/
├── vitest.config.ts
├── playwright.config.ts
├── src/
│   └── test-utils/
│       ├── setup.ts
│       └── helpers.ts
├── tests/
│   ├── unit/
│   │   ├── content.test.ts
│   │   ├── background.test.ts
│   │   └── constants.test.ts
│   ├── integration/
│   │   ├── message-passing.test.ts
│   │   └── storage.test.ts
│   └── e2e/
│       ├── fixtures/
│       │   ├── extension.ts
│       │   ├── tweet-mock.html
│       │   └── webpage-mock.html
│       ├── extension-load.test.ts
│       ├── options-page.test.ts
│       ├── tweet-capture.test.ts
│       └── webpage-capture.test.ts
└── package.json
```

## NPM Scripts

```json
{
  "scripts": {
    "test": "vitest run",
    "test:watch": "vitest",
    "test:coverage": "vitest run --coverage",
    "test:e2e": "playwright test",
    "test:e2e:ui": "playwright test --ui",
    "test:all": "npm run test && npm run test:e2e"
  }
}
```

## Estimated Effort

| Phase | Time |
|-------|------|
| Phase 1: Setup | 1 hour |
| Phase 2: Unit Tests | 3-4 hours |
| Phase 3: Integration Tests | 2 hours |
| Phase 4: E2E Tests | 3-4 hours |
| Phase 5: CI/CD | 30 min |
| **Total** | **9-12 hours** |
