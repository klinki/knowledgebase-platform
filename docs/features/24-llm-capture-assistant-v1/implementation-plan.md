## LLM Capture Assistant v1.1 — Generic `search_captures` Tool

### Summary
- Update the existing assistant feature plan to include generic capture search v1.1.
- Implement a new assistant tool `search_captures` with hybrid query behavior and structured filters.
- Keep existing destructive safeguards unchanged: delete remains confirmation-based and scoped to the active result set.

### Key Changes
- Assistant orchestration:
  - Extend tool allowlist with `search_captures`.
  - Extend planner/tool-call parsing to accept arguments:
    - `query`, `tags`, `tagMatchMode`, `labels`, `labelMatchMode`, `page`, `pageSize`, `threshold`, `contentType`, `status`, `dateFrom`, `dateTo`.
  - Persist each `search_captures` response as a new result set snapshot and set it as current.
- Backend search capability:
  - Add a new capture-level search contract/service used by assistant tooling.
  - Implement hybrid search:
    - Semantic scoring path for completed captures with processed insights.
    - Text-match path over raw capture content/source fields so non-completed captures can match.
  - Apply all filters in one pipeline:
    - `contentType`, `status`, tags/labels with any/all modes, and inclusive `createdAt` date range.
  - Return deterministic pagination + total count + preview payload suitable for result-set persistence.
- Data/DTO/interface additions:
  - Add search DTO/type(s) for capture-search criteria and result records for assistant use.
  - Extend repository/query layer with methods needed for filtered capture search and optional semantic/text blend.
  - Keep chat public API shape unchanged; capability is delivered through existing chat endpoints.
- Frontend assistant page:
  - No route/layout changes needed.
  - Ensure result rendering handles mixed-status search results cleanly (completed/failed/pending) and keeps action context intact.

### Test Plan
- Backend unit tests:
  - `search_captures` applies tags/labels modes and `contentType`/`status`/date filters correctly.
  - Hybrid query returns completed semantic matches plus non-completed text matches.
  - Date range uses `createdAt` inclusively (`dateFrom <= createdAt <= dateTo`).
- Backend integration tests:
  - Chat flow: generic search command returns expected mixed-status captures.
  - Follow-up “delete all these” still creates pending action only.
  - Confirm deletes exactly current result-set IDs and remains owner-scoped.
- Frontend tests:
  - Assistant view renders generic result sets and preserves active context between sequential commands.
  - Existing pending delete confirmation flow still blocks execution until confirm.

### Assumptions and Defaults
- Query mode: hybrid semantic + text.
- Date filter field: `createdAt` only (v1.1).
- At least one criterion is required (query, tags, labels, contentType, status, or date range).
- Pagination defaults and limits follow existing assistant/result-set safety caps.
- MCP remains deferred to phase 2 and will wrap the same backend tooling.
