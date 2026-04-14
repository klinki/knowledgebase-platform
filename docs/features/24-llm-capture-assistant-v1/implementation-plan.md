### LLM Capture Assistant v1 (Dedicated Chat Page, Safe Bulk Actions)

#### Summary
- Deliver a new authenticated `/assistant` page with a persisted per-user chat.
- Implement backend LLM tool orchestration for capture operations: search, add/remove tags, add/remove labels, and delete.
- Use deterministic “deleted Twitter account” detection via tweet skip codes: `twitter_suspended_account`, `twitter_account_limited`, `twitter_post_unavailable`.
- Enforce destructive safety: delete can target only the current chat result set and always requires explicit second-step confirmation.
- Defer MCP exposure to phase 2.

#### Key Changes
- Backend chat API and orchestration:
  - Add `ChatController` with endpoints to load chat history, send message, and confirm/cancel pending actions.
  - Add `AssistantChatService` that runs a tool-calling loop with a strict tool allowlist.
  - Add result-set tracking so follow-ups like “Now delete all these tweets” resolve to the latest assistant result set only.
- Backend capture operations:
  - Add query/mutation service layer for bulk capture actions (by owner-scoped capture IDs).
  - Add bulk tag/label mutation behavior with explicit add/remove semantics.
  - Add bulk delete execution by ID list (owner-scoped), invoked only after confirmation.
  - Keep raw and processed representations aligned: tag/label mutations update both `RawCapture` and `ProcessedInsight` when present.
- Persistence:
  - Add DB entities/tables for:
  - Chat session/history (user-scoped persisted messages)
  - Stored result sets (capture ID snapshots + summary metadata)
  - Pending destructive actions (status, scope, count, confirmation lifecycle)
- Frontend:
  - Add `/assistant` route + shell nav entry.
  - Build assistant page with:
  - Message thread
  - Result set rendering (count + sample rows + action context)
  - Pending-action confirmation card (confirm/cancel) for delete
  - Persisted reload of prior conversation/actions.

#### Public API / Interface Additions
- New API endpoints (v1):
  - `GET /api/v1/chat/session` (or active-session equivalent)
  - `GET /api/v1/chat/session/messages`
  - `POST /api/v1/chat/session/messages` (user message -> assistant response + optional pending action/result set metadata)
  - `POST /api/v1/chat/actions/{actionId}/confirm`
  - `POST /api/v1/chat/actions/{actionId}/cancel`
- Application interfaces to add/extend:
  - Assistant orchestration service contract
  - Capture bulk query/mutation contract (query by filters/skip codes, bulk tag/label add-remove, bulk delete by IDs)

#### Test Plan
- Backend unit tests:
  - Deleted-account query returns only tweet captures with selected skip codes.
  - Tag/label add/remove mutates owner-scoped captures and syncs processed insights.
  - Delete proposal requires confirmation and cannot execute outside current result-set scope.
- Backend integration tests:
  - End-to-end chat flow:
  - “Find me all tweets from deleted accounts” returns expected captures.
  - “Now delete all these tweets” creates pending action (no deletion yet).
  - Confirm endpoint performs deletion and reports exact deleted count.
  - Owner isolation is enforced for all tool actions.
- Frontend tests:
  - Route/nav includes `/assistant`.
  - Chat page loads persisted history.
  - Pending delete shows two-step confirmation UI and only executes after confirm.
  - Result set context is preserved between sequential commands.

#### Assumptions / Defaults
- v1 uses one active persisted chat session per user (no multi-thread session UX yet).
- Assistant result-set execution is capped (store all IDs up to safe limit; show preview subset in UI).
- “Deleted accounts” is intentionally mapped to the selected skip-code set, not heuristic LLM interpretation.
- MCP support is explicitly postponed to phase 2 and will wrap these same backend tools/APIs.
