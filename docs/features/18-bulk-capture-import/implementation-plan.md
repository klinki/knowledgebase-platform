# Execution Plan: Bulk Capture Import

## Summary

- **Feature slug:** `18-bulk-capture-import`
- **Artifact type:** `implementation-plan.md`
- **Target doc path:** `docs/features/18-bulk-capture-import/implementation-plan.md`
- This supersedes the earlier archive-specific bulk-import draft for v1.
- Add a generic bulk capture creation endpoint so clients can submit an array of existing `CaptureRequestDto` entities in one request.
- Keep v1 intentionally simple:
  - new endpoint, not a route overload
  - all-or-nothing validation
  - one Hangfire job per accepted capture
  - no archive-specific dedupe or import metadata in this iteration

## Public Interfaces

- Add a new authenticated endpoint:
  - `POST /api/v1/capture/bulk`
- Request body:
  - JSON array of `CaptureRequestDto`
- Response:
  - `202 Accepted`
  - array of `CaptureAcceptedDto` in request order, one accepted result per submitted capture
- Validation behavior:
  - empty array returns `400 BadRequest`
  - any invalid item returns `400 BadRequest` for the whole request
  - existing single-item `POST /api/v1/capture` remains unchanged
- Default batch limit:
  - maximum `500` items per request in v1

## Implementation Changes

- Add a bulk request validator that reuses the current `CaptureRequestDto` rules with `RuleForEach`, plus batch-level checks for non-empty input and max size.
- Extend `ICaptureService` with a bulk creation method that accepts the owner user id plus a read-only list of `CaptureRequestDto`.
- Implement bulk creation in `CaptureService` as a batch-oriented version of the current single-capture path:
  - build all `RawCapture` entities in memory
  - resolve tags and labels across the whole batch instead of repeating per-item lookups
  - persist all accepted captures with one `SaveChangesAsync`
  - return accepted DTOs for all created captures
- Add repository helpers only where needed to support efficient batch tag and label resolution. Do not introduce archive-specific fields or schema changes in this feature.
- Add a new controller action in `CaptureController` that:
  - validates the array payload
  - resolves the authenticated user as today
  - calls the bulk service method
  - enqueues one Hangfire job per returned capture id
  - returns `202 Accepted` with the accepted DTO array
- Update `SentinelKnowledgebase.ImportCLI` to use the new bulk endpoint when importing Twitter likes:
  - chunk requests into batches of up to `500`
  - keep current progress and ETA reporting
  - keep a temporary fallback to single-item `POST /api/v1/capture` if `POST /api/v1/capture/bulk` returns `404` during rollout

## Test Plan

- Validator tests:
  - rejects empty arrays
  - rejects arrays above `500`
  - rejects the whole request when any item violates the existing capture rules
- Service tests:
  - creates multiple captures in one call
  - reuses existing tags and labels across the batch
  - persists once for the batch rather than once per item
- API tests:
  - valid bulk request returns `202 Accepted` with one accepted result per input item
  - invalid item causes whole-request `400 BadRequest`
  - Hangfire enqueue is called once per created capture
  - existing single-capture endpoint behavior is unchanged
- CLI tests:
  - importer sends batched requests to `/api/v1/capture/bulk`
  - importer falls back to the single-item endpoint when the bulk endpoint is unavailable
- Manual acceptance:
  - run the Twitter likes importer against the existing archive
  - confirm far fewer HTTP requests than the single-item path
  - confirm imported captures still appear and process normally

## Assumptions

- Bulk import in v1 is a transport and persistence optimization, not a new business workflow.
- Archive-specific dedupe, `importSource`, and `externalId` are deferred to a later feature if still needed.
- The main expected win in v1 comes from fewer HTTP round trips and fewer repeated database lookups/saves, while keeping the existing per-capture background processing model.
