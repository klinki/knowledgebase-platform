# Fix Attempt 001

## Attempt Status
completed

## Goal
Make deployment run database migrations reliably before application services start.

## Relation To Previous Attempts
- First attempt for this bug.

## Proposed Change
- Restore the missing generated designer partial for `20260409093000_PreservedLanguages` so EF Core discovers it as a migration.

## Risks
- The restored designer must point at the current target model so it stays aligned with the snapshot.

## Files And Components
- `backend/src/SentinelKnowledgebase.Migrations/Migrations/20260409093000_PreservedLanguages.Designer.cs`

## Verification Plan
- Run `dotnet build backend\src\SentinelKnowledgebase.Migrations\SentinelKnowledgebase.Migrations.csproj`
- Run `dotnet ef migrations list --verbose --project backend\src\SentinelKnowledgebase.Migrations\SentinelKnowledgebase.Migrations.csproj --startup-project backend\src\SentinelKnowledgebase.Migrations\SentinelKnowledgebase.Migrations.csproj`

## Implementation Summary
- Added `20260409093000_PreservedLanguages.Designer.cs` as the missing migration metadata partial.
- Implemented `BuildTargetModel` by delegating to the current `ApplicationDbContextModelSnapshot`, which represents the post-migration model.

## Test Results
- `dotnet build backend\src\SentinelKnowledgebase.Migrations\SentinelKnowledgebase.Migrations.csproj` passed.
- `dotnet ef migrations list --verbose --project backend\src\SentinelKnowledgebase.Migrations\SentinelKnowledgebase.Migrations.csproj --startup-project backend\src\SentinelKnowledgebase.Migrations\SentinelKnowledgebase.Migrations.csproj` listed `20260409093000_PreservedLanguages`.

## Outcome
- Migration discovery is repaired locally.

## Next Step
- Rebuild and redeploy the migrator image, then confirm `20260409093000_PreservedLanguages` is applied on the target database.

## Remaining Gaps
- No live production deployment is available in this environment, so verification is limited to script validation and static inspection.
