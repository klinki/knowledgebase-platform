# Fix Attempt 001

## Attempt Status
- awaiting_user_confirmation

## Goal
- Replace the frontend-only capture list controls with a server-driven list API that supports sorting, filtering, pagination, and total-count reporting.

## Relation To Previous Attempts
- First attempt for this bug.

## Proposed Change
- Add a paged capture list DTO and query parameters to `GET /api/v1/capture`.
- Implement owner-scoped filtering, sorting, and pagination in the backend repository/service/controller path.
- Update `CaptureStateService` to request the current page from the API instead of slicing a full array locally.
- Update backend and frontend tests to validate the new list contract.

## Risks
- Frontend pagination state may drift if the server response shape and local computed state are not updated together.
- Existing tests that assume a plain array list response will fail until aligned with the new contract.

## Files And Components
- backend capture DTOs, service interface, service, repository, and controller
- frontend capture state service and capture models
- backend integration/unit tests and frontend state tests

## Verification Plan
- Run backend unit tests.
- Run backend integration tests.
- Run frontend tests.
- Build backend API and frontend app.

## Implementation Summary
- Added a new owner-scoped `GET /api/v1/capture/list` backend endpoint with `page`, `pageSize`, `sortField`, `sortDirection`, `contentType`, and `status` query support.
- Kept the existing `GET /api/v1/capture` array response intact so existing non-UI consumers keep working.
- Added backend DTOs, service contract, repository query objects, and EF query logic for server-side filtering, sorting, pagination, and total-count reporting.
- Moved the Angular capture list state service to request paged list data from the backend instead of computing filtered and sorted slices from a full local array.
- Added frontend and backend tests that validate query parameter handling, owner scoping, invalid-filter rejection, and state-service request behavior.

## Test Results
- `dotnet test backend/tests/SentinelKnowledgebase.UnitTests/SentinelKnowledgebase.UnitTests.csproj`
- `dotnet build backend/src/SentinelKnowledgebase.Api/SentinelKnowledgebase.Api.csproj`
- `dotnet test backend/tests/SentinelKnowledgebase.IntegrationTests/SentinelKnowledgebase.IntegrationTests.csproj --no-build`
- `npm test -- --watch=false`
- `npm run build`
- Frontend production build still reports pre-existing style budget warnings for `labels.component.ts` and `dashboard.component.ts`.

## Outcome
- Local verification passed. The bug appears resolved in code and tests, pending user confirmation in the target workflow.

## Next Step
- Ask the user to confirm the captures page now behaves correctly against backend-driven sorting, filtering, and pagination.

## Remaining Gaps
- User confirmation is still required before marking the bug fixed.
