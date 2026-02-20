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

## Phase 2: Unit Tests

**Goal:** Create unit tests for core extraction and API functions.

### Checklist

- [ ] Create tests/unit/content.test.ts
  - [ ] extractTweetId() tests
    - [ ] Extract from data-tweet-id attribute
    - [ ] Extract from time link /status/123
    - [ ] Extract from any status link
    - [ ] Return null for invalid elements
    - [ ] Handle edge cases
  - [ ] extractAuthor() tests
    - [ ] Parse username from user links
    - [ ] Extract display name from span elements
    - [ ] Handle missing display name
    - [ ] Return null for invalid elements
  - [ ] extractText() tests
    - [ ] Extract from [data-testid="tweetText"]
    - [ ] Fallback to [lang] elements
    - [ ] Handle empty content
  - [ ] extractTimestamp() tests
    - [ ] Extract from time element datetime attribute
    - [ ] Return null when not found
  - [ ] extractUrl() tests
    - [ ] Extract from status link
    - [ ] Construct URL from username and tweet ID
    - [ ] Handle relative URLs
  - [ ] addProcessedTweet() LRU tests
    - [ ] Add entries up to limit
    - [ ] Evict oldest when limit reached

- [ ] Create tests/unit/background.test.ts
  - [ ] handleSaveTweet() tests
    - [ ] Use stored API key and URL
    - [ ] Fall back to DEFAULT_API_URL
    - [ ] Handle missing API key
    - [ ] Handle network errors
  - [ ] handleSaveWebpage() tests
    - [ ] Send correct payload structure
    - [ ] Handle API errors
  - [ ] API key/URL handling tests
    - [ ] Retrieve from chrome.storage.local
    - [ ] Use defaults when not configured

- [ ] Create tests/unit/constants.test.ts
  - [ ] Export validation

- [ ] Export functions from content.ts for testing

- [ ] Commit Phase 2

---

## Phase 3: Integration Tests

**Goal:** Test message passing and storage integration.

### Checklist

- [ ] Create tests/integration/message-passing.test.ts
  - [ ] SAVE_TWEET flow
    - [ ] Content script sends message
    - [ ] Background receives and processes
    - [ ] Verify API call is made
  - [ ] SAVE_WEBPAGE flow
    - [ ] Message handling
    - [ ] API call verification
  - [ ] EXTRACT_WEBPAGE_CONTENT flow
    - [ ] Script injection trigger
    - [ ] Data extraction return

- [ ] Create tests/integration/storage.test.ts
  - [ ] API key storage/retrieval
  - [ ] API URL storage/retrieval
  - [ ] Settings persistence

- [ ] Commit Phase 3

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
