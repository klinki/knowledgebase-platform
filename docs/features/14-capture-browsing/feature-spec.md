# Feature: Capture Browsing and Logged-In Not Found (14-capture-browsing)

## Goal

Add a first-class logged-in capture browsing flow with:
- a sortable captures list
- a read-only capture detail page that shows all stored capture information
- a not-found page rendered inside the authenticated shell

## Acceptance Criteria

- [x] Logged-in users can open a dedicated captures page.
- [x] The captures page shows capture type, creation date, processing status, and source.
- [x] The captures page defaults to newest-first ordering.
- [x] The captures page supports client-side sorting by each visible column.
- [x] Selecting a capture opens a dedicated read-only detail page.
- [x] The detail page shows source URL, content type, status, created at, processed at, tags, raw content, metadata, and processed insight data when present.
- [x] The backend capture detail response includes raw content and metadata.
- [x] Unknown routes for authenticated users render a shell-wrapped not-found page.
- [x] Unknown routes for unauthenticated users continue through the current login/auth flow.
- [x] No new write/edit/delete capture actions are introduced in this feature.

## Architecture Notes

- Reuse existing authenticated shell layout and navigation patterns.
- Reuse existing `GET /api/v1/capture` and `GET /api/v1/capture/{id}` endpoints.
- Extend the capture detail DTO rather than introducing a parallel detail endpoint.
- Keep sorting client-side in the Angular app for v1.
- Keep the feature read-only.

## Important Interfaces and Behavior

### Backend

- `GET /api/v1/capture`
- `GET /api/v1/capture/{id}`
- `CaptureResponseDto`
  - `rawContent`
  - `metadata`

### Frontend routes

- `/captures`
- `/captures/:id`
- authenticated shell wildcard route for not-found

### Frontend behavior

- Capture list sorts by `createdAt desc` by default.
- Capture rows navigate to detail.
- Capture detail formats metadata and processed insight JSON defensively.
- Authenticated unknown routes keep the shell and sidebar visible.

## Implementation Status

- [x] Add backend DTO fields for capture detail.
- [x] Add and update backend mapping for raw content and metadata.
- [x] Add frontend capture state service and read models.
- [x] Add captures list page.
- [x] Add capture detail page.
- [x] Add authenticated not-found page.
- [x] Add shell navigation entry for captures.
- [x] Add route wiring.
- [x] Add frontend tests.
- [x] Add backend tests.

## Verification Plan

- [x] API build passes.
- [x] Frontend build passes.
- [x] Capture list default sort works.
- [x] Capture sorting works for each column.
- [x] Capture detail renders all expected fields.
- [x] Processed insight optional state renders correctly.
- [x] Authenticated unknown routes show shell 404.
- [x] Unauthenticated unknown routes still reach login flow.

## Assumptions and Defaults

- Feature folder name is `14-capture-browsing`.
- Default list sort is created-at descending.
- Detail page is read-only in v1.
- Metadata may be raw text or JSON and is rendered safely.
- Capture detail not-found is handled inside the page, not by redirect.
- Public anonymous 404 is out of scope.
