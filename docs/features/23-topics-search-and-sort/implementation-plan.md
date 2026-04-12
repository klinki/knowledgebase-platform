# Topics Site Search and Sorting

## Summary

- **Feature slug:** `23-topics-search-and-sort`
- **Artifact type:** `implementation-plan.md`
- **Target doc path:** `docs/features/23-topics-search-and-sort/implementation-plan.md`
- Extend the existing paginated topics list so users can:
  - search topics by metadata
  - sort topics by insight count, updated time, or title
  - keep the current list state in the `/topics` URL
- Keep this enhancement scoped to the topics index page and `GET /api/v1/clusters/list`; do not change topic detail, dashboard topic cards, or the separate `/search` page.

## Key Changes

### Backend

- Extend `TopicClusterListQueryDto` and `TopicClusterQueryOptions` with:
  - `Query: string?`
  - `SortField: string` with allowed values `memberCount`, `updatedAt`, `title`
  - `SortDirection: string` with allowed values `asc`, `desc`
- Keep defaults as:
  - `Page = 1`
  - `PageSize = 12`
  - `SortField = memberCount`
  - `SortDirection = desc`
- Update `InsightClusteringService.GetClusterListPageAsync` to normalize and validate the new query values before calling the repository.
- Update `InsightClusterRepository.GetPagedAsync` to:
  - apply owner scoping first
  - apply case-insensitive partial-match filtering on `Title`, `Description`, and `KeywordsJson`
  - compute `TotalCount` after filtering and before pagination
  - apply requested ordering with deterministic tie-breakers so pagination stays stable
- Use stable secondary ordering:
  - `memberCount`: requested direction, then `UpdatedAt desc`, then `Title asc`
  - `updatedAt`: requested direction, then `MemberCount desc`, then `Title asc`
  - `title`: requested direction, then `MemberCount desc`, then `UpdatedAt desc`
- Leave `GET /api/v1/clusters` unchanged; only `/list` becomes queryable/sortable.

### Frontend

- Add topics-list criteria state for:
  - `query`
  - `sortField`
  - `sortDirection`
  - `page`
- Keep `pageSize` fixed at `12`; do not add a page-size control in this change.
- Update `TopicsStateService.loadTopicsPage` to accept criteria instead of just page/pageSize and send `query`, `sortField`, and `sortDirection` to `/v1/clusters/list`.
- Add query-param handling on `/topics` using `ActivatedRoute` and `Router`:
  - URL params: `q`, `sortField`, `sortDirection`, `page`
  - invalid or missing params normalize back to defaults with `replaceUrl`
  - changing search or sort resets `page` to `1`
- Add a compact controls row above the grid:
  - search input with submit-on-enter/button behavior
  - sort dropdown with explicit user-facing options:
    - `Most insights`
    - `Fewest insights`
    - `Recently updated`
    - `Oldest updated`
    - `Title A–Z`
    - `Title Z–A`
  - clear action that removes `q` and resets to default sort and page 1
- Keep the overview card copy aligned with active sort:
  - default copy remains insight-count focused
  - non-default sort uses neutral copy instead of claiming “sorted by cluster size”
- Add a filtered empty state when no topics match the current search, distinct from the existing “No topics yet” empty state.

## Public APIs and Types

- Backend request contract for `GET /api/v1/clusters/list` adds optional query parameters:
  - `query`
  - `sortField`
  - `sortDirection`
- Backend response shape `TopicClusterListPageDto` stays unchanged.
- Frontend adds explicit topic list query types/enums mirroring the backend sort fields and directions.
- Browser URL contract for `/topics` becomes:
  - `q=<search text>`
  - `sortField=<memberCount|updatedAt|title>`
  - `sortDirection=<asc|desc>`
  - `page=<n>`

## Test Plan

- Backend integration tests for `/api/v1/clusters/list`:
  - search matches title
  - search matches description
  - search matches keywords
  - search is owner-scoped
  - sort by `memberCount`, `updatedAt`, and `title`
  - filtered `TotalCount` and pagination remain correct
- Frontend service tests:
  - request URL includes normalized query and sort params
  - default criteria omit or normalize URL params correctly
  - invalid URL params fall back to defaults
- Frontend component tests:
  - controls render with default sort selected
  - submitting search updates router state and reloads data
  - changing sort resets to page 1
  - filtered empty state renders for zero matches
- Verification:
  - frontend unit tests
  - backend integration tests
  - frontend build

## Assumptions and Defaults

- Search is metadata-only in v1: `title`, `description`, and `keywords`; it does not search member insight titles or summaries.
- URL sync includes `page` because list state should survive refresh/back navigation; `pageSize` stays fixed and is not exposed.
- Search is submit-based, not debounced live-search.
- Default sort remains `memberCount desc`, matching current behavior.
