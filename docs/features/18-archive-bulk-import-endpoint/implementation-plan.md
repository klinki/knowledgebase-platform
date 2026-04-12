# Execution Plan: Archive Bulk Import Endpoint

## Summary

- **Feature slug:** `18-archive-bulk-import-endpoint`
- **Artifact type:** `implementation-plan.md`
- **Target doc path:** `docs/features/18-archive-bulk-import-endpoint/implementation-plan.md`
- Add a dedicated authenticated backend bulk-import endpoint for archive ingestion batches.
- Make the new endpoint the primary ingestion path for [SentinelKnowledgebase.ImportCLI](/cli/src/SentinelKnowledgebase.ImportCLI/) when importing Twitter archive likes.
- Preserve the existing downstream behavior for accepted captures:
  - raw captures are stored as `Pending`
  - async processing is enqueued through Hangfire
  - LLM extraction, embeddings, and dashboard visibility stay unchanged
- Scope v1 to improving the existing Twitter archive likes importer. Do **not** broaden this feature into browser uploads or bookmark parsing.

## Public Interfaces

- Add a new authenticated endpoint:
  - `POST /api/v1/capture/archive-imports`
- Request contract:
  - `importSource`: required string such as `twitter_archive_like`
  - `items`: required array with a maximum batch size of `500`
  - each item contains:
    - `externalId`: required stable source-side identifier such as Twitter `tweetId`
    - `capture`: required `CaptureRequestDto` payload for the raw capture itself
- Response contract:
  - `totalReceived`
  - `acceptedCount`
  - `duplicateCount`
  - `rejectedCount`
  - `results[]` with one item per submitted record:
    - `externalId`
    - `status` = `accepted | duplicate | rejected`
    - `captureId` when accepted
    - `message` for rejected items
- Keep the existing single-item `POST /api/v1/capture` endpoint unchanged.
- Update [SentinelCaptureClient.cs](/cli/src/SentinelKnowledgebase.ImportCLI/SentinelCaptureClient.cs) so the CLI:
  - submits archive likes in batches to the new endpoint
  - stops calling `GET /api/v1/capture` for full-list dedupe on the hot path
  - optionally falls back to the current single-item flow if the server returns `404` during rollout

## Implementation Changes

- Add first-class archive dedupe fields to [RawCapture.cs](/backend/src/SentinelKnowledgebase.Domain/Entities/RawCapture.cs):
  - `ImportSource` as nullable string
  - `ExternalId` as nullable string
- Add EF Core configuration and a migration in [ApplicationDbContext.cs](/backend/src/SentinelKnowledgebase.Infrastructure/Data/ApplicationDbContext.cs):
  - index `(OwnerUserId, ImportSource, ExternalId)`
  - make the index unique for rows where both `ImportSource` and `ExternalId` are not null
- Keep archive dedupe out of free-form metadata. Metadata may still carry the original Twitter details, but server-side duplicate detection must rely on the first-class columns.
- Extend the application layer with a new bulk import service path:
  - add batch request/response DTOs under `Application/DTOs/Capture`
  - add a new `ICaptureService` method for archive import batches
  - implement a corresponding `CaptureService` bulk method that:
    - validates batch size and required fields
    - normalizes `externalId`, `SourceUrl`, tags, and labels
    - collapses duplicates within the incoming batch by `externalId`
    - loads existing archive keys for the owner and `importSource` in one query
    - preloads reusable tags, label categories, and label values once per batch
    - creates all new `RawCapture` entities in memory
    - persists accepted captures with one `SaveChangesAsync`
    - returns per-item outcome records for accepted, duplicate, and rejected items
- Extend the repository layer with batch-oriented primitives:
  - `AddRangeAsync(IEnumerable<RawCapture>)`
  - `GetExistingImportKeysAsync(Guid ownerUserId, string importSource, IReadOnlyCollection<string> externalIds)`
  - supporting helpers for preloading tags and labels by owner
- Add a new controller action in [CaptureController.cs](/backend/src/SentinelKnowledgebase.Api/Controllers/CaptureController.cs) that:
  - reuses the existing authenticated user resolution
  - returns `200 OK` with the batch summary and per-item outcomes
  - enqueues Hangfire processing only for newly accepted captures
- Handle race conditions explicitly:
  - if the unique import-key index is hit during save because another request inserted the same item concurrently, translate that conflict into `duplicate` outcomes rather than a batch-wide `500`
- Update the CLI importer:
  - chunk Twitter likes into fixed-size batches of `500`
  - call the new bulk endpoint instead of issuing one `POST /api/v1/capture` per like
  - treat server-returned `duplicate` results as skipped duplicates
  - keep progress, ETA, and summary reporting based on aggregate batch results
- Add logging and monitoring for bulk imports:
  - log `importSource`, batch size, accepted count, duplicate count, rejected count, and elapsed time
  - add counters for imported items and duplicate skips if the current monitoring surface supports it without broad observability refactoring

## Test Plan

- Unit tests for bulk import application logic:
  - reject empty batches and batches above the configured max size
  - reject items with missing `externalId` or invalid capture payload
  - collapse duplicate `externalId` values inside the same request
  - skip already-imported records returned by the repository dedupe lookup
  - reuse existing tags and labels instead of creating duplicates
- Persistence and integration tests:
  - migration creates the nullable import columns and unique filtered index
  - `GetExistingImportKeysAsync` returns the expected server-side duplicates
  - concurrent inserts of the same `(OwnerUserId, ImportSource, ExternalId)` resolve as duplicates rather than crashing the full batch
- API tests:
  - authenticated request with mixed accepted, duplicate, and rejected items returns the correct summary
  - Hangfire enqueue happens only for accepted items
  - legacy single-capture endpoint behavior remains unchanged
- CLI tests:
  - importer submits likes in batches instead of one request per item
  - importer handles mixed per-item outcomes from the bulk endpoint
  - importer falls back to single-item mode when the bulk endpoint is unavailable during rollout
- Manual acceptance:
  - run the Twitter likes importer against the provided archive in [twitter_export](/docs/features/18-archive-bulk-import-endpoint/twitter_export)
  - verify the number of HTTP requests drops from one request per new tweet to one request per batch
  - verify imported items still enter the normal async processing pipeline
  - compare total import time against the current single-item ingestion path and confirm a material reduction

## Assumptions

- This feature is a backend and CLI optimization for trusted archive import clients, not a public browser upload feature.
- v1 focuses on the current Twitter archive likes workflow, but the endpoint contract should stay generic enough for future archive sources.
- Archive duplicate detection should be based on `importSource + externalId`, not on parsing metadata blobs.
- Accepted items must remain fully compatible with the existing processing and retrieval pipeline.
