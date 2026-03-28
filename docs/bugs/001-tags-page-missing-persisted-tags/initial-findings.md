# Initial Findings

## Confirmed Facts
- `frontend/src/app/features/tags/tags.component.ts` calls `tagsState.loadTags()` on init and renders `tagsState.tags()` directly.
- `frontend/src/app/core/services/tags-state.service.ts` fetches `GET /api/v1/tags` and stores the returned array in memory.
- `backend/src/SentinelKnowledgebase.Api/Controllers/TagsController.cs` serves the same `GET /api/v1/tags` route.
- `backend/src/SentinelKnowledgebase.Infrastructure/Repositories/TagRepository.cs` filters tag summaries with `Where(tag => tag.Count > 0)`.
- `POST /api/v1/tags` returns a `TagSummaryDto` with `Count = 0`, so the frontend can show the new tag until reload.

## Likely Cause
The list endpoint excludes zero-count tags, so tags created directly from the tags page disappear after refresh unless they have already been attached to a capture.

## Reproduction Status
Confirmed from code inspection and supported by existing integration tests:
- created tags are returned with count 0
- the listing query hides count 0 records

## Unknowns
- Whether the dashboard should keep its current "used tags only" behavior.
- Whether the tags page should use a different endpoint from dashboard tag summaries.

## Evidence Gathered
- `backend/src/SentinelKnowledgebase.Infrastructure/Repositories/TagRepository.cs`
- `backend/src/SentinelKnowledgebase.Api/Controllers/TagsController.cs`
- `frontend/src/app/core/services/tags-state.service.ts`
- `frontend/src/app/features/tags/tags.component.ts`
