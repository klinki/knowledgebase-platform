# Bug Description

## Title
Search results link to processed insight ids instead of capture ids

## Status
- open

## Reported Symptoms
- Search result cards in the frontend navigate to an invalid capture URL.
- The link appears to contain the GUID of the processed insight rather than the raw capture.

## Expected Behavior
- Clicking a search result should open the matching capture detail page.

## Actual Behavior
- The search page builds links from the search result id, which currently resolves to the processed insight id.

## Reproduction Details
- Observed in the search frontend on 2026-04-12.
- Affected file: `frontend/src/app/features/search/search.component.html`

## Affected Area
- Search frontend
- Combined search result contract

## Constraints
- Preserve the existing search result payload shape where possible.
- Avoid breaking other consumers of the search API.

## Open Questions
- None for the local fix.
