# Feature: Dedicated Search Page (`21-search-page`)

## Summary

- Artifact type: `feature-spec.md`
- Target path: `docs/features/21-search-page/feature-spec.md`
- Add a dedicated authenticated route at `/search` as the primary knowledgebase search surface.
- Move search out of the dashboard so the dashboard returns to overview-only content.
- Keep the existing exact-pair label search on the Labels page as a secondary workflow.
- Build the new page around one combined search form that supports free-text query, exact tag filters, and exact label-pair filters, with full URL-backed state.

## Key Changes

- Frontend navigation:
  - Add a `Search` item to the authenticated shell sidebar.
  - Add `/search` to the shell child routes.
  - Remove the dashboard search box and dashboard inline search-result mode.
- Search page behavior:
  - One form with:
    - free-text query input
    - multi-tag selector
    - repeatable category/value label rows
    - `tagMatchMode` toggle: `any` or `all` (default `any`)
    - `labelMatchMode` toggle: `any` or `all` (default `all`)
    - explicit submit action; do not auto-run while typing
    - advanced tag and label filters collapse behind a toggle while the semantic query stays visible
  - Disable submit until at least one criterion is present.
  - Hydrate form state from URL query params on load and execute the search automatically when valid params exist.
  - Persist form state back to the URL after submit and when clearing filters.
  - Result cards link to `/captures/:id` and show title, summary, source URL, tags, labels, and optional similarity when text search was used.
- Dashboard behavior:
  - Keep recent captures, trending tags, stats, and admin processing controls.
  - Remove search-specific state and UI from the dashboard component.
- Labels behavior:
  - Keep the existing label-page exact search intact; it is not removed by this feature.

## Public Interfaces

- New frontend route:
  - `/search`
- New backend endpoint:
  - `POST /api/v1/search`
- New request DTO:
  - `SearchRequestDto`
  - `query: string | null`
  - `tags: string[]`
  - `tagMatchMode: 'any' | 'all'`
  - `labels: LabelAssignmentDto[]`
  - `labelMatchMode: 'any' | 'all'`
  - `page: number` default `1`
  - `pageSize: number` default `20`
  - `threshold: number` default `0.3`
- New response DTO:
  - `SearchResultPageDto`
  - `items: SearchResultDto[]`
  - `totalCount: number`
  - `page: number`
  - `pageSize: number`
- Paged item DTO:
  - `SearchResultDto`
  - `id`
  - `title`
  - `summary`
  - `sourceUrl`
  - `processedAt`
  - `tags`
  - `labels`
  - `similarity: number | null`
- Search execution rules:
  - When `query` is present, semantic matching is required and results are filtered further by tags and labels if supplied.
  - When `query` is absent, tags/labels-only searches are allowed.
  - Sort by `similarity desc` when `query` is present; otherwise sort by `processedAt desc`.
  - Apply paging after filtering and sorting.
  - Return `400` when no usable criteria are provided.
- URL state:
  - `q=<text>`
  - repeated `tag=<value>`
  - repeated `label=<encodedCategory>::<encodedValue>`
  - `tagMode=any|all`
  - `labelMode=any|all`
  - `page=<number>`
  - `pageSize=<number>`

## Implementation Notes

- Extend the current frontend search state into a dedicated page-oriented search service instead of keeping semantic-only logic.
- Reuse existing tag and label catalog-loading services or their backend endpoints to power typeahead/suggestions in the combined form.
- Keep existing `POST /api/v1/search/semantic`, `/tags`, and `/labels` endpoints for compatibility; the new page should use the new combined endpoint.
- Backend search service should add a combined query path rather than merging separate endpoint results in Angular.

## Test Plan

- Frontend route spec includes `/search` in the authenticated shell.
- Search page unit tests cover:
  - URL hydration on initial load
  - URL hydration for `page` and `pageSize`
  - submit disabled with no criteria
  - query-param updates after submit and clear
  - page-size changes reset back to page `1`
  - pagination controls keep the current criteria and update the page
  - correct request payload for text-only, tags-only, labels-only, and mixed searches
  - result rendering with and without similarity
  - empty and error states
- Dashboard tests confirm search UI is removed and overview content still loads.
- Backend tests cover:
  - text-only semantic search
  - tags-only search with `any` and `all`
  - labels-only search with `any` and `all`
  - mixed criteria intersection behavior
  - sort order with and without text query
  - total-count and page-slice behavior
  - `400` on empty request

## Assumptions and Defaults

- Feature slug is `21-search-page`.
- `/search` is authenticated and lives inside the existing shell.
- The dedicated page is the primary search entrypoint, but the Labels page keeps its current exact-pair search for now.
- Pagination is included with page-size options `20`, `50`, and `100`.
- No result editing, bulk actions, saved searches, or advanced ranking controls are included in v1.
- The page searches existing indexed knowledge only; this feature does not add new ingestion or indexing behavior.
