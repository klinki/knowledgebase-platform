# Initial Findings

## Confirmed Facts
- `Program.cs` registers Hangfire with `builder.Services.AddHangfire(...)` and `builder.Services.AddHangfireServer()`.
- After building the host, startup calls static `RecurringJob.AddOrUpdate<IInsightClusteringService>(...)`.
- The exception message explicitly instructs using service-based APIs like `IRecurringJobManager` for .NET applications.

## Likely Cause
- The worker mixes DI-based Hangfire configuration with a static recurring job API that depends on `JobStorage.Current`.

## Unknowns
- None needed for the fix.

## Reproduction Status
- Reproduced by code inspection against the provided stack trace.

## Evidence Gathered
- `backend/src/SentinelKnowledgebase.Worker/Program.cs`
- Worker container stack trace from 2026-04-11
