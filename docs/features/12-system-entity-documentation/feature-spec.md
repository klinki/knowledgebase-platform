# Feature: System Entity Documentation (12-system-entity-documentation)

## Goal

Add a canonical, engineering-facing document that explains the implemented
Sentinel entities, lifecycle states, and end-to-end data flows.

## Acceptance Criteria

- [x] A dedicated `docs/ENTITY-MODEL.md` document exists as the primary source
      of truth for implemented system entities.
- [x] The document covers both current entity clusters:
      knowledge ingestion/retrieval and authentication/session management.
- [x] The document includes an entity catalog, relationship model, lifecycle
      states, and end-to-end flow diagrams.
- [x] Lifecycle descriptions are grounded in implemented enums or persisted
      timestamp/flag fields already present in code.
- [x] Each Mermaid diagram is paired with readable Markdown summary text.
- [x] `docs/ARCHITECTURE.md` links to the entity model as a key documentation
      entry point.
- [x] `README.md` links to the entity model so repo visitors can find it
      quickly.

## Architecture Notes

- This feature adds documentation only and does not change runtime behavior.
- The entity model is intentionally limited to implemented behavior and excludes
  backlog concepts such as folder organization and export.
- Authentication entities are part of the documented system model because they
  participate directly in user-facing and extension-facing flows.
- Current code and accepted feature specs take precedence over older
  speculative architecture notes.

## Important Interfaces and Behavior

### Documentation entry points

- `docs/ENTITY-MODEL.md`
- `docs/ARCHITECTURE.md`
- `README.md`

### Covered entity groups

- Knowledge: `RawCapture`, `ProcessedInsight`, `EmbeddingVector`, `Tag`
- Authentication: `ApplicationUser`, `UserInvitation`, `DeviceAuthorization`,
  `RefreshToken`

## Implementation Status

- [x] Create the canonical entity-model document.
- [x] Document relationships across the implemented persisted entities.
- [x] Document lifecycle states for capture processing, invitations, device
      authorization, and refresh tokens.
- [x] Document the main ingestion, processing, retrieval, and authentication
      flows.
- [x] Link the new document from architecture and README entry points.

## Verification Plan

- [x] Confirm every documented entity exists in the current codebase.
- [x] Confirm every documented lifecycle state is backed by code-level fields or
      enums.
- [x] Confirm diagrams reflect the current API, worker, and persistence
      boundaries.
- [x] Confirm Markdown links resolve from `README.md` and `docs/ARCHITECTURE.md`.

## Assumptions and Defaults

- This first pass optimizes for engineer onboarding.
- Mermaid is acceptable for repo-hosted documentation.
- `docs/STATUS.md` remains unchanged because this is a documentation feature,
  not a new tracked delivery milestone.
