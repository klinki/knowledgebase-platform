# Implementation Plan: Search Page Worktree (`21-search-page`)

## Summary

- Artifact type: `implementation-plan.md`
- Target path: `docs/features/21-search-page/implementation-plan.md`
- Base branch: `master` at current `HEAD`
- New branch: `codex/21-search-page`
- New worktree path: `C:\ai-workspace\knowledgebase-platform-search-page`
- Because the main worktree is already dirty, all feature work should happen in the new worktree only.

## Worktree and Documentation Setup

- Create a new git worktree from `master` on branch `codex/21-search-page` at `C:\ai-workspace\knowledgebase-platform-search-page`.
- Persist the previously approved feature spec verbatim to:
  - `docs/features/21-search-page/feature-spec.md`
- Persist this implementation plan verbatim to:
  - `docs/features/21-search-page/implementation-plan.md`
- Add the feature to `docs/STATUS.md` backlog as:
  - `Search Page (Ref: /docs/features/21-search-page/implementation-plan.md)`

## Backend Changes

- Extend [SearchDto.cs](C:/ai-workspace/knowledgebase-platform/backend/src/SentinelKnowledgebase.Application/DTOs/Search/SearchDto.cs) with:
  - `SearchRequestDto`
  - `SearchResultDto`
  - shared enum/string contract for `tagMatchMode` and `labelMatchMode`
- Extend [ISearchService.cs](C:/ai-workspace/knowledgebase-platform/backend/src/SentinelKnowledgebase.Application/Services/Interfaces/ISearchService.cs) with a combined search method:
  - `SearchAsync(Guid ownerUserId, SearchRequestDto request)`
- Add `POST /api/v1/search` to [SearchController.cs](C:/ai-workspace/knowledgebase-platform/backend/src/SentinelKnowledgebase.Api/Controllers/SearchController.cs).
- Keep existing `/semantic`, `/tags`, and `/labels` endpoints unchanged for compatibility.
- Extend [IRepositories.cs](C:/ai-workspace/knowledgebase-platform/backend/src/SentinelKnowledgebase.Infrastructure/Repositories/IRepositories.cs) and [ProcessedInsightRepository.cs](C:/ai-workspace/knowledgebase-platform/backend/src/SentinelKnowledgebase.Infrastructure/Repositories/ProcessedInsightRepository.cs) with a single combined repository query that:
  - supports optional semantic query, optional tags, and optional label pairs
  - enforces `ownerUserId`
  - treats semantic match as required when `query` is present
  - applies tag and label filters as additional constraints
  - supports `any` and `all` matching separately for tags and labels
  - returns `similarity` when semantic search is used, otherwise `null`
  - orders by `similarity desc` for semantic searches, else `processedAt desc`
  - returns paged results with `page`, `pageSize`, and `totalCount`
- Implement request normalization in [SearchService.cs](C:/ai-workspace/knowledgebase-platform/backend/src/SentinelKnowledgebase.Application/Services/SearchService.cs):
  - trim query
  - deduplicate tags case-insensitively
  - deduplicate label pairs case-insensitively
  - reject empty requests with `400`
  - generate embeddings only when `query` is present
  - normalize default `page=1` and `pageSize=20`

## Frontend Changes

- Add a new standalone search page component under `frontend/src/app/features/search/` with:
  - one combined form
  - free-text query input
  - repeatable tag chips/input
  - repeatable label category/value rows
  - tag match mode toggle (`any`/`all`)
  - label match mode toggle (`any`/`all`)
  - explicit submit and clear actions
- Add `/search` to [app.routes.ts](C:/ai-workspace/knowledgebase-platform/frontend/src/app/app.routes.ts).
- Add a `Search` nav item to [shell.component.ts](C:/ai-workspace/knowledgebase-platform/frontend/src/app/features/shell/shell.component.ts).
- Replace the current semantic-only [search-state.service.ts](C:/ai-workspace/knowledgebase-platform/frontend/src/app/core/services/search-state.service.ts) with a page-oriented combined search service that:
  - builds the new request payload
  - calls `POST /v1/search`
  - exposes results/loading/error state
  - exposes pagination and total-count state
  - parses URL query params into page state
  - writes page state back to URL on submit and clear
- Update [knowledge.model.ts](C:/ai-workspace/knowledgebase-platform/frontend/src/app/shared/models/knowledge.model.ts) with a shared search result model that includes `processedAt` and nullable `similarity`.
- Remove dashboard search UI and semantic-search mode from [dashboard.component.ts](C:/ai-workspace/knowledgebase-platform/frontend/src/app/features/dashboard/dashboard.component.ts).
- Leave the Labels page exact-pair search in place; do not migrate or remove it in this feature.
- Reuse existing tag and label catalog data for suggestions where practical; do not add new suggestion endpoints in v1.

## URL and UI Behavior

- URL query params:
  - `q=<text>`
  - repeated `tag=<value>`
  - repeated `label=<category>::<value>`
  - `tagMode=any|all`
  - `labelMode=any|all`
  - `page=<number>`
  - `pageSize=<number>`
- On page load:
  - if valid criteria exist in the URL, hydrate the form and execute the search automatically
  - otherwise render the empty pre-search state
- Submit button is disabled until at least one criterion is present.
- Clear removes all criteria, clears results, and removes search query params.
- Page and page-size changes keep current filters in the URL and trigger a fresh search.
- Omit `page` and `pageSize` from the URL when they are at defaults (`1` and `20`).
- Result cards navigate to `/captures/:id`.
- Similarity is shown only when the backend returns it.

## Test Plan

- Frontend:
  - route spec covers `/search`
  - search page tests cover URL hydration, submit payloads, page changes, page-size changes, clear behavior, loading/error/empty states, and result rendering
  - dashboard tests confirm search UI removal without regressing overview/admin behavior
  - `npm run build` and relevant Angular tests in `frontend`
- Backend:
  - extend unit tests in `SearchServiceTests`
  - extend integration tests in `SearchControllerTests`
  - cover text-only, tags-only, labels-only, mixed criteria, `any`/`all` semantics, empty-request validation, sort behavior, and paged responses
  - run targeted backend tests for search plus backend build

## Assumptions and Defaults

- Work starts from `master`, not from the dirty working tree state.
- Branch naming follows repo convention with `codex/` prefix.
- Combined search is implemented as a new backend API, not by merging separate frontend calls.
- The dashboard becomes overview-only.
- The Labels page keeps its existing exact search as a secondary surface.
- Saved searches, bulk actions, and ranking controls remain out of scope for v1.
