# Initial Findings

## Confirmed Facts
- The migrations project currently contains both `20260409081640_TopicClustering` and `20260409093000_PreservedLanguages`, and builds successfully.
- The reported production symptom is that only `20260409081640_TopicClustering` was applied.
- The migrations folder contained `20260409093000_PreservedLanguages.cs` but did not contain `20260409093000_PreservedLanguages.Designer.cs`.
- After restoring the missing designer file, `dotnet ef migrations list` includes `20260409093000_PreservedLanguages`.

## Likely Cause
- EF Core migration discovery was incomplete because the `PreservedLanguages` migration was missing its generated designer partial class with migration metadata.

## Unknowns
- Whether the observed production issue came from a Postgres readiness race, a missing migrator image, or both.

## Reproduction Status
- Reproduced by code inspection of the deployment path.

## Evidence Gathered
- `dotnet build backend\src\SentinelKnowledgebase.Migrations\SentinelKnowledgebase.Migrations.csproj`
- `dotnet ef migrations list --verbose --project backend\src\SentinelKnowledgebase.Migrations\SentinelKnowledgebase.Migrations.csproj --startup-project backend\src\SentinelKnowledgebase.Migrations\SentinelKnowledgebase.Migrations.csproj`
