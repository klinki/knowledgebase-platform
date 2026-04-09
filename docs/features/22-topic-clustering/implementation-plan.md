# Execution Plan: Topic Clustering in a Dedicated Worktree

## Summary

- **Feature slug:** `20-topic-clustering`
- **Artifact type:** `implementation-plan.md`
- **Target doc path:** `docs/features/20-topic-clustering/implementation-plan.md`
- Implement the previously approved topic-clustering design in a **separate git worktree** so no code or docs changes are made in the current dirty primary worktree.
- Base the worktree on current `master` commit `af97c4d` and use:
  - branch: `codex/topic-clustering`
  - worktree path: `C:\ai-workspace\knowledgebase-platform-topic-clustering`

## Worktree Setup and Execution Contract

- Once Plan Mode ends, create the isolated worktree from the current repository root with:
  - `git worktree add -b codex/topic-clustering C:\ai-workspace\knowledgebase-platform-topic-clustering af97c4d`
- Do all implementation, testing, staging, and commits only inside `C:\ai-workspace\knowledgebase-platform-topic-clustering`.
- Do not copy, restore, or reconcile any unrelated tracked or untracked files from the primary worktree.
- Persist this approved plan verbatim to `docs/features/20-topic-clustering/implementation-plan.md` in the new worktree before code changes begin.
- Keep commits atomic and path-scoped. Recommended commit split:
  1. `docs(feature): Add topic clustering implementation plan`
  2. `feat(backend): Add persisted topic clustering`
  3. `feat(frontend): Add topic discovery views`
  4. `test: Cover topic clustering flows`
- Do not modify the current `master` worktree during implementation.

## Implementation Changes

### Backend

- Add persisted cluster entities:
  - `InsightCluster`
  - `InsightClusterMembership`
- Store one optional cluster membership per `ProcessedInsight`.
- Add owner-scoped rebuild orchestration in a new clustering service.
- Rebuild clusters per owner from existing processed-insight embeddings using the approved mutual-nearest-neighbor component algorithm:
  - skip users with fewer than `6` processed insights
  - keep edges for mutual top-`5` neighbors with cosine similarity `>= 0.72`
  - persist only components of size `>= 3`
  - rank members by centroid similarity
- Generate cluster `title`, `description`, and `3` keywords from representative summaries using the existing chat-completions path, with keyword fallback if generation fails.
- Trigger `RebuildOwnerClustersAsync(ownerUserId)` after a capture finishes processing.
- Add nightly rebuild for owners whose clusters are older than `24` hours.
- Extend read models and APIs:
  - `GET /api/v1/dashboard/overview` includes `topicClusters`
  - `GET /api/v1/clusters`
  - `GET /api/v1/clusters/{id}`
  - capture detail includes optional cluster link data
- Expose a read-only suggested label shape:
  - `{ category: "Topic", value: <cluster title> }`
- Do not write cluster titles into the existing labels system in v1.

### Frontend

- Add dashboard **Topics** section showing up to `5` topic groups with title, description, member count, representative items, and suggested-label badge.
- Add topic detail route `/topics/:id` with ordered member list and standard loading/error/empty states.
- Extend capture detail to show the current topic group when present.
- Keep search, tags, and labels behavior unchanged except for optional display of topic-link data already present in the read model.
- Use “Topics” in user-facing copy and keep “cluster” naming internal to backend/service code.

### Data and compatibility

- Use the existing processed-insight summary embeddings as clustering input.
- Do not backfill existing tags or labels.
- Do not cluster raw captures.
- Insights that do not meet clustering thresholds remain unclustered.
- Full rebuilds replace prior clusters for one owner atomically.

## Test Plan

- Backend unit tests:
  - graph construction
  - threshold and minimum-size filtering
  - single-membership enforcement
  - centroid ranking
  - title-generation fallback
- Backend integration tests:
  - owner scoping
  - cluster rebuild after processing completion
  - dashboard overview topic payload
  - cluster detail member ordering
  - stale cluster replacement
- Frontend unit tests:
  - dashboard topic rendering
  - topic detail state handling
  - capture detail topic-link rendering
- Frontend e2e:
  - dashboard to topic page navigation
  - representative item navigation
  - empty-account dashboard behavior
- Verification in the isolated worktree:
  - backend tests
  - frontend tests
  - affected project builds

## Assumptions and Defaults

- The implementation starts only after Plan Mode ends.
- The isolated worktree branches from `af97c4d` on `master`, ignoring unrelated local changes in the primary worktree.
- V1 remains read-only: no “apply topic as label” action is included.
- No extra operational history table is added in v1; Hangfire, logs, and metrics are sufficient.
