# Execution Plan: Twitter Archive Likes Import

## Summary

- **Feature slug:** `17-twitter-archive-import`
- **Artifact type:** `implementation-plan.md`
- **Target doc path:** `docs/features/17-twitter-archive-import/implementation-plan.md`
- Build **v1 as a standalone local CLI importer**, not a server-side upload feature.
- Scope v1 to **Twitter likes import only**. Do **not** implement bookmarks in v1 because the provided archive does not expose a reliable dedicated bookmarks dataset.
- Reuse the existing authenticated capture flow:
  - device auth: `POST /api/auth/device/start`, `POST /api/auth/device/poll`, `POST /api/auth/token/refresh`
  - ingestion: `POST /api/v1/capture`
  - dedupe source: `GET /api/v1/capture`
- Keep the backend and frontend unchanged in v1. The importer is the only new delivery surface.

## Public Interfaces

- Add a new client CLI project at [cli/src/SentinelKnowledgebase.ImportCLI/](/cli/src/SentinelKnowledgebase.ImportCLI/).
- Expose one primary command for v1:
  - `sentinel-import twitter likes --input <path> --api-url <url>`
- `--input` accepts either:
  - the unzipped archive directory root, or
  - the `.zip` archive path
- Authentication behavior is fixed:
  - if a valid cached access token exists, use it
  - otherwise start device auth, show verification URL and user code, poll until approved, then cache refresh/access tokens locally
- No new REST endpoints, DTOs, or frontend upload contracts in v1.

## Implementation Changes

- Implement the importer as a pure HTTP client CLI. Do **not** extend the existing server-admin CLI because that tool is for local server administration, not end-user remote ingestion.
- Split the importer into four internal layers:
  - archive input resolver: detect zip vs directory and open the archive without requiring manual unzip
  - Twitter dataset reader: load `data/manifest.js`, validate archive shape, then parse `data/like.js`
  - capture mapper: convert each like into the existing capture DTO
  - ingest runner: auth, dedupe, post, report results
- Parse `like.js` by stripping the JS assignment wrapper and deserializing the payload array. Treat malformed rows as skipped items, not fatal importer failure.
- Map each like to the existing capture request as:
  - `contentType = Tweet`
  - `sourceUrl =` normalized tweet URL derived from `tweetId`/`expandedUrl`
  - `rawContent = fullText`
  - `labels = [{ category: "Source", value: "Twitter" }]`
  - `tags = ["twitter", "archive-import"]`
  - `metadata` must include at least:
    - `source = "twitter"`
    - `importSource = "twitter_archive_like"`
    - `tweetId`
    - `expandedUrl`
    - `capturedAt` = import timestamp
    - archive account/generation details from `manifest.js` when available
- Dedupe is **client-side** in v1:
  - call `GET /api/v1/capture`
  - filter captures whose metadata contains a Twitter `tweetId`
  - skip any archive item whose `tweetId` already exists
- Output a final import summary with:
  - total likes read
  - duplicates skipped
  - successfully submitted captures
  - failed submissions
  - malformed/skipped records
- Keep the parser extensible for a later `bookmarks` source by defining an internal source-adapter boundary now, but do not implement bookmark ingestion logic in v1.

## Test Plan

- Unit tests for archive parsing:
  - parse `like.js` rows from directory input
  - parse the same payload from zip input
  - reject missing or invalid `like.js`
  - tolerate malformed rows and continue
- Unit tests for mapping:
  - `tweetId`, `rawContent`, `sourceUrl`, tags, labels, and metadata fields are generated correctly
  - empty `fullText` falls back to a safe minimal raw payload rather than crashing
- Unit tests for dedupe:
  - existing `tweetId` is skipped
  - non-Twitter captures are ignored during duplicate detection
- Integration-style CLI tests with mocked HTTP:
  - first run authenticates and submits new captures
  - second run skips duplicates
  - expired access token refreshes successfully
  - failed capture posts are reported and do not abort the full batch
- Manual acceptance:
  - run against the provided archive in [twitter_export](/docs/features/17-twitter-archive-import/twitter_export)
  - verify imported items appear as Twitter captures and enter the normal async processing pipeline

## Assumptions

- v1 solves **your local archive import workflow**, not a generic end-user web upload workflow.
- Bookmarks are out of scope for v1 until a reliable export source is identified.
- The existing `GET /api/v1/capture` volume is acceptable for v1 dedupe. If this becomes slow later, add a dedicated server-side duplicate lookup in a follow-up feature rather than broadening v1 now.
- Imported likes should be treated exactly like other tweet captures once accepted by the API: same queueing, same LLM processing, same embeddings, same dashboard visibility.
