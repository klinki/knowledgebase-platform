# Bug Description

## Title
Worker crashes on startup when registering recurring Hangfire job

## Status
- open

## Reported Symptoms
- The worker process exits during startup with `System.InvalidOperationException: Current JobStorage instance has not been initialized yet`.
- Stack trace points to static `Hangfire.RecurringJob.AddOrUpdate` usage in worker startup.

## Expected Behavior
- The worker should start successfully and register its recurring clustering job using the configured Hangfire storage.

## Actual Behavior
- Startup fails before the worker can run because it uses a static Hangfire API that requires `JobStorage.Current`.

## Reproduction Details
- Observed on 2026-04-11 from container logs.
- Failing file: `backend/src/SentinelKnowledgebase.Worker/Program.cs`

## Affected Area
- Background worker startup
- Hangfire recurring job registration

## Constraints
- Preserve the existing recurring job schedule and job id.

## Open Questions
- None for the local code fix.
