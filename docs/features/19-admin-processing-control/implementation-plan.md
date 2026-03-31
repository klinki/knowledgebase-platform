# Implementation Plan: Admin Capture Processing Control

## Summary

- **Feature slug:** `19-admin-processing-control`
- **Artifact type:** `implementation-plan.md`
- **Target doc path:** `docs/features/19-admin-processing-control/implementation-plan.md`
- Add an admin-only operations panel to the existing `/dashboard` page so admins can pause and resume capture processing globally.
- Chosen behavior for v1:
  - pause scope is global across all users and capture types
  - new captures are still accepted while paused
  - in-flight jobs are allowed to finish
  - admins get a full ops panel, not just a toggle

## Public Interfaces

- Add a new admin-only API group under `api/v1/admin/processing`.
- `GET /api/v1/admin/processing` returns `CaptureProcessingAdminOverviewDto` with:
  - `isPaused`
  - `changedAt`
  - `changedByDisplayName`
  - `captureCounts` for `pending`, `processing`, `completed`, `failed`
  - `jobCounts` for Hangfire `enqueued`, `scheduled`, `processing`, `failed`
  - `recentCaptures` as the latest 10 system-wide captures, reusing the existing list-item shape
- `POST /api/v1/admin/processing/pause` flips the global state to paused and returns the updated overview.
- `POST /api/v1/admin/processing/resume` flips the global state to running, enqueues all current `Pending` captures, and returns the updated overview.
- Add a persistent singleton entity `CaptureProcessingControl` with `IsPaused`, `ChangedAt`, and `ChangedByUserId`.
- Keep existing capture create, bulk-create, and retry endpoints unchanged at the route/schema level. Only the `message` field should reflect whether processing was enqueued immediately or deferred because processing is paused.

## Implementation Changes

- Add `CaptureProcessingControl` to the EF model plus a migration that seeds a single default row with `IsPaused = false`.
- Add an admin processing service responsible for:
  - loading and updating the singleton control row
  - resolving the acting admin display name
  - reading global raw-capture counts by status
  - reading Hangfire queue counts via Hangfire monitoring APIs
  - returning the admin overview DTO
  - enqueuing all current `Pending` capture ids on resume
- Extend raw-capture data access with global helpers for:
  - latest system-wide captures
  - counts grouped by `CaptureStatus`
  - current pending capture ids for the resume sweep
- Update `CaptureController` create, bulk-create, and retry actions so they:
  - still persist the capture or retry state as today
  - check the processing control after persistence
  - enqueue immediately only when processing is running
  - skip enqueue when paused and return a paused-aware accepted message
- Update `CaptureService.ProcessCaptureAsync` so it:
  - checks the processing control before switching a capture to `Processing`
  - if paused, leaves the capture in `Pending`, schedules the same job again after a fixed `60` second delay, and exits successfully
  - no-ops when the capture is already `Completed` or already `Processing`, so delayed pause-era jobs and resume-triggered jobs do not double-process
- Keep the existing retry-state filter behavior for real failures and retries; do not introduce pause as a new capture status in v1.
- Add an admin-only state service in the frontend for the new admin endpoints.
- Render the ops panel only for admins at the top of the existing dashboard page. Non-admin dashboard behavior stays unchanged.
- The ops panel should show:
  - current state: `Running` or `Paused`
  - who changed it and when
  - pause/resume button with in-flight loading protection
  - global capture status cards
  - Hangfire job-count cards
  - recent system captures with status, timestamp, and source

## Test Plan

- Unit tests:
  - pausing updates the singleton row with actor and timestamp
  - resuming enqueues every current pending capture id
  - overview mapping returns DB status counts plus Hangfire job counts
  - `ProcessCaptureAsync` reschedules and exits while paused without setting `Processing`
  - `ProcessCaptureAsync` no-ops for captures already `Completed` or already `Processing`
- Integration tests:
  - `GET /api/v1/admin/processing` returns `401` for anonymous and `403` for non-admin users
  - admin can pause and resume successfully
  - captures created while paused return `202 Accepted` and remain `Pending`
  - resuming causes paused pending captures to become eligible for processing
  - existing non-admin dashboard and capture endpoints remain unchanged apart from paused acceptance messaging
- Frontend tests:
  - admin dashboard renders the ops panel
  - non-admin dashboard does not render the ops panel
  - pause/resume actions refresh state and disable the button during submission
  - counts and recent captures render from the admin overview payload
- Manual acceptance:
  - pause processing as admin
  - submit one or more captures and verify they stay pending
  - verify any already-running capture finishes
  - resume processing and verify pending captures begin processing without manual per-capture retries

## Assumptions

- v1 stores only the latest state change actor and timestamp, not a full audit history.
- v1 does not ask admins for a pause reason.
- v1 embeds the control in the current dashboard instead of adding a separate `/admin/processing` route.
- v1 relies on service-level idempotency to tolerate delayed pause-era Hangfire jobs after resume rather than trying to deduplicate queued Hangfire jobs in storage.
