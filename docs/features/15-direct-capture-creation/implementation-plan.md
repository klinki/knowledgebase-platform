# Implementation Plan: Frontend Direct Capture Creation

## Summary

Implement direct frontend capture creation in four slices:

1. save the feature docs
2. relax backend capture validation
3. add create behavior to frontend capture state
4. add the create-capture page and routing

## Planned Changes

### Backend

- Update `CaptureRequestValidator` so `sourceUrl` is optional.
- Keep URL validation when `sourceUrl` is present.
- Keep `rawContent` required.
- Add validator tests for:
  - URL-only capture
  - direct-content capture without URL
  - invalid supplied URL

### Frontend state and mapping

- Extend the capture state service with a create method that:
  - accepts form inputs
  - maps URL-only capture to `Article`
  - generates minimal raw content and metadata
  - trims and filters comma-separated tags
  - posts to `/api/v1/capture`
- Return the accepted capture id so the UI can navigate to detail after success.

### Create Capture page

- Add a standalone `/captures/new` page inside the authenticated shell.
- Render one form with:
  - source URL
  - content type
  - raw content textarea
  - comma-separated tags
- Client-side rules:
  - require at least one of URL or content
  - require content type for direct-content mode
  - lock to `Article` when only URL is present
  - respect the backend raw-content length limit
- Show explicit validation, submit-loading, success, and error states.

### Routing and navigation

- Add `/captures/new` to the shell routes.
- Add `Create Capture` to the sidebar navigation.
- Add a CTA from the captures list page to the create page.

## Test Cases

- Backend:
  - validator accepts URL-only payload
  - validator accepts direct-content payload without URL
  - validator rejects invalid URL when provided
- Frontend:
  - create page renders in shell
  - URL-only submission maps to `Article` and generated raw content
  - direct-content submission requires selected type
  - comma-separated tags are trimmed before submit
  - empty form is rejected client-side
  - success navigates to capture detail
- Integration:
  - authenticated create works for URL-only capture
  - authenticated create works for direct-content capture

## Implementation Order

1. Save the feature docs under `docs/features/15-direct-capture-creation/`.
2. Update backend validator and unit tests.
3. Extend capture state with create behavior.
4. Add the create page and route.
5. Add shell and captures-page navigation affordances.
6. Add frontend unit tests.
7. Add backend integration assertions if needed.
8. Run builds and tests.
9. Mark the feature spec checklists complete.

## Execution Notes

- Reuse the existing Angular standalone-component and signals-service patterns.
- Do not add a new backend URL-fetch endpoint.
- Use lightweight metadata JSON to identify whether the capture came from URL input or manual input.
