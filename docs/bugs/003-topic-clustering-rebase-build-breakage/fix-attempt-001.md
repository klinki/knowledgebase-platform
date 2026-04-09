# Fix Attempt 001

## Attempt Status
completed

## Goal
Restore a passing build and test baseline for the rebased `codex/topic-clustering` branch.

## Relation To Previous Attempts
- First attempt for this bug.

## Proposed Change
- Re-home shared language abstractions so both `Application` and `Infrastructure` can use them without a project-reference cycle.
- Re-run backend and frontend verification and repair any stale tests exposed by the rebase.

## Risks
- Additional rebased tests could still fail after the compile issue is fixed.

## Files And Components
- `backend/src/SentinelKnowledgebase.Domain/Localization/LanguageCatalog.cs`
- `backend/src/SentinelKnowledgebase.Domain/Services/IUserLanguagePreferencesService.cs`
- `backend/src/SentinelKnowledgebase.Api/Controllers/AuthController.cs`
- `backend/src/SentinelKnowledgebase.Application/Services/CaptureService.cs`
- `backend/src/SentinelKnowledgebase.Application/Services/ContentProcessor.cs`
- `backend/src/SentinelKnowledgebase.Infrastructure/Authentication/UserLanguagePreferencesService.cs`
- `backend/src/SentinelKnowledgebase.Infrastructure/DependencyInjection.cs`
- `backend/tests/SentinelKnowledgebase.IntegrationTests/AuthControllerTests.cs`
- `backend/tests/SentinelKnowledgebase.IntegrationTests/CaptureLanguageProcessingTests.cs`
- `backend/tests/SentinelKnowledgebase.UnitTests/CaptureServiceTests.cs`
- `frontend/src/app/features/dashboard/dashboard.component.spec.ts`
- `frontend/src/app/features/settings/settings.component.spec.ts`

## Verification Plan
- Run `dotnet test backend\SentinelKnowledgebase.slnx`
- Run `npm test -- --watch=false` in `frontend`
- Run `npm run build` in `frontend`

## Implementation Summary
- Reverted the attempted direct infrastructure-to-application reference after it produced a restore cycle.
- Moved `LanguageCatalog` and `IUserLanguagePreferencesService` into the `Domain` project and updated consumers to use the new namespaces.
- Reset the bootstrap admin language state inside the auth integration test so the `Accept-Language` scenario is isolated from earlier fixture mutations.
- Fixed stale frontend specs introduced by the branch changes: one missing `SearchStateService` import and one outdated preserved-language expectation.

## Test Results
- Initial failure: `dotnet test backend\SentinelKnowledgebase.slnx` failed to compile because `SentinelKnowledgebase.Infrastructure` used `Application` symbols without a legal dependency path.
- Intermediate failure: adding a direct project reference created an MSBuild restore cycle.
- Final verification: `dotnet test backend\SentinelKnowledgebase.slnx` passed.
- Final verification: `npm test -- --watch=false` passed in `frontend`.
- Final verification: `npm run build` passed in `frontend`.

## Outcome
- Local build and test failures are resolved in the topic-clustering worktree.

## Next Step
- Ask the user to confirm the branch behaves correctly in their environment or CI.

## Remaining Gaps
- Existing dependency vulnerability warnings remain for `Newtonsoft.Json` 11.0.1 and were not changed as part of this repair.
