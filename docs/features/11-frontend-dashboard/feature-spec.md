# Feature: Frontend Dashboard (11-frontend-dashboard)

## Goal

Replace stubbed Angular dashboard and tags state with backend-driven state and
clear signals-based read models for dashboard, search, and tag views.

This feature depends on the frontend auth/session foundation owned by
`10-user-authentication`.

## Acceptance Criteria

- [x] Dashboard no longer depends on hardcoded local seed data.
- [x] Tags page no longer depends on hardcoded local seed data.
- [x] Dashboard overview loads recent captures, top tags, and summary stats from
      backend read models.
- [x] Semantic search results replace overview content while a query is active.
- [x] Clearing the search query restores dashboard overview state.
- [x] Tags page loads backend tag summaries through a dedicated endpoint.
- [x] Dashboard and tags pages use explicit loading, empty, and error states.
- [x] No fake local fallback data is shown when backend requests fail.
- [x] Angular frontend keeps using signals-based services instead of adding a
      store library.

## Architecture Notes

- Auth and session state remain owned by `10-user-authentication`.
- Angular should use domain-focused signals services for dashboard, search, and
  tags read state.
- Components should keep only local UI inputs; shared async data state belongs
  in services.
- Small backend API additions are allowed to support frontend read models.
- `docs/templates/feature-spec.md` is currently missing; this feature spec
  follows the existing repository pattern.

## Important Interfaces and Behavior

### Backend API additions

- `GET /api/v1/dashboard/overview`
- `GET /api/v1/tags`

### Read-model DTO planning

- `DashboardOverviewDto`
  - `recentCaptures: CaptureListItemDto[]`
  - `topTags: TagSummaryDto[]`
  - `stats: DashboardStatsDto`
- `CaptureListItemDto`
  - `id`
  - `title`
  - `sourceUrl`
  - `capturedAt`
  - `status`
  - `tags`
- `TagSummaryDto`
  - `id`
  - `name`
  - `count`
  - `lastUsedAt`
- `DashboardStatsDto`
  - `totalCaptures`
  - `activeTags`

### Frontend behavior

- Dashboard overview is the default state when no search query is active.
- Semantic search uses the existing authenticated search API and maps results to
  dashboard-ready list presentation.
- Tags page renders backend tag summaries without placeholder local data.
- Error states are explicit UI states, not silent fallback to fake content.

## Implementation Status

- [x] Replace hardcoded `KnowledgeService` seed data.
- [x] Split read state into focused signals services for dashboard, search, and
      tags.
- [x] Add dashboard overview backend endpoint and DTOs.
- [x] Add tags summary backend endpoint and DTOs.
- [x] Update dashboard component to load overview state and switch to semantic
      search results on query.
- [x] Update tags component to load backend tag summaries.
- [x] Add explicit loading, empty, and error states to dashboard and tags pages.
- [x] Update frontend tests and Playwright coverage for backend-driven state.

## Verification Plan

- [x] Dashboard initial load renders backend overview data.
- [x] Dashboard search shows semantic search results for active queries.
- [x] Clearing the query restores overview content.
- [x] Tags page renders backend tag summaries.
- [x] Backend failures show explicit error states.
- [x] Empty backend responses show explicit empty states.
- [x] Playwright coverage no longer depends on hardcoded initial dashboard data.

## Assumptions and Defaults

- Feature folder name is `11-frontend-dashboard`.
- Angular continues using signals-based services rather than a store library.
- Minimal backend read-model endpoints may be added to support dashboard and
  tags views.
- This feature does not cover frontend auth/session hardening; that work lives
  in `10-user-authentication/frontend-auth.md`.
