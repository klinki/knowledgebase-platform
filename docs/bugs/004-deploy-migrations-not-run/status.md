# Bug Status

## Current State
awaiting_user_confirmation

## Active Attempt
`fix-attempt-001.md`

## Last Updated
2026-04-11 - missing migration designer restored and migration discovery verified

## Confirmation Date

## Resolution Summary
- Restored the missing `20260409093000_PreservedLanguages.Designer.cs` file so EF Core can discover the preserved-languages migration.

## Attempt History
- `fix-attempt-001.md` - harden deploy migration execution and align Bitbucket image publishing

## State Change Log
- 2026-04-11: bug opened
- 2026-04-11: clarified production symptom indicates a stale migrator image missing `20260409093000_PreservedLanguages`
- 2026-04-11: verified migrations project builds successfully with both recent migrations present
- 2026-04-11: confirmed `20260409093000_PreservedLanguages.Designer.cs` was missing from source
- 2026-04-11: restored the designer partial and verified `dotnet ef migrations list` now includes `20260409093000_PreservedLanguages`

## Notes
- `dotnet ef migrations list` still reports local database authentication failure in this environment, but migration discovery succeeds and includes `PreservedLanguages`.
