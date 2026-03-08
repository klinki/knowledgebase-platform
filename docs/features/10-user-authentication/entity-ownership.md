# Entity Ownership Plan

## Summary

Authentication now exists, but the knowledge model is still effectively global.
Captured content, processed insights, search results, dashboard data, and tags
are not yet scoped to the authenticated user who owns them.

This document defines the ownership model for feature
[10-user-authentication](feature-spec.md). The implementation is now in place,
with strict owner scoping by default:

- knowledge data is visible only to its owner
- admins keep auth and user-management powers, but not cross-user knowledge
  access
- tags are per-user rather than global
- existing ownerless knowledge data may be discarded in local/dev environments
  instead of requiring a production-grade backfill plan

## Ownership Model

### Explicitly owned entities

- `RawCapture`
  Requires `OwnerUserId`.
  The EF relationship to the owning user is configured in
  [ApplicationDbContext](../../../backend/src/SentinelKnowledgebase.Infrastructure/Data/ApplicationDbContext.cs).
- `ProcessedInsight`
  Requires `OwnerUserId`.
  The owner must always match the source raw capture owner.
- `Tag`
  Requires `OwnerUserId`.
  Tag names become unique per user rather than globally unique.

### Transitively owned entities

- `EmbeddingVector`
  Remains owned through its `ProcessedInsight`.
  Do not add a separate owner column unless later profiling proves a concrete
  need.

### Ownership semantics

- Ownership is determined from the authenticated user principal.
- Device-login capture flows create knowledge owned by the approving user.
- Foreign knowledge resources should be hidden rather than acknowledged.
  Resource-specific reads and deletes return `404 Not Found` for non-owners.
- Admin users keep admin-only auth capabilities such as invitations, password
  reset, token revocation, and Hangfire access.
  They do not gain cross-user capture/search/dashboard visibility in v1.

## Implemented Backend Changes

### Persistence and schema

- Added required owner columns and foreign keys to `RawCapture`,
  `ProcessedInsight`, and `Tag`.
- Updated EF model configuration to define those relationships.
- Replaced the global unique tag-name constraint with a per-user unique
  constraint on `(OwnerUserId, Name)`.
- Added indexes to support owner-scoped reads for:
  - recent captures
  - dashboard counts
  - tag summaries
  - semantic search
  - tag search

### Write path changes

- `POST /api/v1/capture` derives the owner from the authenticated user and
  stamps the created `RawCapture`.
- Capture processing keeps the original capture owner and stamps the derived
  `ProcessedInsight` with the same owner.
- Tag lookup/creation is user-scoped so the same tag name may exist for
  different users without collision.

### Read path changes

- `GET /api/v1/capture/{id}` and `DELETE /api/v1/capture/{id}` are
  owner-scoped and return `404` for foreign resources.
- Dashboard overview and tag summaries return only the signed-in user's data.
- Semantic search and tag search return only owner-matching insights.
- Repository and service interfaces are explicitly user-scoped rather
  than relying on global queries.

### Controller and service direction

- Controllers extract `userId` from claims and pass it into service methods.
- Capture, dashboard, and search services accept the current user identifier as
  part of their read/write operations.
- Repository methods for reads, counts, summaries, and searches are
  ownership-aware instead of globally querying all rows.

## Interface and Behavior Notes

Public route shapes remain unchanged:

- `POST /api/v1/capture`
- `GET /api/v1/capture/{id}`
- `DELETE /api/v1/capture/{id}`
- `GET /api/v1/dashboard/overview`
- `GET /api/v1/tags`
- `POST /api/v1/search/semantic`
- `POST /api/v1/search/tags`

Behavior changes to lock in:

- capture ownership is assigned automatically from the authenticated user
- foreign resource access returns `404 Not Found`
- dashboard and search responses are owner-scoped
- two different users may use the same tag name
- one user may not create duplicate tags with the same trimmed tag name

## Test Plan

- Creating a capture as user A persists ownership on the raw capture and any
  derived processed insight.
- The derived embedding remains reachable only through an owner-matching
  processed insight.
- User A cannot read or delete user B's capture by ID and receives `404`.
- Dashboard overview for user A excludes user B's captures and tags.
- Semantic search for user A excludes user B's insights.
- Tag search for user A excludes user B's insights and tags.
- Two different users can each create the same tag name without conflict.
- A single user cannot create duplicate tags with the same trimmed tag name.
- Device-login capture flow still creates data owned by the approving user.
- Admin-only auth endpoints still behave as admin-only, while knowledge queries
  remain owner-scoped.

## Migration and Rollout Assumptions

- Existing ownerless knowledge data is cleared by the ownership migration
  before the new foreign keys are applied.
- This plan does not require a production backfill strategy for prior data.
- [docs/ENTITY-MODEL.md](../../ENTITY-MODEL.md) is updated to reflect explicit
  ownership on knowledge entities.
