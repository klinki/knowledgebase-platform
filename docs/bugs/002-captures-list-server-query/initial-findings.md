# Initial Findings

## Confirmed Facts
- `CaptureStateService` currently stores the full capture list in `capturesState` and performs sorting, filtering, and pagination in computed signals.
- `CaptureController.GetAllCaptures()` currently exposes no query parameters and returns `IEnumerable<CaptureResponseDto>`.
- `ICaptureService` and `IRawCaptureRepository` do not expose a paged or filtered list query.
- Existing frontend tests still assert client-side sorting against a full-array response.

## Likely Cause
- The captures browsing feature was originally specified and implemented as client-side sorting for v1, and a later frontend-only change added filtering and pagination without adding a matching backend list contract.

## Unknowns
- None that block implementation.

## Reproduction Status
- Confirmed by code inspection and existing list-endpoint behavior.

## Evidence Gathered
- `frontend/src/app/core/services/capture-state.service.ts`
- `backend/src/SentinelKnowledgebase.Api/Controllers/CaptureController.cs`
- `backend/src/SentinelKnowledgebase.Application/Services/Interfaces/ICaptureService.cs`
- `backend/src/SentinelKnowledgebase.Infrastructure/Repositories/RawCaptureRepository.cs`
