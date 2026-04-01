# Bug Description

## Title
Capture list sorting, filtering, and pagination only run in the frontend

## Status
- awaiting_user_confirmation

## Reported Symptoms
- The captures page applies sorting, filtering, and pagination only after loading the full capture list into Angular state.
- Backend `GET /api/v1/capture` still returns the full owner-scoped dataset with no query contract for list controls.

## Expected Behavior
- The captures list should apply sorting, filtering, and pagination in the backend so the frontend only requests the current page and the API remains correct for larger datasets.

## Actual Behavior
- Angular computes `filteredAndSorted`, `totalFilteredCount`, and page slicing locally from a fully loaded array.
- The backend list endpoint ignores sort, filter, page, and page-size concerns entirely.

## Reproduction Details
- Open the captures page.
- Change sort order, content-type filter, status filter, or page size.
- Observe that only the frontend state changes while the network request remains a plain `GET /api/v1/capture`.

## Affected Area
- `frontend/src/app/core/services/capture-state.service.ts`
- `backend/src/SentinelKnowledgebase.Api/Controllers/CaptureController.cs`
- backend capture repository/service list query path

## Constraints
- Keep existing capture detail and mutation endpoints intact.
- Preserve current visible captures-page behavior while moving the data work to the API.

## Open Questions
- None. The required direction is to move list controls into the backend.
