# Fix Attempt 001

## Attempt Status
in_progress

## Goal
Allow the worker to start and register the recurring clustering job without relying on `JobStorage.Current`.

## Relation To Previous Attempts
- First attempt for this bug.

## Proposed Change
- Resolve `IRecurringJobManager` from the host service provider and use it to register the recurring job.

## Risks
- Minimal; this is the Hangfire-recommended API for DI-based applications.

## Files And Components
- `backend/src/SentinelKnowledgebase.Worker/Program.cs`

## Verification Plan
- Run `dotnet build backend\src\SentinelKnowledgebase.Worker\SentinelKnowledgebase.Worker.csproj`

## Implementation Summary
- Investigation complete; patch in progress.

## Test Results
- Pending.

## Outcome
- Awaiting implementation and verification.

## Next Step
- Patch worker startup and build the worker project.

## Remaining Gaps
- No live container run is available in this environment, so verification is limited to compile-time validation.
