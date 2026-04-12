# Fix Attempt 001

## Attempt Status
completed

## Goal
Make search result links open the correct capture detail page.

## Relation To Previous Attempts
- First attempt for this bug.

## Proposed Change
- Add `captureId` to the combined search result DTO and repository projection.
- Keep the existing `id` field for the processed insight id so other consumers do not break.
- Update the search frontend to route using `captureId`.
- Add regression coverage in frontend and backend tests.

## Risks
- Minimal schema risk because this is a response contract change only.
- Existing search consumers that deserialize the response strictly may need to ignore the new field, but the current contract is backward compatible.

## Files And Components
- `backend/src/SentinelKnowledgebase.Infrastructure/Repositories/IRepositories.cs`
- `backend/src/SentinelKnowledgebase.Infrastructure/Repositories/ProcessedInsightRepository.cs`
- `backend/src/SentinelKnowledgebase.Application/DTOs/Search/SearchDto.cs`
- `backend/src/SentinelKnowledgebase.Application/Services/SearchService.cs`
- `frontend/src/app/shared/models/knowledge.model.ts`
- `frontend/src/app/core/services/search-state.service.ts`
- `frontend/src/app/features/search/search.component.html`
- `frontend/src/app/features/search/search.component.spec.ts`
- `backend/tests/SentinelKnowledgebase.IntegrationTests/SearchControllerTests.cs`

## Verification Plan
- Run focused frontend and backend tests covering the search contract and search page link.

## Implementation Summary
- Added `captureId` to the combined search response contract on the backend and threaded it through the Angular search state.
- Updated the search result card to route to `/captures/{captureId}` instead of the processed insight id.
- Added regression coverage for the search-state normalization, search component routing, and backend search controller response.

## Test Results
- `npm test -- --watch=false --include src/app/core/services/search-state.service.spec.ts --include src/app/features/search/search.component.spec.ts`
- `dotnet test backend/tests/SentinelKnowledgebase.IntegrationTests/SentinelKnowledgebase.IntegrationTests.csproj --filter FullyQualifiedName~SearchControllerTests`

## Outcome
- Local verification passed. Awaiting user confirmation in the real search flow.

## Next Step
- Ask the user to confirm the search result links now open the correct capture detail page.

## Remaining Gaps
- No known code gaps. Final confirmation still depends on the user's environment and data.
