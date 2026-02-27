# Feature Specification: Hangfire Integration

## Goal
Replace the current in-memory background processing (hosted services + Channels) with Hangfire to provide persistent, reliable, and observable background job execution.

## Scope
- **Storage**: Use PostgreSQL as the persistent storage back-end for Hangfire.
- **Dashboard**: Enable the Hangfire Dashboard for monitoring jobs in the development environment.
- **Integration**: Refactor `CaptureService` to enqueue processing tasks as Hangfire background jobs.
- **Resilience**: Configure retry policies for failed capture processing tasks.

## Acceptance Criteria
- [ ] Hangfire is configured to use the existing PostgreSQL database.
- [ ] The Hangfire Dashboard is accessible at `/hangfire` (secured or dev-only).
- [ ] Capture processing is enqueued as a background job instead of using the in-memory queue.
- [ ] Jobs survive application restarts.
- [ ] Failed jobs automatically retry according to a configurable policy.
- [ ] Existing `CaptureProcessingBackgroundService` and related in-memory queue classes are retired/deleted.

## Implementation Status
### Phase 1: Infrastructure
- [x] [DONE] Add NuGet packages: `Hangfire.AspNetCore`, `Hangfire.PostgreSql`.
- [x] [DONE] Configure Hangfire services and server in `Program.cs`.
- [x] [DONE] Map Hangfire Dashboard middleware.

### Phase 2: Refactoring
- [x] [DONE] Update `CaptureController` to use `IBackgroundJobClient` for enqueuing captures.
- [x] [DONE] Ensure `CaptureService.ProcessCaptureAsync` is compatible with Hangfire execution (e.g., handles its own scopes if necessary, though Hangfire usually handles this).
- [x] [DONE] Remove `ICaptureProcessingQueue`, `CaptureProcessingQueue`, and `CaptureProcessingBackgroundService`.

### Phase 3: Verification
- [x] [DONE] Verify job persistence by stopping the app while a job is enqueued.
- [x] [DONE] Verify retry logic by simulating a transient failure in `ProcessCaptureAsync`.

## Verification Plan
- [ ] **Job Execution**: Queue a capture and verify it completes successfully via the Hangfire Dashboard.
- [ ] **Persistence**: Queue a capture, kill the API server, restart it, and verify the job still completes.
- [ ] **Dashboard**: Verify the dashboard shows real-time job status and history.
