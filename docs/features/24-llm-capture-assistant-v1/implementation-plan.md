## Search + Assistant Sorting v1

### Summary
- Add explicit sorting to regular `/search` and to assistant `search_captures`.
- Keep existing endpoints; extend request/tool arguments with `sortField` + `sortDirection`.
- Support assistant follow-up commands like "sort these by newest" by re-running the last `search_captures` criteria against the full dataset and creating a new result-set snapshot.

### Implementation Changes
- **Regular search API and backend**
- Extend `SearchRequestDto` with `sortField` and `sortDirection`.
- Supported regular-search sort fields: `relevance`, `processedAt`, `title`, `sourceUrl`.
- Supported direction: `asc`, `desc`.
- Update validation to allow only the fields/directions above.
- Update `SearchService` normalization:
- If query exists and sort is omitted: default `relevance desc`.
- If query is empty and sort is omitted: default `processedAt desc`.
- If `sortField=relevance` with empty query: normalize to `processedAt desc`.
- Extend `IProcessedInsightRepository.SearchAsync(...)` to accept sort arguments and apply deterministic ordering with stable tie-breaker by `Id`.
- Preserve current filtering semantics (query/tag/label/threshold/pagination).

- **Regular search frontend**
- Extend `SearchCriteria` with `sortField` + `sortDirection`.
- Persist sort state in URL query params (`sortField`, `sortDirection`) via `SearchStateService`.
- Include sort fields in `/v1/search` POST body.
- Add sort control in search results toolbar using these options:
- Relevance (best match), Newest processed, Oldest processed, Title A-Z, Title Z-A, Source A-Z, Source Z-A.
- Use one normalized criteria builder so submit/page/page-size/sort changes all keep consistent behavior.

- **Assistant `search_captures` sorting**
- Extend assistant tool schema/parser (`PlannedToolCall`) with `sortField` + `sortDirection`.
- Supported assistant capture sort fields: `relevance`, `createdAt`, `status`, `contentType`, `sourceUrl`.
- Supported direction: `asc`, `desc`.
- Extend capture search contracts (`CaptureSearchCriteria`, `CaptureSearchQueryOptions`) and repository sort logic to apply field sorting deterministically.
- Defaults for assistant:
- Query present + sort omitted: `relevance desc`.
- Query absent + sort omitted: `createdAt desc`.
- `relevance` requested without query: normalize to `createdAt desc`.

- **Assistant follow-up re-sort behavior**
- Add persisted criteria snapshot to assistant result sets (`CriteriaJson`) and store normalized `search_captures` criteria for each search result set.
- On follow-up sort intent ("sort these ..."):
- Require active result set to be `queryType=search_captures` with valid criteria snapshot.
- Re-run full `search_captures` using stored criteria + new sort, create a new result-set snapshot, and set it as active.
- If active result set is not sortable (for example `deleted_twitter_accounts`), return a clear assistant message asking user to run/confirm a sortable search first.
- Keep delete safety unchanged: delete remains confirmation-based and scoped to the current active result-set snapshot.

- **Assistant planner/fallback updates**
- Update OpenAI planner prompt argument docs to include `sortField`, `sortDirection`.
- Add deterministic fallback parsing for sort intents (`newest/oldest/relevance/source/status/type`) that produces a `search_captures` tool call with sort args.

- **Data and migration**
- Add migration for assistant result-set criteria persistence (`CriteriaJson` text/json column with safe default).
- Keep chat endpoint shapes unchanged; sorting is delivered through existing chat endpoints/tool calls.

### Test Plan
- **Backend unit**
- `SearchService` sort normalization defaults and fallback (`relevance` only with query).
- `ProcessedInsightRepository.SearchAsync` ordering for each regular-search sort mode.
- `CaptureBulkActionService.SearchCapturesAsync` forwards sort args and normalization.
- `AssistantChatService` follow-up sort intent reuses stored criteria and creates a new result set.
- Negative case: follow-up sort when active result set is non-`search_captures` returns non-destructive guidance.
- Existing delete-confirmation tests remain green.

- **Backend integration**
- `/api/v1/search` returns deterministic ordering for:
- query + relevance,
- query + processedAt/title/sourceUrl,
- no query + default processedAt.
- Chat flow:
- initial `search_captures`,
- follow-up “sort these by oldest/newest” creates a new snapshot with changed order,
- follow-up delete still targets only the latest snapshot after explicit confirm.

- **Frontend tests**
- `search-state.service` parse/build URL params includes sort fields and POST payload includes sort.
- Search component sort control updates URL + triggers search + preserves pagination semantics.
- Assistant component/state regression checks still pass with sorted result-set rendering and unchanged pending-delete flow.

### Assumptions and Defaults
- Regular search default: `relevance desc` when query exists; otherwise `processedAt desc`.
- Assistant capture search default: `relevance desc` when query exists; otherwise `createdAt desc`.
- Follow-up assistant sorting applies only to active `search_captures` context and always re-runs full search criteria (not preview-only reorder).
- No new endpoints are introduced; only request/tool argument and persistence extensions are added.
