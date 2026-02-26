# Backend Code Review Fix Plan

## Problem Summary
The backend review identified four production-impacting issues:
1. Repository methods commit changes directly, breaking Unit of Work transaction boundaries.
2. Capture processing uses fire-and-forget `Task.Run`, risking disposed scoped dependencies.
3. Search logic performs in-memory semantic and tag filtering instead of database-side execution.
4. The capture POST endpoint advertises one response contract but returns a different payload shape.

## Implementation Approach
Apply fixes in small, isolated tasks so each change can be validated and committed independently:
- Restore proper Unit of Work behavior by removing repository-level `SaveChangesAsync()` calls.
- Replace fire-and-forget processing with a queued background worker using `Channel<Guid>` + `BackgroundService` + scoped resolution.
- Move semantic and tag filtering work into EF Core/PostgreSQL queries, using pgvector operators for similarity ranking.
- Align API response metadata and payload by returning a typed accepted-response DTO.
- Update affected tests and run backend build/tests after changes.

## TODO Checklist
- [ ] **Task 1: Fix Unit of Work repository behavior**
  - Remove `_context.SaveChangesAsync()` calls from repository `AddAsync` / `UpdateAsync` / `DeleteAsync` methods.
  - Keep transaction commits centralized through `_unitOfWork.SaveChangesAsync()` in application services.

- [ ] **Task 2: Replace unsafe background processing**
  - Add an in-memory capture processing queue abstraction backed by `Channel<Guid>`.
  - Add a hosted background service that dequeues capture IDs, creates a new DI scope, and processes captures safely.
  - Update capture creation flow to enqueue work instead of `Task.Run`.

- [ ] **Task 3: Move search operations to the database**
  - Refactor semantic search to compute similarity in PostgreSQL/pgvector and only return top-k threshold matches.
  - Refactor tag search to perform filtering in SQL (including `MatchAll` behavior) instead of loading all rows in memory.
  - Ensure necessary pgvector EF integration is configured in runtime DI.

- [ ] **Task 4: Fix capture API response contract**
  - Introduce a typed DTO for accepted capture responses.
  - Update `CaptureController` response annotations and returned payload to match Swagger/OpenAPI output.
  - Update integration/unit tests that assert capture POST response payload shape.

- [ ] **Task 5: Validate and finalize**
  - Run backend build and relevant tests.
  - Address only regressions introduced by these fixes.
  - Confirm each task is committed separately.
