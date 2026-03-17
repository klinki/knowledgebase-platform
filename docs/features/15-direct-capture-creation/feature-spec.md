# Feature: Frontend Direct Capture Creation (15-direct-capture-creation)

## Goal

Add a logged-in `Create Capture` page that lets users create captures directly
from the Angular frontend in two ways:

- URL input that creates a minimal article-style capture without remote fetching
- direct content input that submits typed or pasted content as a normal capture

## Acceptance Criteria

- [x] Logged-in users can open a dedicated `Create Capture` page from the shell navigation.
- [x] The page supports URL-only capture creation.
- [x] The page supports direct content capture creation.
- [x] URL-only capture submits as `Article` with generated minimal raw content and metadata.
- [x] Direct content capture requires user-selected content type.
- [x] Optional comma-separated tags are trimmed and submitted through the existing capture contract.
- [x] Backend validation accepts direct content capture without a URL.
- [x] Backend validation still rejects invalid URLs when a URL is supplied.
- [x] Successful creation routes the user to the new capture detail page.
- [x] The feature introduces no new server-side fetch endpoint or new capture-write endpoint.

## Architecture Notes

- Reuse the existing `POST /api/v1/capture` ingestion endpoint.
- Keep create-capture state inside the existing capture-focused frontend area rather than dashboard or search services.
- Treat URL-only capture as a minimal persisted capture, not webpage extraction.
- Keep the feature inside the authenticated shell only.

## Important Interfaces and Behavior

### Backend

- `POST /api/v1/capture`
- `CaptureRequestDto`
  - `sourceUrl`
  - `contentType`
  - `rawContent`
  - `metadata`
  - `tags`

### Frontend routes

- `/captures/new`

### Frontend behavior

- A single plain form supports both URL-only and direct-content modes.
- At least one of `sourceUrl` or `rawContent` is required.
- If only `sourceUrl` is provided:
  - `contentType = Article`
  - `rawContent` is generated from the URL
  - metadata records a frontend URL-input source marker
- If `rawContent` is provided:
  - user selects `contentType`
  - `sourceUrl` is optional
  - metadata records a frontend manual-input source marker

## Implementation Status

- [x] Add feature documentation for direct capture creation.
- [x] Relax backend capture validation to allow direct content without a URL.
- [x] Add backend validator coverage for URL-only and direct-content capture.
- [x] Extend frontend capture state with create-capture behavior.
- [x] Add a dedicated create-capture page.
- [x] Add shell route and navigation entry for `Create Capture`.
- [x] Add client-side mapping for URL-only and direct-content submissions.
- [x] Add frontend unit coverage for create-capture state and page behavior.
- [x] Add backend integration coverage for authenticated create flows.

## Verification Plan

- [x] API build passes.
- [x] Frontend build passes.
- [x] Validator accepts URL-only and direct-content payloads.
- [x] Validator rejects invalid supplied URLs.
- [x] Authenticated user can create URL-only capture.
- [x] Authenticated user can create direct-content capture.
- [x] Frontend create page enforces client-side validation.
- [x] Successful create navigates to capture detail.

## Assumptions and Defaults

- Feature folder name is `15-direct-capture-creation`.
- Entry point is a dedicated sidebar page.
- URL-based frontend capture in v1 does not fetch remote content.
- Direct input uses a plain textarea.
- Tags are a simple comma-separated input.
- URL-only captures default to `Article`.
- Direct text captures require user-selected content type.
