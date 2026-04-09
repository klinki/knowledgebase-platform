# Bug Description

## Title
Topic clustering branch fails backend build after rebase

## Status
- open

## Reported Symptoms
- `dotnet test backend\SentinelKnowledgebase.slnx` fails during compilation in the rebased `codex/topic-clustering` worktree.
- `SentinelKnowledgebase.Infrastructure` cannot resolve `SentinelKnowledgebase.Application` namespaces and types used by `UserLanguagePreferencesService`.

## Expected Behavior
- The rebased branch should build and run its test suites successfully in its dedicated worktree.

## Actual Behavior
- Backend compilation stops before tests execute because the infrastructure project is missing required application-layer types.

## Reproduction Details
- Worktree: `C:\ai-workspace\knowledgebase-platform-topic-clustering`
- Command: `dotnet test backend\SentinelKnowledgebase.slnx`
- First observed on: 2026-04-09

## Affected Area
- Backend build configuration
- `SentinelKnowledgebase.Infrastructure`

## Constraints
- Continue work in the existing topic-clustering worktree.
- Preserve bug-fix history for the branch repair.

## Open Questions
- Whether additional compile or test failures remain after the missing project reference is restored.
