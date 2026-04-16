# Topic Cluster Search Launch

## Summary

- **Feature slug:** `25-topic-cluster-search`
- **Artifact type:** `implementation-plan.md`
- **Target doc path:** `docs/features/25-topic-cluster-search/implementation-plan.md`
- Add direct topic-to-search handoff so users can launch `/search` scoped to a specific topic cluster.
- Extend regular search criteria with topic-cluster filtering support while preserving all existing query/tag/label/sort/pagination behavior.

## Key Changes

### Topics page UX

- Add a `Search on topic` action on each topic card, positioned under `Open topic`.
- Route target: `/search?topicId=<cluster-guid>`.
- Keep existing `Open topic` behavior unchanged.

### Search frontend state and UI

- Extend `SearchCriteria` with `topicId`.
- Parse and persist `topicId` in `/search` URL query params.
- Treat `topicId` as a valid criterion so scoped searches execute immediately on page load.
- Add topic filter input to the search form (`Topic cluster ID`) so the prefilled value is visible/editable.
- Include `topicClusterId` in the `/api/v1/search` POST payload.

### Search backend filtering

- Extend `SearchRequestDto` with `TopicClusterId`.
- Extend validation criterion checks so topic-cluster-only requests are valid.
- Extend search service and repository contracts to accept optional `topicClusterId`.
- Apply owner-scoped membership filtering in processed-insight search:
  - only insights in the requested cluster
  - still combined with existing query/tag/label filters
  - existing sorting and pagination remain deterministic.

## Public API / Contract

- `POST /api/v1/search` request adds optional:
  - `topicClusterId: Guid`
- Response shape remains unchanged.
- Frontend URL contract adds optional:
  - `topicId=<cluster-guid>`

## Test Plan

- Backend unit tests:
  - validator accepts topic-cluster-only criteria
  - search service forwards `topicClusterId` without embedding generation when query is empty.
- Backend integration tests:
  - `/api/v1/search` with only `topicClusterId` returns only insights in that topic cluster.
- Frontend tests:
  - search state parses/builds `topicId` and includes `topicClusterId` payload field.
  - search component loads with prefilled topic id and executes initial search.
  - topics page renders `Search on topic` link with `topicId` query param.

## Assumptions and Defaults

- Topic filtering is an additional constraint, not a separate endpoint.
- `topicId` must be a valid GUID; invalid values are ignored client-side.
- Existing search defaults stay unchanged:
  - query present => default `relevance desc`
  - query absent => default `processedAt desc`.
