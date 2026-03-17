# Implementation Plan: Capture Browsing and Logged-In Not Found

## Summary

Implement the feature in three slices:
1. backend capture detail contract update
2. frontend capture list and detail flow
3. authenticated shell 404

## Planned Changes

### Backend

- Update `CaptureResponseDto` to include `RawContent` and `Metadata`.
- Update `CaptureService` mapping so capture detail responses expose the full stored capture payload.
- Keep `CaptureController` route shapes unchanged.
- Add integration coverage for detail response content and owner scoping.

### Frontend data and state

- Add a dedicated signals-based capture state service for list and detail loading.
- Add capture list and capture detail read-model types separate from dashboard list items.
- Keep sorting client-side in the service and component layer.

### Capture list page

- Add a standalone component for `/captures`.
- Render a sortable table with:
  - type
  - created
  - status
  - source
- Default to `createdAt desc`.
- Toggle sort direction on repeated column clicks.
- Navigate to `/captures/:id` on row click.
- Show explicit loading, empty, and error states.

### Capture detail page

- Add a standalone component for `/captures/:id`.
- Render source URL, content type, status, timestamps, tags, raw content, metadata, and processed insight details.
- Parse `metadata`, `keyInsights`, and `actionItems` as JSON when possible; otherwise render plain text.
- Show an in-page not-found state when the capture is missing.

### Authenticated 404

- Add a shell child not-found component.
- Add a shell child wildcard route after the known logged-in routes.
- Keep current auth guard behavior so anonymous users still flow to login.

### Shell navigation

- Add a `Captures` nav item to the sidebar.
- Keep dashboard, tags, and invitation navigation unchanged.

## Test Cases

- Backend:
  - detail endpoint returns raw content and metadata
  - detail endpoint returns only the owner’s capture
- Frontend:
  - captures page initial render sorts newest-first
  - sorting by type, status, source, and created works
  - row click navigates to detail
  - detail page shows raw content and metadata
  - detail page renders processed insight when present
  - detail page handles null processed insight
  - detail page handles not-found capture
  - authenticated wildcard route renders shell 404
  - anonymous unknown route still redirects through login

## Implementation Order

1. Add the feature docs in `docs/features/14-capture-browsing/`.
2. Extend backend capture DTO and mapping.
3. Add backend integration coverage.
4. Add frontend capture models and state service.
5. Add captures list component and route.
6. Add capture detail component and route.
7. Add authenticated not-found component and wildcard route.
8. Add shell navigation entry.
9. Add frontend tests and run builds/tests.
10. Mark the feature spec checklists complete.

## Execution Notes

- Follow the existing docs-as-code workflow for feature status updates.
- Reuse Angular standalone-component and signals-service patterns already used by dashboard, tags, and auth features.
- Do not add capture mutation actions, server-side sorting, or new capture endpoints in this feature.
