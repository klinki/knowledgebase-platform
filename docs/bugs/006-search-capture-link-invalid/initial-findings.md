# Initial Findings

## Confirmed Facts
- `frontend/src/app/features/search/search.component.html` links each result with `[routerLink]="['/captures', result.id]"`.
- `frontend/src/app/core/services/search-state.service.ts` normalizes the combined search response into `SearchResult` objects without any capture-specific id.
- `backend/src/SentinelKnowledgebase.Application/Services/SearchService.cs` maps combined search results from `SearchRecord.Id`.
- `backend/src/SentinelKnowledgebase.Infrastructure/Repositories/ProcessedInsightRepository.cs` projects combined search results from `ProcessedInsight.Id`, while the underlying row also has `RawCaptureId`.

## Likely Cause
- The combined search endpoint exposes the processed insight id as `id`, and the frontend assumes that field is the capture id when building the capture detail link.

## Unknowns
- Whether any other search consumers expect `id` to remain the processed insight id.

## Reproduction Status
- Reproduced by code inspection.

## Evidence Gathered
- `frontend/src/app/features/search/search.component.html`
- `frontend/src/app/core/services/search-state.service.ts`
- `backend/src/SentinelKnowledgebase.Application/Services/SearchService.cs`
- `backend/src/SentinelKnowledgebase.Infrastructure/Repositories/ProcessedInsightRepository.cs`
