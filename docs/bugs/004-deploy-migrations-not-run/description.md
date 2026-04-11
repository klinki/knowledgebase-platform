# Bug Description

## Title
Production deployment does not reliably execute database migrations

## Status
- open

## Reported Symptoms
- After deployment, pending EF Core migrations are not consistently applied automatically.
- The production compose stack expects a `migrator` image, but not every CI pipeline publishes it.
- Observed deployment applied `20260409081640_TopicClustering` but not `20260409093000_PreservedLanguages`.

## Expected Behavior
- Every deployment should wait for PostgreSQL readiness and then execute the latest migration bundle before starting the API and worker.

## Actual Behavior
- The latest migration source file existed, but `20260409093000_PreservedLanguages.Designer.cs` was missing from the migrations project.
- Without the designer partial class and migration metadata, EF Core did not discover `PreservedLanguages` as a concrete migration in the bundle.

## Reproduction Details
- Deployment path inspected on 2026-04-11.
- Files reviewed:
  - `deploy/scripts/deploy.sh`
  - `deploy/docker-compose.prod.yml`
  - `.github/workflows/deploy.yml`
  - `bitbucket-pipelines.yml`

## Affected Area
- Production deployment orchestration
- CI image publishing for deployment

## Constraints
- Keep the production rollout order: database first, then migrations, then app services.
- Avoid breaking existing GitHub Actions deployment behavior.

## Open Questions
- Whether deployments currently run primarily through GitHub Actions, Bitbucket Pipelines, or both in active environments.
